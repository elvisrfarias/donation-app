# ============================================================================
#  Doação App — atalhos de desenvolvimento (raiz do monorepo).
#  Rode sempre da raiz. Os alvos .NET montam ./backend como /src no container;
#  os alvos de compose usam o docker-compose.yml e o .env desta pasta (raiz).
#  Como NÃO há SDK .NET local, tudo roda dentro de containers do SDK .NET 9,
#  com um volume NuGet persistente para acelerar restores.
#  Rode `make` ou `make help` para ver os alvos.
#
#  Atalhos do dia a dia (reiniciam TODA a stack num comando só):
#    make dev         -> sobe/reinicia o ambiente de desenvolvimento (Vite HMR)
#    make dev-reset   -> idem, zerando o banco/arquivos (re-roda o seed)
#    make prod        -> sobe/reinicia o ambiente de produção (nginx)
#    make prod-reset  -> idem, zerando o banco/arquivos
# ============================================================================

SDK_IMAGE  ?= mcr.microsoft.com/dotnet/sdk:9.0
NUGET_VOL  ?= doacao-nuget
SOLUTION   ?= DoacaoApp.sln
CONFIG     ?= Release
INFRA      := src/DoacaoApp.Infrastructure
API        := src/DoacaoApp.Api
HOST_OWNER := $(shell stat -c '%u:%g' backend/DoacaoApp.sln 2>/dev/null || echo 1000:1000)
BACKUP_DIR ?= backups
STAMP      := $(shell date +%Y%m%d-%H%M%S)

# Wrapper: roda o `dotnet` dentro do container do SDK, montando o código e o cache NuGet.
DOTNET = docker run --rm -v "$(CURDIR)/backend":/src -v $(NUGET_VOL):/root/.nuget -w /src $(SDK_IMAGE) dotnet
# Wrapper para o `dotnet ef` (instala a tool e ajusta o PATH).
EF_RUN = docker run --rm -v "$(CURDIR)/backend":/src -v $(NUGET_VOL):/root/.nuget -w /src $(SDK_IMAGE) bash -c

.DEFAULT_GOAL := help
.PHONY: help restore build rebuild test test-domain test-application test-integration test-filter \
        migration migration-remove db-update image env \
        dev dev-reset dev-logs prod prod-reset prod-logs \
        down down-v logs ps clean \
        backup backup-db backup-storage restore-db restore-storage

help: ## Mostra esta ajuda
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) \
	  | awk 'BEGIN {FS = ":.*?## "}; {printf "  \033[36m%-20s\033[0m %s\n", $$1, $$2}'

# ---- Build / restore -------------------------------------------------------

restore: ## Restaura os pacotes NuGet
	$(DOTNET) restore $(SOLUTION)

build: ## Compila a solution inteira (src + testes)
	$(DOTNET) build $(SOLUTION) -c $(CONFIG)

rebuild: ## Recompila do zero
	$(DOTNET) build $(SOLUTION) -c $(CONFIG) --no-incremental

# ---- Testes ----------------------------------------------------------------

test: test-domain test-application ## Roda os testes unitários (Domain + Application)

test-domain: ## Testes de domínio (VOs + regras)
	$(DOTNET) test tests/DoacaoApp.Domain.Tests -c $(CONFIG)

test-application: ## Testes de casos de uso (Moq)
	$(DOTNET) test tests/DoacaoApp.Application.Tests -c $(CONFIG)

test-integration: ## Testes de integração (Testcontainers; precisa de Docker no host)
	$(DOTNET) test tests/DoacaoApp.IntegrationTests -c $(CONFIG)

# Ex.: make test-filter FILTER=CpfTests
test-filter: ## Roda um filtro de teste no Domain (use FILTER=...)
	$(DOTNET) test tests/DoacaoApp.Domain.Tests -c $(CONFIG) --filter "FullyQualifiedName~$(FILTER)"

# ---- Migrations (EF Core) --------------------------------------------------

migration: ## Cria migration -> make migration name=NomeDaMigration
	@test -n "$(name)" || { echo "Uso: make migration name=NomeDaMigration"; exit 1; }
	$(EF_RUN) 'dotnet tool install -g dotnet-ef --version 9.0.0 >/dev/null 2>&1; export PATH=$$PATH:/root/.dotnet/tools; \
	  dotnet ef migrations add $(name) -p $(INFRA) -s $(API) -o Persistence/Migrations'

migration-remove: ## Remove a última migration
	$(EF_RUN) 'dotnet tool install -g dotnet-ef --version 9.0.0 >/dev/null 2>&1; export PATH=$$PATH:/root/.dotnet/tools; \
	  dotnet ef migrations remove -p $(INFRA) -s $(API)'

db-update: ## Aplica as migrations no banco (lê ConnectionStrings__Database do ambiente)
	$(EF_RUN) 'dotnet tool install -g dotnet-ef --version 9.0.0 >/dev/null 2>&1; export PATH=$$PATH:/root/.dotnet/tools; \
	  dotnet ef database update -p $(INFRA) -s $(API)'

# ---- Docker / execução -----------------------------------------------------
# Conjuntos de arquivos compose por ambiente. Um único `make dev`/`make prod`
# reinicia TODA a stack (down + build + up), sem precisar encadear comandos.
#   DEV  = base + override (API em Development) + frontend Vite com HMR.
#   PROD = somente a base (frontend nginx + API em Production), sem overrides de dev.
COMPOSE_DEV  := docker compose --profile dev -f docker-compose.yml -f docker-compose.override.yml -f docker-compose.dev.yml
# Força Production mesmo que o .env (voltado a dev) defina ASPNETCORE_ENVIRONMENT=Development.
COMPOSE_PROD := docker compose --env-file .env.prod -f docker-compose.yml -f docker-compose.prod.yml

env: ## Cria o .env a partir do .env.example (se não existir)
	@test -f .env || cp .env.example .env
	@echo ".env pronto."

image: ## Builda a imagem de produção da API (Dockerfile multi-stage)
	docker build -t doacao-api:latest backend

dev: env ## Reinicia TODA a stack de desenvolvimento (Vite HMR + API + Postgres + MinIO + pgAdmin)
	$(COMPOSE_DEV) down --remove-orphans
	$(COMPOSE_DEV) up -d --build
	@$(COMPOSE_DEV) ps
	@printf '\n  Stack de DEV no ar (portas padrão; ajuste no .env):\n    Frontend (Vite/HMR)  http://localhost:5445\n    API (Scalar)         http://localhost:8080/scalar/v1\n    pgAdmin              http://localhost:5050\n    MinIO console        http://localhost:9001\n  Logs: make dev-logs\n\n'

dev-reset: env ## Igual ao dev, mas APAGA os volumes (banco + arquivos) e re-seeda do zero
	$(COMPOSE_DEV) down -v --remove-orphans
	$(COMPOSE_DEV) up -d --build
	@$(COMPOSE_DEV) ps

dev-logs: ## Acompanha os logs de desenvolvimento (frontend Vite + API)
	$(COMPOSE_DEV) logs -f frontend api

prod: ## Reinicia TODA a stack de produção (frontend nginx + API em Production)
	$(COMPOSE_PROD) down --remove-orphans
	$(COMPOSE_PROD) up -d --build
	@$(COMPOSE_PROD) ps
	@printf '\n  Stack de PRODUÇÃO no ar (nginx de borda + HTTPS). Logs: make prod-logs\n\n'

prod-reset: ## Igual ao prod, mas APAGA os volumes (zera e re-seeda) — exige CONFIRM=yes; faz backup antes
	@test "$(CONFIRM)" = "yes" || { echo "Isso apaga TODOS os dados de PRODUÇÃO (banco + arquivos). Rode: make prod-reset CONFIRM=yes"; exit 1; }
	@$(MAKE) backup
	$(COMPOSE_PROD) down -v --remove-orphans
	$(COMPOSE_PROD) up -d --build
	@$(COMPOSE_PROD) ps

prod-logs: ## Acompanha os logs de produção (API)
	$(COMPOSE_PROD) logs -f api

down: ## Para a stack (mantém os dados)
	docker compose down --remove-orphans

down-v: ## Para a stack e remove os volumes (apaga banco + arquivos do MinIO) — exige CONFIRM=yes
	@test "$(CONFIRM)" = "yes" || { echo "Isso apaga TODOS os dados (banco + arquivos do MinIO). Rode: make down-v CONFIRM=yes"; exit 1; }
	docker compose down -v --remove-orphans

logs: ## Acompanha os logs da API
	docker compose logs -f api

ps: ## Status dos containers
	docker compose ps

# ---- Backup / restore (Postgres + MinIO) -----------------------------------
# Os containers (doacao-postgres, doacao-minio) precisam estar rodando.
# Nada aqui é automático/agendado — rode manualmente antes de qualquer operação
# arriscada (deploy, migration em produção, `make prod-reset`).

backup: backup-db backup-storage ## Backup completo: banco (pg_dump) + arquivos (tar do volume do MinIO)

backup-db: ## Backup do Postgres -> backups/db-<timestamp>.dump
	@mkdir -p $(BACKUP_DIR)
	umask 077; docker exec doacao-postgres sh -c 'pg_dump -U "$$POSTGRES_USER" -d "$$POSTGRES_DB" -Fc' > $(BACKUP_DIR)/db-$(STAMP).dump \
	  || { rm -f $(BACKUP_DIR)/db-$(STAMP).dump; echo "Falha no pg_dump; backup descartado."; exit 1; }
	@echo "Backup do banco salvo em $(BACKUP_DIR)/db-$(STAMP).dump"

backup-storage: ## Backup dos arquivos do MinIO -> backups/storage-<timestamp>.tar.gz
	@mkdir -p $(BACKUP_DIR)
	docker run --rm --volumes-from doacao-minio -v "$(CURDIR)/$(BACKUP_DIR)":/backup alpine \
	  sh -c "umask 077 && tar czf /backup/storage-$(STAMP).tar.gz -C /data ."
	@echo "Backup dos arquivos salvo em $(BACKUP_DIR)/storage-$(STAMP).tar.gz"

restore-db: ## Restaura o Postgres -> make restore-db file=backups/db-XXXX.dump
	@test -n "$(file)" || { echo "Uso: make restore-db file=backups/db-XXXX.dump"; exit 1; }
	@test -f "$(file)" || { echo "Arquivo não encontrado: $(file)"; exit 1; }
	docker exec -i doacao-postgres sh -c 'pg_restore -U "$$POSTGRES_USER" -d "$$POSTGRES_DB" --clean --if-exists' < $(file)
	@echo "Banco restaurado a partir de $(file)."

restore-storage: ## Restaura os arquivos do MinIO -> make restore-storage file=backups/storage-XXXX.tar.gz
	@test -n "$(file)" || { echo "Uso: make restore-storage file=backups/storage-XXXX.tar.gz"; exit 1; }
	@test -f "$(file)" || { echo "Arquivo não encontrado: $(file)"; exit 1; }
	docker run --rm --volumes-from doacao-minio -v "$(CURDIR)/$(file)":/backup/restore.tar.gz alpine \
	  sh -c "rm -rf /data/* && tar xzf /backup/restore.tar.gz -C /data"
	@echo "Arquivos restaurados a partir de $(file)."

# ---- Frontend (Node/Vite no host) ------------------------------------------
.PHONY: front-install front-dev front-build front-lint front-image

front-install: ## Instala as dependências do frontend
	cd frontend && npm install

front-dev: ## Sobe o Vite dev server (proxy /api -> :8080)
	cd frontend && npm run dev

front-build: ## Build de produção do frontend
	cd frontend && npm run build

front-lint: ## Lint do frontend
	cd frontend && npm run lint

front-image: ## Builda a imagem nginx do frontend
	docker build -t doacao-frontend:latest frontend

# ---- Limpeza ---------------------------------------------------------------

clean: ## Remove bin/obj e devolve a propriedade dos arquivos gerados por container
	docker run --rm -v "$(CURDIR)/backend":/src -w /src alpine sh -c \
	  'find . -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} + ; chown -R $(HOST_OWNER) /src'