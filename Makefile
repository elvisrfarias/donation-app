# ============================================================================
#  Doação App — atalhos de desenvolvimento (raiz do monorepo).
#  Rode sempre da raiz. Os alvos .NET montam ./backend como /src no container;
#  os alvos de compose usam o docker-compose.yml e o .env desta pasta (raiz).
#  Como NÃO há SDK .NET local, tudo roda dentro de containers do SDK .NET 9,
#  com um volume NuGet persistente para acelerar restores.
#  Rode `make` ou `make help` para ver os alvos.
# ============================================================================

SDK_IMAGE  ?= mcr.microsoft.com/dotnet/sdk:9.0
NUGET_VOL  ?= doacao-nuget
SOLUTION   ?= DoacaoApp.sln
CONFIG     ?= Release
INFRA      := src/DoacaoApp.Infrastructure
API        := src/DoacaoApp.Api
HOST_OWNER := $(shell stat -c '%u:%g' backend/DoacaoApp.sln 2>/dev/null || echo 1000:1000)

# Wrapper: roda o `dotnet` dentro do container do SDK, montando o código e o cache NuGet.
DOTNET = docker run --rm -v "$(CURDIR)/backend":/src -v $(NUGET_VOL):/root/.nuget -w /src $(SDK_IMAGE) dotnet
# Wrapper para o `dotnet ef` (instala a tool e ajusta o PATH).
EF_RUN = docker run --rm -v "$(CURDIR)/backend":/src -v $(NUGET_VOL):/root/.nuget -w /src $(SDK_IMAGE) bash -c

.DEFAULT_GOAL := help
.PHONY: help restore build rebuild test test-domain test-application test-integration test-filter \
        migration migration-remove db-update image up up-build down down-v logs ps clean env

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

env: ## Cria o .env a partir do .env.example (se não existir)
	@test -f .env || cp .env.example .env
	@echo ".env pronto."

image: ## Builda a imagem de produção (Dockerfile multi-stage)
	docker build -t doacao-api:latest backend

up: env ## Sobe a stack (postgres + api + frontend + pgadmin)
	docker compose up -d

up-build: env ## Sobe a stack reconstruindo as imagens (api + frontend)
	docker compose up -d --build

down: ## Para a stack
	docker compose down

down-v: ## Para a stack e remove os volumes (apaga os dados do Postgres)
	docker compose down -v

logs: ## Acompanha os logs da API
	docker compose logs -f api

ps: ## Status dos containers
	docker compose ps

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