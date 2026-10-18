# Dooa — Gestão de Pontos de Doação/Coleta

Sistema para gerenciar pontos de doação/coleta (cestas básicas, roupas, calçados…) com **rastreabilidade
por beneficiário** e **controle de equidade** entre os pontos de um município.

> **Regra central:** todos os pontos de coleta do município compartilham o histórico de retiradas. Se um
> beneficiário já retirou cestas no Ponto A neste ciclo, o Ponto B é impedido de liberar uma retirada
> além do limite — a verificação soma o histórico de **todos** os pontos.

Além do controle de retiradas, o sistema tem um lado público e colaborativo: mapa de pontos, avaliações,
mural de avisos e promessas de doação (ver [Colaboração](#colaboração-público-e-rede-de-gestores)).

| Camada | Tecnologia |
|---|---|
| Backend | .NET 9 / C# 13, Clean Architecture + DDD + CQRS (MediatR), EF Core 9 + PostgreSQL 16 |
| Frontend | React 19 + Vite 8 + TypeScript, MVVM, Tailwind v4, mapa OpenStreetMap — ver [frontend/README.md](frontend/README.md) |
| Infra | Docker Compose, MinIO/S3 (imagens), nginx de borda + certbot (TLS em produção) |

## Início rápido

Pré-requisito: **Docker + Docker Compose** (não é preciso SDK .NET nem Node locais).

```bash
cp .env.example .env     # ajuste senhas e segredos
make dev                 # stack completa com hot reload do frontend
```

| Serviço | URL |
|---|---|
| Frontend (dev, Vite) | http://localhost:5445 |
| API | http://localhost:8080 — docs em `/scalar/v1`, health em `/health` |
| PGAdmin | http://localhost:5050 |
| Console do MinIO | http://localhost:9001 |

Login inicial (seed de dev, configurável no `.env`): `admin@doacao.local` / `Admin@123456`.

Sem `make`, o equivalente ao modo "produção local" é `docker compose up --build` (frontend em nginx na
porta 3000). No startup em dev a API aplica as migrations, cria roles + admin e garante o bucket do MinIO.

### Comandos do dia a dia

Rode sempre da raiz; `make help` lista tudo.

| Comando | O que faz |
|---|---|
| `make dev` / `make dev-reset` | Reinicia a stack de desenvolvimento (o `-reset` apaga banco e arquivos e re-seeda) |
| `make dev-logs` | Logs do Vite e da API |
| `make prod` / `make prod-logs` | Stack de produção (nginx de borda + certbot) — ver [docs/deploy-hostinger.md](docs/deploy-hostinger.md) |
| `make test` | Testes unitários (Domain + Application) em container do SDK |
| `make test-integration` | Testes de integração (exigem Docker no host) |
| `make migration name=Nome` | Nova migration do EF Core |
| `make backup` / `make restore-db file=…` | Backup/restauração de banco e arquivos |
| `make down` | Para a stack mantendo os dados |

## Sumário

- [Estrutura do monorepo](#estrutura-do-monorepo)
- [Arquitetura](#arquitetura)
- [Modelo de domínio](#modelo-de-domínio)
- [Colaboração (público e rede de gestores)](#colaboração-público-e-rede-de-gestores)
- [Desenvolvimento](#desenvolvimento)
- [API e endpoints](#api-e-endpoints)
- [Autenticação e roles](#autenticação-e-roles)
- [Observabilidade](#observabilidade)
- [Object Storage](#object-storage-minios3)
- [Variáveis de ambiente](#variáveis-de-ambiente)
- [Documentação complementar](#documentação-complementar)
- [Débito técnico conhecido](#débito-técnico-conhecido)

## Estrutura do monorepo

```
.
├── backend/                  # solution .NET (src/, tests/, Dockerfile)
│   ├── src/
│   │   ├── DoacaoApp.Domain/          # agregados, Value Objects, eventos, erros, interfaces de repositório
│   │   ├── DoacaoApp.Application/     # casos de uso CQRS (Command/Query + Validator + Handler)
│   │   ├── DoacaoApp.Infrastructure/  # EF Core, repositórios, Identity/JWT, storage S3, seed, migrations
│   │   └── DoacaoApp.Api/             # Controllers, Minimal APIs, middlewares, Scalar/OpenAPI
│   └── tests/
│       ├── DoacaoApp.Domain.Tests/        # Value Objects e regras de domínio
│       ├── DoacaoApp.Application.Tests/   # handlers (Moq)
│       └── DoacaoApp.IntegrationTests/    # ponta a ponta com PostgreSQL real (Testcontainers)
├── frontend/                 # SPA React (MVVM)
├── docs/                     # documentação do projeto de extensão e deploy
├── docker-compose.yml        # base: postgres, minio, api, frontend, pgadmin
├── docker-compose.override.yml  # portas expostas (aplicado automaticamente)
├── docker-compose.dev.yml    # frontend com Vite + HMR
├── docker-compose.prod.yml   # produção: só o nginx de borda publica portas (80/443)
├── edge/                     # nginx de borda (TLS, HSTS, limites, IP real) + certbot
├── Makefile                  # atalhos (make help)
└── .env.example / .env.prod.example
```

Cada módulo do Domain/Application segue a mesma divisão: `CollectionPoints`, `Beneficiaries`, `Reviews`,
`Donations`, `Notices`, `Activity` (só Application), `Storage` e `Authentication` (só Application/Infrastructure).

## Arquitetura

Dependências apontam sempre para dentro:

```
API ─► Infrastructure ─► Application ─► Domain
```

- **Domain** — sem dependência de framework (só `ErrorOr` e `MediatR.Contracts`).
- **Application** — casos de uso via **MediatR**, validação com **FluentValidation** (`ValidationBehavior`),
  escopo por ponto (`CollectionPointScopeBehavior`) e **Result Pattern** (`ErrorOr<T>`: o fluxo de negócio
  nunca lança exception).
- **Infrastructure** — EF Core + Npgsql, ASP.NET Core Identity com **BCrypt**, JWT + refresh tokens,
  dispatch de Domain Events após o commit (interceptor de `SaveChanges`), `AWSSDK.S3`.
- **API** — Controllers (domínios gerenciais) + Minimal APIs (auth e parte pública), Correlation ID,
  exception handler global, rate limiting, health checks, Scalar.

| Padrão | Onde |
|---|---|
| Aggregate Roots | `CollectionPoint`, `Beneficiary`, `Review`, `Donation`, `PointNotice`, `StoredFile` |
| Value Objects (`Create` → `ErrorOr`) | `Cpf`, `Email`, `PhoneNumber`, `Address`, `ContactInfo`, `WithdrawalCycle` |
| Strongly-typed IDs | `CollectionPointId`, `BeneficiaryId`, `WithdrawalId`, `StockItemId`… |
| Domain Events | `CollectionPointCreated`, `WithdrawalRegistered`, `BeneficiaryRegistered`… |
| Repository + Unit of Work | `ICollectionPointRepository`, `IBeneficiaryRepository`, `IUnitOfWork` |

Convenções de estilo e de mapeamento EF estão em [CLAUDE.md](CLAUDE.md).

## Modelo de domínio

- **CollectionPoint** (Aggregate Root) — associação, igreja ou entidade pública. Contém, como entidades
  internas: `Withdrawal`s, **itens de estoque** (`StockItem`: nome, unidade, categoria, quantidade, meta,
  limite de alerta, se é acompanhado), **categorias** (`Category`, catálogo próprio do ponto, exibido no
  mapa) e **receitas de cesta** (`BasketTemplate`: quanto de cada item compõe uma cesta). Guarda também a
  regra de retirada (`WithdrawalRule`), descrição, horário e necessidades.
- **Beneficiary** (Aggregate Root) — entidade central de rastreabilidade. CPF único no município; e-mail e
  telefone principal obrigatórios. Uma pessoa pode ser atendida por vários pontos (`BeneficiaryPointLink`),
  e só os pontos vinculados a enxergam e editam. Vínculos de parentesco (`BeneficiaryKinship`) são só
  informativos e espelhados no outro lado com o tipo inverso. Não existe o conceito de "família" e o
  beneficiário não guarda limite algum.
- **Withdrawal** (Entity do `CollectionPoint`) — uma visita. Pode conter uma **cesta básica** (receita +
  quantidade, com baixa automática no estoque) e/ou **itens livres** (roupas, calçados…). Referencia o
  beneficiário por Id.
- **Review**, **Donation**, **PointNotice**, **StoredFile** — Aggregate Roots próprios, ligados ao ponto
  por Id (sem navegação).

### A regra de equidade

```
RegisterWithdrawal(beneficiary, retiradasComCestaNoCicloEmTodosOsPontos, instante, ...):
  - o ponto deve estar ativo
  - os itens retirados precisam existir no estoque do próprio ponto, em quantidade suficiente
  - se a retirada inclui cesta básica:
      retiradasComCestaNoCiclo < ponto.WithdrawalRule.MaxWithdrawalsPerCycle
```

O agregado decide com base na contagem **recebida por parâmetro**; quem a calcula é o
`RegisterWithdrawalHandler`, via `ICollectionPointRepository.CountBeneficiaryWithdrawalsAsync(beneficiaryId,
início, fim)`, que varre a tabela `withdrawals` **inteira** (todos os pontos) na janela do ciclo vigente.
Assim a regra é testável sem banco e respeita as fronteiras de agregado.

- O **histórico** é somado entre todos os pontos, mas o **limite aplicado** é o do ponto onde a retirada
  acontece (`WithdrawalCycle`: tipo do ciclo + máximo; padrão 3 por ano).
- Só retiradas com cesta básica contam para o limite; itens livres ficam apenas no histórico.
- Só a retirada **mais recente** do beneficiário (em qualquer ponto) pode ser corrigida, e só pelo ponto que a fez.

## Colaboração (público e rede de gestores)

| Recurso | Quem | Descrição |
|---|---|---|
| Mapa público | visitante | Pontos ativos com endereço, contato, horário, necessidades e avaliação |
| Avaliações | visitante / Admin | Nota 1–5 + comentário, com denúncia; moderação por ocultação lógica |
| Mural de avisos | ponto → comunidade ou rede | `Audience` `Public` (visível no mapa) ou `Managers` (só a rede) |
| "Quero doar" | visitante → ponto | Doação prometida; o gestor confirma (`receive`, soma ao estoque, tudo ou nada) ou cancela |
| Feed de atividade | gestor | Doações, avisos, avaliações e alertas de estoque do ponto (sem tabela própria) |

O mapeamento destes recursos ao Modelo 3C está em [docs/mapeamento-3c.md](docs/mapeamento-3c.md).

## Desenvolvimento

O host só precisa de Docker. Compilação, testes e migrations rodam em containers do SDK .NET 9 com volume
NuGet persistente (`doacao-nuget`) — o `Makefile` já monta tudo. O SDK .NET 10 do host serve apenas ao editor.

```bash
make build                         # solution inteira (src + testes)
make test                          # Domain + Application
make test-filter FILTER=CpfTests   # um filtro no Domain
make test-integration              # Testcontainers: exige acesso ao socket do Docker
make migration name=NomeDaMigration
```

**TDD em regra de negócio:** o teste que falha vem antes da implementação, em `Domain.Tests` ou
`Application.Tests`. Cobertura implementada:

- todos os Value Objects (casos válidos e inválidos, incl. dígitos verificadores do CPF);
- limite de retirada por ciclo e regras de estoque no agregado;
- handlers críticos (retirada, cadastro de beneficiário, doações, avisos, autenticação, upload);
- integração ponta a ponta (`WithdrawalFlowTests`, `CommunityFlowTests`, `ExactLocationTests`).

O CI (`.github/workflows/ci.yml`) roda build + os três projetos de teste no backend e lint + testes + build no frontend.

### Migrations

Versionadas em `backend/src/DoacaoApp.Infrastructure/Persistence/Migrations`. Em **dev** são aplicadas no
startup (`RunMigrationsOnStartup=true`). Crie novas com `make migration name=<Nome>` (os arquivos saem como
`root`; `make clean` devolve a propriedade). Em produção a flag também está ligada no `docker-compose.prod.yml`.

## API e endpoints

Com a API no ar: documentação interativa em **`/scalar/v1`** (botão **Authorize** para JWT) e OpenAPI cru em
`/openapi/v1.json`. Erros seguem **ProblemDetails** (RFC 9110); validações retornam
**ValidationProblemDetails** (400).

### Público (anônimo)

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/public/collection-points` | Pontos ativos (endereço, contato, estoque público, avaliação) |
| GET | `/api/public/collection-points/{pointId}/reviews` | Avaliações visíveis |
| GET | `/api/public/sponsors` | Patrocinadores ativos, do Diamante à Prata (seção "Patrocinadores" da página inicial) |
| POST | `/api/public/collection-points/{pointId}/reviews` | Cria avaliação |
| POST | `/api/public/reviews/{reviewId}/report` | Denuncia avaliação |
| GET | `/api/public/collection-points/{pointId}/notices` | Avisos públicos do mural |
| POST | `/api/public/collection-points/{pointId}/donations` | Avisa que pretende doar |
| GET | `/health`, `/health/ready` | Health checks |

### Autenticação (rate limit)

| Método | Rota | Acesso |
|---|---|---|
| POST | `/api/auth/login` | anônimo |
| POST | `/api/auth/refresh` | anônimo (rotaciona o refresh token) |
| POST | `/api/auth/revoke` | anônimo (logout) |
| POST | `/api/auth/forgot-password` | anônimo (envia o link de redefinição; resposta igual exista ou não a conta) |
| POST | `/api/auth/reset-password` | anônimo (nova senha com o token do link) |
| POST | `/api/auth/register` | Admin |
| GET / PUT | `/api/auth/users`, `/api/auth/users/{id}` | Admin, Gestor (só usuários do próprio ponto) — e-mail, nome, telefone, papel e ponto; trocar e-mail, papel ou ponto encerra as sessões do usuário |
| POST | `/api/auth/users/{id}/deactivate` | Admin |

### Gerencial (JWT)

"Gestor" = `PointManager`, que só age no **próprio ponto**; `Admin` age em qualquer um.

| Método | Rota | Acesso / observações |
|---|---|---|
| GET | `/api/collection-points`, `/{id}` | Admin, Gestor (detalhe com estoque e configurações) |
| POST | `/api/collection-points` | Admin |
| PUT | `/api/collection-points/{id}` | Admin, Gestor — todos os dados do ponto (nome, endereço, contato, horários, necessidades, foto) |
| GET | `/api/collection-points/{id}/dashboard` | Admin, Gestor (`?from=&to=` em `yyyy-MM-dd`; padrão o mês atual, máx. 366 dias) |
| POST / PUT / DELETE | `/api/collection-points/{id}/stock-items[/{itemId}]` | Admin, Gestor — catálogo de itens do ponto |
| PUT | `/api/collection-points/{id}/stock-items/quantities` | Admin, Gestor — ajuste de quantidades |
| PUT | `/api/collection-points/{id}/stock-items/settings` | Admin, Gestor — metas, alertas e itens acompanhados |
| POST / PUT / DELETE | `/api/collection-points/{id}/categories[/{categoryId}]` | Admin, Gestor — categorias do ponto |
| POST / PUT / DELETE | `/api/collection-points/{id}/basket-templates[/{templateId}]` | Admin, Gestor — receitas de cesta |
| PUT | `/api/collection-points/{id}/withdrawal-rule` | Admin, Gestor — tipo e máximo do ciclo |
| GET / POST / PUT / DELETE | `/api/sponsors[/{id}]` | Admin — patrocinadores mensais (nome, categoria Diamante/Ouro/Prata, site, logo, exibido ou oculto) |
| GET | `/api/beneficiaries`, `/{id}` | autenticado (Admin: todos; gestor: os vinculados ao seu ponto) |
| POST | `/api/beneficiaries/lookup-by-cpf` (`{ "cpf": "..." }`) | Admin, Gestor — busca municipal no balcão; CPF no corpo (fora de logs). Sem vínculo com o ponto devolve só nome abreviado + CPF mascarado (`beneficiary: null`) e grava log de auditoria |
| POST / PUT | `/api/beneficiaries`, `/{id}` | Admin, Gestor (os dados valem para todos os pontos) |
| POST | `/api/beneficiaries/{id}/points` | Admin, Gestor — vincula pessoa já cadastrada a um ponto (409 se já vinculada; log de auditoria) |
| POST | `/api/beneficiaries/{id}/kinships` | Admin, Gestor — parentesco informativo, espelhado (gestor: só de beneficiário vinculado ao seu ponto) |
| GET | `/api/beneficiaries/{id}/withdrawals?pointId=` | Admin, Gestor — histórico + saldo do ciclo pela regra do ponto (gestor: só de beneficiário vinculado ao seu ponto) |
| POST | `/api/withdrawals` | Admin, Gestor — cesta (receita + quantidade) e/ou itens livres; beneficiário ativo e vinculado ao ponto; `occurredAtUtc` opcional, nunca no futuro e, para gestor, no máximo 7 dias atrás |
| PUT | `/api/withdrawals/{id}` | Admin, Gestor — só a retirada mais recente do beneficiário (ativo e vinculado ao ponto) |
| GET | `/api/collection-points/{id}/donations` | Admin, Gestor |
| POST | `…/donations/{donationId}/receive` · `/cancel` | Admin, Gestor (409 se já fechada) |
| GET | `/api/notices` | Admin, Gestor — mural da rede |
| POST / DELETE | `/api/collection-points/{id}/notices[/{noticeId}]` | Admin, Gestor (`audience`: `Public` ou `Managers`; DELETE arquiva) |
| GET | `/api/collection-points/{id}/activity` | Admin, Gestor — feed de atividade |
| POST | `/api/files` · GET `/api/files/{id}` · GET `/api/files?ownerType=&ownerId=` | upload (Admin, Gestor; máx. 20 MB; extensão do objeto vem do tipo validado) e consulta (Admin, URL pré-assinada) |
| DELETE | `/api/files/{id}` | Admin (soft-delete) |
| POST | `/api/reviews/{id}/hide` · `/restore` | Admin (moderação) |

## Autenticação e roles

**Roles:** `Admin`, `PointManager`, `Viewer`.

`login`/`register` emitem um **access token** (JWT curto, stateless) e um **refresh token** persistido em
banco, revogável e rotacionado a cada uso. O login tem lockout após tentativas falhas.

**Esqueci minha senha:** a tela `/forgot-password` pede um link, que chega por e-mail (Resend) apontando para
`/reset-password#token=...`. O token vai no fragmento da URL, então nunca chega a logs de proxy/CDN; no banco
fica só o hash SHA-256. Cada link vale 1 hora (`Security__PasswordResetTokenMinutes`), serve uma única vez e é
invalidado por um pedido mais novo; pedidos para o mesmo usuário têm intervalo mínimo de 60 s
(`Security__PasswordResetCooldownSeconds`). Redefinir a senha encerra todas as sessões e desfaz o bloqueio por
tentativas erradas, mas usuários desativados não recebem link. O e-mail sai por uma fila em segundo plano: a
resposta não muda de tempo nem de conteúdo conforme o e-mail exista, e uma falha do Resend só vai para o log.

### Por que BCrypt explícito (e não o hasher padrão do Identity)?

O padrão do ASP.NET Core Identity é PBKDF2-HMAC-SHA256. Aqui ele foi substituído por `BCryptPasswordHasher`
porque:

1. **Função adaptativa** — o *work factor* é explícito (`Security:BCryptWorkFactor`) e pode subir com o hardware.
2. **Salt por usuário** embutido no hash (`$2a$<cost>$<salt+hash>`), sem coluna extra.
3. **Resistência a GPU/ASIC** — o agendamento de chaves do Blowfish é intensivo em memória.
4. **Rehash transparente** — senhas antigas são re-hasheadas no próximo login (`SuccessRehashNeeded`).

O hasher é registrado **antes** de `AddIdentityCore` (que usa `TryAdd`) para prevalecer sobre o padrão.

## SEO e prévias de link

- **Páginas renderizadas pela API:** o nginx do frontend manda `/`, `/pontos/{id}/{slug}` e `/sitemap.xml` para
  `/api/public/seo/...`. A API lê o `index.html` do build (`Seo__IndexHtmlUrl`, padrão `http://frontend:8080/index.html`)
  e devolve a página com título, descrição, `canonical`, Open Graph/Twitter, JSON-LD (`ItemList` no início, `Place`
  em cada ponto) e o conteúdo em `<noscript>`. O app React carrega normalmente por cima. Se a API falhar, o nginx
  serve o `index.html` estático.
- **Página de cada ponto:** `/pontos/{id}/{slug}`. Um slug desatualizado redireciona com 301 para o atual, e o
  `og:image` aponta para `/api/public/collection-points/{id}/image`, um endereço fixo que redireciona para uma URL
  pré-assinada nova.
- **Fora do índice:** `/login`, `/forgot-password`, `/reset-password` e `/app/*` saem com `X-Robots-Tag: noindex`.
  Caminhos inexistentes respondem 404 de verdade.
- **Domínio canônico:** `https://${APP_DOMAIN}`. O `www` redireciona com 301 (certificado próprio no certbot).
- `Seo__SiteUrl` (produção: `https://${APP_DOMAIN}`) é a base de canonical, `og:url` e sitemap.

## Observabilidade

- **Serilog** estruturado (console + arquivo em `logs/`), com `CorrelationId` em todo log.
- **Correlation ID** por request (`X-Correlation-ID`, reaproveitado ou gerado), devolvido na resposta.
- **Health checks** em `/health` e `/health/ready` (inclui o PostgreSQL).
- **Rate limiting** por IP real (após `ForwardedHeaders`) nas rotas de autenticação, nas escritas públicas
  (avaliações e doações: 10/10 min; denúncias: 20/10 min) e na busca por CPF (60/10 min) — seção
  `IpRateLimiting` do `appsettings.json`. Regras com curinga de id (`/*/`) somam todas as rotas num só contador.

## Object Storage (MinIO/S3)

Imagens (ex.: foto do ponto) sobem por `POST /api/files` para um bucket S3-compatível — **MinIO** em
dev/self-hosted, **Amazon S3** em produção (`AWSSDK.S3`, `Infrastructure/Storage/S3FileStorage.cs`). O
Postgres guarda só o metadado (`StoredFile`); o binário vive no bucket, e o upload valida o tipo pelos
*magic bytes*. No boot, `StorageBucketInitializer` garante que o bucket existe.

Para migrar do MinIO para o S3: `Storage__Provider=AmazonS3`, limpe `Storage__ServiceUrl`/`Storage__PublicUrl`
e use as credenciais AWS reais em `MINIO_ROOT_USER`/`MINIO_ROOT_PASSWORD` (mesmos nomes, lidos pelo `IAmazonS3`).

## Variáveis de ambiente

Modelo completo em `.env.example` (dev) e `.env.prod.example` (produção; **nunca versione** o `.env.prod`).

| Variável | Descrição |
|---|---|
| `ConnectionStrings__Database` | Connection string do PostgreSQL |
| `Jwt__Secret` | Segredo do JWT (≥ 32 chars; gere com `openssl rand -base64 48`) |
| `Jwt__AccessTokenHours` / `Jwt__RefreshTokenHours` | Validade dos tokens |
| `Security__BCryptWorkFactor` | Custo do BCrypt (padrão 12) |
| `Email__ResendApiKey` | Chave da API do Resend; vazia, os e-mails não saem (só aparecem no log da API) |
| `Email__FromAddress` | Remetente, num domínio verificado no Resend (ex.: `Dooa <nao-responda@dooa.com.br>`) |
| `Email__AppBaseUrl` | Base dos links enviados por e-mail (em produção, `https://${APP_DOMAIN}`) |
| `RunMigrationsOnStartup` | Aplica migrations no boot |
| `SeedData__Enabled` / `SeedData__Admin*` | Seed do usuário admin (em produção, só no primeiro boot) |
| `SeedData__DemoData` | Cria os pontos de coleta de demonstração (só dev; forçado `false` em produção) |
| `MINIO_ROOT_USER` / `MINIO_ROOT_PASSWORD` | Credenciais do MinIO (ou chaves AWS) |
| `Storage__Provider` | `MinIO` ou `AmazonS3` |
| `Storage__ServiceUrl` / `Storage__PublicUrl` | Endpoint interno (API→storage) / público (navegador→URL pré-assinada) |
| `Storage__Bucket` / `Storage__Region` / `Storage__PresignExpiryMinutes` | Bucket, região e validade das URLs |
| `APP_DOMAIN` / `STORAGE_DOMAIN` / `ACME_EMAIL` | Domínios e e-mail do Let's Encrypt (produção) |

## Documentação complementar

Veja o índice em [docs/README.md](docs/README.md): requisitos, UML, mapeamento 3C, usabilidade, validação com
usuários e [deploy em VPS](docs/deploy-hostinger.md).

## Débito técnico conhecido

Pontos abertos de propósito, por exigirem mais que uma correção pontual:

- **Token de auth no `localStorage`** (`frontend/src/models/api/tokenStorage.ts`) — exposto a roubo via XSS.
  A mitigação padrão (cookie `HttpOnly` + `SameSite`) exige mudança coordenada: backend emitindo
  `Set-Cookie` e CORS com credentials, frontend com `withCredentials` e uma estratégia de CSRF.
- **Cobertura da Application layer incompleta** — por exemplo `CreateCollectionPoint`/`UpdateCollectionPoint`,
  consultas de beneficiário, o módulo `Reviews` e a maior parte de `Storage` ainda não têm teste.
- **Integração cobre poucos fluxos** — faltam upload de arquivo, moderação/denúncia de review e atualização de
  estoque ponta a ponta (o `FakeFileStorage` existe mas nenhum `[Fact]` o exercita).
- **`IdentityService` sem teste unitário próprio** — o lockout só é exercitado pelos testes de integração. Se a
  lógica crescer, vale um `DoacaoApp.Infrastructure.Tests` com `UserManager` mockado.
- **Tag do MinIO fixada manualmente** (`docker-compose.yml`) — revalide contra o changelog oficial antes de produção.
