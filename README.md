# Doação App — Gestão de Pontos de Doação/Coleta

Sistema de gerenciamento de pontos de doação/coleta (cesta básica, roupas, etc.) com foco em
**rastreabilidade por beneficiário** e **controle de equidade** entre beneficiários.

> Regra central: **todos os pontos de coleta do município compartilham o histórico de retiradas**.
> Se a Família Silva já retirou no Ponto A neste ciclo, o Ponto B é impedido de liberar uma nova
> retirada além do limite — a verificação considera o histórico de **todos** os pontos.

Construído com **.NET 9 / C# 13**, **Clean Architecture + DDD + CQRS**, **EF Core 9 + PostgreSQL 16**.
O **frontend** (em [`frontend/`](frontend/)) é uma SPA **React 19 + Vite 8 + TypeScript** com
arquitetura **MVVM**, **Tailwind CSS** e mapa em **OpenStreetMap** (react-leaflet) — detalhes em
[frontend/README.md](frontend/README.md).

## Estrutura do monorepo

```
.
├── backend/             # API .NET 9 (Clean Architecture + DDD + CQRS)
├── frontend/            # SPA React 19 + Vite 8 (MVVM, Tailwind, OpenStreetMap)
├── docker-compose.yml   # orquestra postgres + minio + api + frontend (+ pgadmin)
├── Makefile             # atalhos de dev (rode `make help`)
└── .env / .env.example  # variáveis lidas pelo compose
```

Subir tudo: `cp .env.example .env && make up` (ou `docker compose up --build`). Em seguida o
frontend fica em `http://localhost:3000`, a API em `http://localhost:8080`.

---

## Sumário

- [Arquitetura](#arquitetura)
- [Stack técnica](#stack-técnica)
- [Estrutura de pastas](#estrutura-de-pastas)
- [Modelo de domínio](#modelo-de-domínio)
- [Como executar](#como-executar)
- [Migrations](#migrations)
- [Testes](#testes)
- [Documentação da API (Scalar)](#documentação-da-api-scalar)
- [Endpoints](#endpoints)
- [Autenticação, roles e o porquê do BCrypt](#autenticação-roles-e-o-porquê-do-bcrypt)
- [Observabilidade](#observabilidade)
- [Object Storage (MinIO/S3)](#object-storage-minios3)
- [Variáveis de ambiente](#variáveis-de-ambiente-resumo)
- [Débito técnico conhecido](#débito-técnico-conhecido)

---

## Arquitetura

Clean Architecture com dependências apontando sempre para dentro:

```
API  ─►  Infrastructure  ─►  Application  ─►  Domain
└──────────────────────────────────────────────┘
        (Domain não depende de ninguém)
```

- **Domain** — Entities, Value Objects, Domain Events, Aggregates, interfaces de repositório.
  Sem dependência de framework (só `ErrorOr` e `MediatR.Contracts`).
- **Application** — Casos de uso (CQRS via **MediatR**), validação (**FluentValidation**),
  DTOs e o `ValidationBehavior` (pipeline). Result Pattern com **ErrorOr** (sem exceptions para
  fluxo de negócio).
- **Infrastructure** — EF Core + PostgreSQL, repositórios, ASP.NET Core Identity (hashing **BCrypt**),
  JWT + refresh tokens, dispatch de Domain Events (interceptor de `SaveChanges`).
- **API** — Minimal APIs (auth e parte pública) + Controllers (domínios gerenciais), middlewares
  (Correlation ID, exception handler), Scalar/OpenAPI, rate limiting, health checks.

### Padrões aplicados

| Padrão | Onde |
|---|---|
| Aggregates / Aggregate Root | `CollectionPoint`, `Beneficiary` |
| Value Objects | `Cpf`, `Email`, `PhoneNumber`, `Address`, `ContactInfo`, `WithdrawalCycle` |
| Strongly-typed IDs | `CollectionPointId`, `BeneficiaryId`, `WithdrawalId` |
| Domain Events | `CollectionPointCreated`, `WithdrawalRegistered`, `BeneficiaryRegistered` |
| Result Pattern | `ErrorOr<T>` em todos os fluxos de negócio |
| CQRS | Commands/Queries + Handlers (MediatR) |
| Repository + Unit of Work | `ICollectionPointRepository`, `IBeneficiaryRepository`, `IUnitOfWork` |

---

## Stack técnica

- **.NET 9**, **C# 13** (primary constructors, collection expressions, records para VOs)
- **ASP.NET Core Web API** (Minimal APIs + Controllers)
- **EF Core 9** Code-First + **Npgsql** / **PostgreSQL 16**
- **ASP.NET Core Identity** + **JWT Bearer** + refresh token em banco
- **BCrypt.Net-Next** (hashing de senha — ver [seção](#autenticação-roles-e-o-porquê-do-bcrypt))
- **MediatR** (CQRS), **FluentValidation**, **ErrorOr** (Result Pattern)
- **Serilog** (Console + File), **HealthChecks**, **AspNetCoreRateLimit**, Correlation ID
- **Scalar** (documentação interativa da API)
- **AWSSDK.S3** contra **MinIO** (local) / **Amazon S3** (prod) — armazenamento de imagens
- **xUnit** + **FluentAssertions** + **Moq**; **Testcontainers** (PostgreSQL real)

---

## Estrutura de pastas

```
doacao-app/
├── DoacaoApp.sln
├── Directory.Build.props          # props comuns (net9.0, nullable, langversion)
├── Directory.Packages.props       # Central Package Management (versões)
├── Dockerfile                     # multi-stage (sdk build -> aspnet alpine)
├── docker-compose.yml             # api + postgres + minio + frontend + pgadmin
├── docker-compose.override.yml    # ajustes de dev
├── .env.example                   # variáveis (copiar para .env)
├── src/
│   ├── DoacaoApp.Domain/
│   │   ├── Common/                # Entity, AggregateRoot, IDomainEvent, IUnitOfWork
│   │   ├── ValueObjects/          # Cpf, Email, PhoneNumber, Address, ContactInfo, WithdrawalCycle
│   │   ├── CollectionPoints/      # CollectionPoint (root, com Stock), Withdrawal, eventos, repo
│   │   ├── Beneficiaries/         # Beneficiary (root), vínculos, eventos, repo
│   │   ├── Reviews/               # Review (root) — avaliação pública de um ponto
│   │   ├── Storage/                # StoredFile (root) — metadado de arquivo em object storage
│   │   └── Errors/                # DomainErrors (catálogo de erros)
│   ├── DoacaoApp.Application/
│   │   ├── Common/                # Behaviors (ValidationBehavior), Abstractions
│   │   ├── Authentication/        # Commands (Register/Login/Refresh), Roles, AuthErrors
│   │   ├── CollectionPoints/      # Commands, Queries, Contracts, EventHandlers (inclui estoque)
│   │   ├── Beneficiaries/         # Commands, Queries, Contracts, EventHandlers
│   │   ├── Reviews/               # CreateReview, ModerateReview, ListReviewsForPoint
│   │   └── Storage/                # UploadImage, DeleteFile, GetStoredFile, ListFilesByOwner
│   ├── DoacaoApp.Infrastructure/
│   │   ├── Persistence/           # AppDbContext, Configurations, Repositories,
│   │   │                          # Interceptors, Converters, Seed, Migrations
│   │   ├── Identity/              # ApplicationUser, BCryptPasswordHasher, IdentityService
│   │   ├── Authentication/        # JwtOptions, JwtTokenGenerator
│   │   └── Storage/                # S3FileStorage, StorageBucketInitializer, StorageOptions
│   └── DoacaoApp.Api/
│       ├── Controllers/           # CollectionPoints, Beneficiaries, Withdrawals, Reviews, Files
│       ├── Endpoints/             # Auth, PublicCollectionPoints (Minimal APIs)
│       ├── Common/                # ApiController, HttpErrorMapping, MinimalApiResults
│       ├── OpenApi/               # BearerSecuritySchemeTransformer
│       ├── Services/              # CurrentUser
│       ├── Middleware/            # CorrelationId, GlobalExceptionHandler
│       ├── Extensions/            # ServiceCollection + WebApplication
│       └── Program.cs
└── tests/
    ├── DoacaoApp.Domain.Tests/        # VOs + regras de domínio
    ├── DoacaoApp.Application.Tests/   # use cases (Moq)
    └── DoacaoApp.IntegrationTests/    # Testcontainers (PostgreSQL real)
```

---

## Modelo de domínio

- **CollectionPoint** (Aggregate Root) — associação, igreja ou entidade pública. Contém `Withdrawal`s
  e o **estoque** (`Stock`: dicionário `ItemCategory → quantidade`, ex. `Food`, `Clothing`).
- **Beneficiary** (Aggregate Root) — entidade central de rastreabilidade. CPF único no município, e-mail e telefone principal obrigatórios, segundo telefone e endereço de residência opcionais (`Email`/`PhoneNumber`/`Address`); uma mesma pessoa pode ser atendida por vários pontos (`BeneficiaryPointLink`) e cada gestor decide vinculá-la ao seu ponto — só os pontos vinculados a enxergam e editam. Mantém uma lista de
  **vínculos de parentesco** (`BeneficiaryKinship`: beneficiário relacionado + tipo — cônjuge, pai,
  filho, irmão, avô, neto, tio, sobrinho, primo, sogro, genro/nora, agregado ou outro). Todo vínculo é
  espelhado automaticamente no outro lado com o tipo inverso e é apenas informativo: não altera a cota
  de retirada de ninguém. Não existe o conceito de "família". O limite de retiradas não é do beneficiário:
  é a **regra do ponto** (`CollectionPoint.WithdrawalRule`, tipo do ciclo + máximo por família; padrão 3 por ano).
- **Withdrawal** (Entity) — vive dentro de `CollectionPoint`. Referencia o beneficiário por Id.
- **Review** (Aggregate Root) — avaliação pública de um `CollectionPoint` (nota 1–5 + comentário
  opcional), com moderação (`Hide`/`Restore`) e contagem de denúncias (`Report`).
- **StoredFile** (Aggregate Root) — metadado de um arquivo (tipicamente foto de ponto de coleta) em
  object storage S3-compatível; vínculo com o dono via `OwnerType`/`OwnerId` (sem FK) e exclusão como
  soft-delete (`MarkDeleted`). Ver [Object Storage](#object-storage-minios3).

### A regra de equidade

```
RegisterWithdrawal(beneficiary, retiradasNoCicloEmTodosOsPontos, instante):
  - ponto deve estar ativo
  - retiradasNoCicloEmTodosOsPontos < ponto.WithdrawalRule.MaxWithdrawalsPerCycle
```

A contagem (`retiradasNoCicloEmTodosOsPontos`) é calculada pelo handler via
`ICollectionPointRepository.CountBeneficiaryWithdrawalsAsync(beneficiaryId, início, fim)`, que consulta a
tabela `withdrawals` **inteira** (todos os pontos) dentro da janela do ciclo vigente — daí o
compartilhamento municipal do histórico.

---

## Como executar

### Pré-requisitos
- Docker + Docker Compose (a compilação ocorre dentro do container; **não é preciso ter o SDK .NET local**).

### Subir tudo com Docker

```bash
cp .env.example .env          # ajuste as senhas/segredos
docker compose up --build     # sobe postgres + minio (healthcheck) -> api -> pgadmin + frontend
```

- Frontend: http://localhost:3000
- API: http://localhost:8080  (docs: http://localhost:8080/scalar/v1)
- Health: http://localhost:8080/health
- PGAdmin: http://localhost:5050
- Console do MinIO: http://localhost:9001 (credenciais em `MINIO_ROOT_USER`/`MINIO_ROOT_PASSWORD`)
- Admin de seed: `admin@doacao.local` / `Admin@123456` (configurável no `.env`)

> No startup (dev), a API aplica as **migrations** automaticamente, cria as **roles** + o **admin**
> e garante o bucket do MinIO (`StorageBucketInitializer`).

### Rodar localmente (com SDK .NET 9 instalado)

```bash
docker compose up -d postgres
dotnet run --project src/DoacaoApp.Api
```

---

## Migrations

A migration inicial já está versionada em `src/DoacaoApp.Infrastructure/Persistence/Migrations`.

Em **dev**, são aplicadas no startup (`RunMigrationsOnStartup=true`). Em **produção**, prefira aplicar
via CLI no deploy:

```bash
# criar nova migration
dotnet ef migrations add <Nome> \
  -p src/DoacaoApp.Infrastructure -s src/DoacaoApp.Api -o Persistence/Migrations

# aplicar
dotnet ef database update -p src/DoacaoApp.Infrastructure -s src/DoacaoApp.Api
```

Sem SDK local? Rode o EF dentro de um container do SDK:

```bash
docker run --rm -v "$PWD":/src -w /src mcr.microsoft.com/dotnet/sdk:9.0 bash -c \
  "dotnet tool install -g dotnet-ef --version 9.0.0 && export PATH=\$PATH:/root/.dotnet/tools && \
   dotnet ef migrations add <Nome> -p src/DoacaoApp.Infrastructure -s src/DoacaoApp.Api -o Persistence/Migrations"
```

---

## Testes

```bash
# tudo (precisa de Docker para os testes de integração com Testcontainers)
dotnet test

# só unitários (rápidos, sem Docker)
dotnet test tests/DoacaoApp.Domain.Tests
dotnet test tests/DoacaoApp.Application.Tests
```

Cobertura obrigatória implementada:
- **Todos os Value Objects** (casos felizes + inválidos, incl. dígitos verificadores do CPF)
- **Regra de limite de retirada por família** (`CollectionPointWithdrawalTests`)
- **Use cases críticos**: `RegisterWithdrawalHandlerTests`, `RegisterBeneficiaryHandlerTests`
- **Integração** ponta a ponta com PostgreSQL real (`WithdrawalFlowTests`): login → criar ponto →
  família → beneficiário → retirada → bloqueio da 2ª retirada no ciclo.

---

## Documentação da API (Scalar)

Com a API no ar, acesse **`/scalar/v1`**. O documento OpenAPI cru fica em **`/openapi/v1.json`**.
O Scalar exibe o botão **Authorize** (esquema Bearer/JWT) para testar endpoints protegidos.

---

## Endpoints

### Público (anônimo)
| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/public/collection-points` | Lista pontos de coleta ativos (endereço + contato + estoque) |
| GET | `/api/public/collection-points/{pointId}/reviews` | Avaliações visíveis de um ponto |
| POST | `/api/public/collection-points/{pointId}/reviews` | Cria uma avaliação (nota + comentário) |
| POST | `/api/public/reviews/{reviewId}/report` | Denuncia uma avaliação |
| GET | `/health`, `/health/ready` | Health checks |

### Autenticação (Minimal API, com rate limit)
| Método | Rota | Acesso |
|---|---|---|
| POST | `/api/auth/login` | anônimo |
| POST | `/api/auth/refresh` | anônimo (rotaciona o refresh token) |
| POST | `/api/auth/revoke` | anônimo (logout) |
| POST | `/api/auth/register` | **Admin** |

### Gerencial (JWT)
| Método | Rota | Acesso |
|---|---|---|
| POST | `/api/collection-points` | Admin |
| PUT | `/api/collection-points/{id}` | Admin |
| GET | `/api/collection-points/{id}` | Admin, PointManager (gestor só o próprio ponto; inclui estoque e configurações) |
| PUT | `/api/collection-points/{id}/stock` | Admin, PointManager |
| PUT | `/api/collection-points/{id}/stock-settings` | Admin, PointManager (metas, avisos de estoque baixo, categorias acompanhadas e a regra de retirada do ponto; gestor só o próprio ponto) |
| GET | `/api/collection-points/{id}/dashboard` | Admin, PointManager (só o próprio ponto; `?from=&to=` em `yyyy-MM-dd`, padrão o mês atual, máx. 366 dias: retiradas por dia/mês, atendimentos e últimas retiradas) |
| GET | `/api/beneficiaries` | autenticado (Admin: todos; gestor: os do próprio ponto) |
| GET | `/api/beneficiaries/{id}` | autenticado (gestor só vê os do próprio ponto) |
| GET | `/api/beneficiaries/by-cpf/{cpf}` | Admin, PointManager (busca municipal, usada na retirada) |
| POST | `/api/beneficiaries` | Admin, PointManager |
| PUT | `/api/beneficiaries/{id}` | Admin, PointManager (gestor só edita os vinculados ao próprio ponto; os dados valem para todos os pontos) |
| POST | `/api/beneficiaries/{id}/points` | Admin, PointManager (vincula uma pessoa já cadastrada a um ponto; o gestor só vincula ao próprio ponto; 409 se já vinculada) |
| POST | `/api/beneficiaries/{id}/kinships` | Admin, PointManager (vínculo de parentesco informativo, espelhado no outro beneficiário) |
| GET | `/api/beneficiaries/{id}/withdrawals?pointId=` | Admin, PointManager (histórico + saldo do ciclo pela regra do ponto; gestor só consulta pelo próprio ponto) |
| POST | `/api/withdrawals` | Admin, PointManager (informa `categories`, lista de itens da visita; cada item precisa ter quantidade positiva no estoque do ponto; só cestas básicas seguem o limite do ciclo, os demais itens são livres e ficam registrados no histórico) |
| POST | `/api/files` | Admin, PointManager (upload multipart, máx. 20MB) |
| GET | `/api/files/{id}` | autenticado (metadado + URL pré-assinada) |
| GET | `/api/files?ownerType=&ownerId=` | autenticado (arquivos de um dono) |
| DELETE | `/api/files/{id}` | Admin (soft-delete) |
| POST | `/api/reviews/{id}/hide` | Admin (moderação) |
| POST | `/api/reviews/{id}/restore` | Admin (moderação) |

Erros seguem **ProblemDetails** (RFC 9110); validações retornam **ValidationProblemDetails** (400).

---

## Autenticação, roles e o porquê do BCrypt

**Roles**: `Admin`, `PointManager`, `Viewer`.

**Fluxo**: `login`/`register` emitem um **access token** (JWT curto, stateless) + um **refresh token**
(persistido em banco, revogável e rotacionado a cada uso).

### Por que BCrypt explícito (e não o hasher padrão do Identity)?

O hasher padrão do ASP.NET Core Identity usa PBKDF2-HMAC-SHA256. Aqui ele foi **substituído** por uma
implementação explícita de `IPasswordHasher<ApplicationUser>` usando **BCrypt** (`BCryptPasswordHasher`),
porque:

1. **Função adaptativa** — o *work factor* é um parâmetro explícito (custo exponencial, configurável em
   `Security:BCryptWorkFactor`). Conforme o hardware evolui, basta aumentá-lo.
2. **Salt individual por usuário** — gerado automaticamente e **embutido no próprio hash**
   (`$2a$<cost>$<salt+hash>`), dispensando coluna de salt separada.
3. **Resistência a GPU/ASIC** — o agendamento de chaves do Blowfish é intensivo em memória, dificultando
   brute force offline.
4. **Rehash transparente** — ao aumentar o work factor, senhas antigas são re-hasheadas no próximo login
   (`PasswordVerificationResult.SuccessRehashNeeded`).

O registro do hasher é feito **antes** do `AddIdentityCore` (que usa `TryAdd`), garantindo que o BCrypt
prevaleça sobre o padrão.

---

## Observabilidade

- **Serilog** estruturado (Console + arquivo em `logs/`), com `CorrelationId` em todo log.
- **Correlation ID** por request (header `X-Correlation-ID`, reaproveitado ou gerado), devolvido na
  resposta e propagado ao Serilog via `LogContext`.
- **HealthChecks** em `/health` (inclui o PostgreSQL).
- **Rate limiting** (AspNetCoreRateLimit) nas rotas de autenticação — ver seção `IpRateLimiting` no
  `appsettings.json`.

---

## Object Storage (MinIO/S3)

Imagens (ex.: fotos de ponto de coleta) são enviadas via `POST /api/files` e guardadas num bucket
S3-compatível — **MinIO** em dev/self-hosted, **Amazon S3** em produção — através do `AWSSDK.S3`
(`Infrastructure/Storage/S3FileStorage.cs`). Só o metadado (`StoredFile`) fica no Postgres; o binário
vive no bucket. No boot, `StorageBucketInitializer` garante que o bucket configurado existe.

Para migrar de MinIO para Amazon S3: troque `Storage__Provider=AmazonS3`, limpe `Storage__ServiceUrl`/
`Storage__PublicUrl` e use as credenciais AWS reais em `MINIO_ROOT_USER`/`MINIO_ROOT_PASSWORD` (os
mesmos nomes de variável, lidos pelo `IAmazonS3` configurado — ver `.env.example`).

---

## Variáveis de ambiente (resumo)

Veja `.env.example`. Principais:

| Variável | Descrição |
|---|---|
| `ConnectionStrings__Database` | Connection string do PostgreSQL |
| `Jwt__Secret` | Segredo do JWT (≥ 32 chars; use secret manager em prod) |
| `Security__BCryptWorkFactor` | Custo do BCrypt (padrão 12) |
| `RunMigrationsOnStartup` | Aplicar migrations no boot (dev) |
| `SeedData__Enabled` / `SeedData__Admin*` | Seed do usuário admin |
| `MINIO_ROOT_USER` / `MINIO_ROOT_PASSWORD` | Credenciais do MinIO (ou access/secret key da AWS em prod) |
| `Storage__Provider` | `MinIO` ou `AmazonS3` |
| `Storage__ServiceUrl` / `Storage__PublicUrl` | Endpoint interno (API→storage) / público (navegador→URLs pré-assinadas) |
| `Storage__Bucket` / `Storage__Region` | Bucket e região do object storage |
| `Storage__PresignExpiryMinutes` | Validade das URLs pré-assinadas de download |

---

## Débito técnico conhecido

Auditoria de 2026-08-10 corrigiu os achados de alta severidade (lockout de login, rate limit
contornável, upload sem checagem de magic bytes, segredo padrão em produção, backup/restore
inexistente, containers como root). O que ficou como débito **aberto**, de propósito, por exigir
mais que uma correção pontual:

- **Token de auth no `localStorage`** (`frontend/src/models/api/tokenStorage.ts`) — access e
  refresh token ficam em `localStorage`, expostos a roubo via XSS. Migrar para cookie `HttpOnly` +
  `SameSite` é a mitigação padrão, mas exige mudança coordenada: backend emitindo `Set-Cookie` e
  CORS com credentials, frontend removendo a leitura direta do token e passando `withCredentials`
  no Axios, e uma estratégia de CSRF. Não é uma correção isolada — é uma mudança de arquitetura de
  autenticação.
- **Cobertura de teste incompleta na Application layer**: sem teste ainda para
  `CreateCollectionPoint`/`UpdateCollectionPoint`, `FindBeneficiaryByCpf`/
  `GetBeneficiaryById`/`ListBeneficiaries`, todo o módulo `Reviews` (`CreateReview`, `ModerateReview`,
  `ListReviewsForPoint`) e a maior parte de `Storage` (`GetStoredFile`, `ListFilesByOwner`,
  `DeleteFile`). `UploadImage`/`StoredFile.Create`/`MarkDeleted` e todo o módulo `Authentication`
  ganharam teste nesta auditoria.
- **`DoacaoApp.IntegrationTests` cobre um único fluxo** (`WithdrawalFlowTests`) — faltam upload de
  arquivo, moderação/denúncia de review e atualização de estoque ponta a ponta. O `FakeFileStorage`
  já existe no projeto de testes mas não é exercitado por nenhum `[Fact]` ainda.
- **`IdentityService` (Infrastructure) não tem projeto de teste unitário próprio** — a lógica de
  lockout adicionada nesta auditoria (`IsLockedOutAsync`/`AccessFailedAsync`) só é exercitada pelos
  testes de integração, que dependem de Docker. Se a lógica de identidade crescer, vale um
  `DoacaoApp.Infrastructure.Tests` com `UserManager` mockado (seus métodos são `virtual`).
- **Tag do MinIO fixada manualmente** (`RELEASE.2025-04-22T22-12-26Z` em `docker-compose.yml`) —
  escolhida para sair do `latest` flutuante, mas não conferida contra o changelog oficial do MinIO
  no momento da troca. Revalide antes de ir para produção.