# Doação App — Gestão de Pontos de Doação/Coleta

Sistema de gerenciamento de pontos de doação/coleta (cesta básica, roupas, etc.) com foco em
**rastreabilidade familiar** e **controle de equidade** entre beneficiários.

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
├── docker-compose.yml   # orquestra postgres + api + frontend (+ pgadmin)
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
| Aggregates / Aggregate Root | `CollectionPoint`, `Family` |
| Value Objects | `Cpf`, `Email`, `PhoneNumber`, `Address`, `ContactInfo`, `WithdrawalCycle` |
| Strongly-typed IDs | `CollectionPointId`, `FamilyId`, `BeneficiaryId`, `WithdrawalId` |
| Domain Events | `CollectionPointCreated`, `WithdrawalRegistered`, `BeneficiaryLinkedToFamily` |
| Result Pattern | `ErrorOr<T>` em todos os fluxos de negócio |
| CQRS | Commands/Queries + Handlers (MediatR) |
| Repository + Unit of Work | `ICollectionPointRepository`, `IFamilyRepository`, `IUnitOfWork` |

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
- **xUnit** + **FluentAssertions** + **Moq**; **Testcontainers** (PostgreSQL real)

---

## Estrutura de pastas

```
doacao-app/
├── DoacaoApp.sln
├── Directory.Build.props          # props comuns (net9.0, nullable, langversion)
├── Directory.Packages.props       # Central Package Management (versões)
├── Dockerfile                     # multi-stage (sdk build -> aspnet alpine)
├── docker-compose.yml             # api + postgres + pgadmin
├── docker-compose.override.yml    # ajustes de dev
├── .env.example                   # variáveis (copiar para .env)
├── src/
│   ├── DoacaoApp.Domain/
│   │   ├── Common/                # Entity, AggregateRoot, IDomainEvent, IUnitOfWork
│   │   ├── ValueObjects/          # Cpf, Email, PhoneNumber, Address, ContactInfo, WithdrawalCycle
│   │   ├── CollectionPoints/      # CollectionPoint (root), Withdrawal, eventos, repo
│   │   ├── Families/              # Family (root), Beneficiary, eventos, repo
│   │   └── Errors/                # DomainErrors (catálogo de erros)
│   ├── DoacaoApp.Application/
│   │   ├── Common/                # Behaviors (ValidationBehavior), Abstractions
│   │   ├── Authentication/        # Commands (Register/Login/Refresh), Roles, AuthErrors
│   │   ├── CollectionPoints/      # Commands, Queries, Contracts, EventHandlers
│   │   └── Families/              # Commands, Queries, Contracts, EventHandlers
│   ├── DoacaoApp.Infrastructure/
│   │   ├── Persistence/           # AppDbContext, Configurations, Repositories,
│   │   │                          # Interceptors, Converters, Seed, Migrations
│   │   ├── Identity/              # ApplicationUser, BCryptPasswordHasher, IdentityService
│   │   └── Authentication/        # JwtOptions, JwtTokenGenerator
│   └── DoacaoApp.Api/
│       ├── Controllers/           # CollectionPoints, Families, Withdrawals
│       ├── Endpoints/             # Auth, PublicCollectionPoints (Minimal APIs)
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

- **CollectionPoint** (Aggregate Root) — associação, igreja ou entidade pública. Contém `Withdrawal`s.
- **Family** (Aggregate Root) — entidade central de rastreabilidade. Define o `WithdrawalCycle`
  (tipo + máximo de retiradas por ciclo) e contém `Beneficiary`s.
- **Beneficiary** (Entity) — vive dentro de `Family`. CPF único no município.
- **Withdrawal** (Entity) — vive dentro de `CollectionPoint`. Referencia família/beneficiário por Id.

### A regra de equidade

```
RegisterWithdrawal(family, beneficiary, retiradasNoCicloEmTodosOsPontos, instante):
  - ponto deve estar ativo
  - beneficiário deve pertencer à família
  - retiradasNoCicloEmTodosOsPontos < family.Cycle.MaxWithdrawalsPerCycle
```

A contagem (`retiradasNoCicloEmTodosOsPontos`) é calculada pelo handler via
`ICollectionPointRepository.CountFamilyWithdrawalsAsync(familyId, início, fim)`, que consulta a
tabela `withdrawals` **inteira** (todos os pontos) dentro da janela do ciclo vigente — daí o
compartilhamento municipal do histórico.

---

## Como executar

### Pré-requisitos
- Docker + Docker Compose (a compilação ocorre dentro do container; **não é preciso ter o SDK .NET local**).

### Subir tudo com Docker

```bash
cp .env.example .env          # ajuste as senhas/segredos
docker compose up --build     # sobe postgres (healthcheck) -> api -> pgadmin
```

- API: http://localhost:8080  (docs: http://localhost:8080/scalar/v1)
- Health: http://localhost:8080/health
- PGAdmin: http://localhost:5050
- Admin de seed: `admin@doacao.local` / `Admin@123456` (configurável no `.env`)

> No startup (dev), a API aplica as **migrations** automaticamente e cria as **roles** + o **admin**.

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
| GET | `/api/public/collection-points` | Lista pontos de coleta ativos (endereço + contato) |
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
| POST | `/api/families` | Admin, PointManager |
| GET | `/api/families/{id}` | autenticado |
| POST | `/api/families/{id}/beneficiaries` | Admin, PointManager |
| GET | `/api/families/{id}/withdrawals` | autenticado (histórico + saldo do ciclo) |
| POST | `/api/withdrawals` | Admin, PointManager |

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

## Variáveis de ambiente (resumo)

Veja `.env.example`. Principais:

| Variável | Descrição |
|---|---|
| `ConnectionStrings__Database` | Connection string do PostgreSQL |
| `Jwt__Secret` | Segredo do JWT (≥ 32 chars; use secret manager em prod) |
| `Security__BCryptWorkFactor` | Custo do BCrypt (padrão 12) |
| `RunMigrationsOnStartup` | Aplicar migrations no boot (dev) |
| `SeedData__Enabled` / `SeedData__Admin*` | Seed do usuário admin |
```