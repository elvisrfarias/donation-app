# Dooa — Frontend

SPA do Dooa: mapa público de pontos de coleta (OpenStreetMap) + área de gestão
(pontos, estoque, beneficiários, retiradas com a regra de equidade, doações, mural de avisos e usuários).
Visão geral do sistema e da API: [README da raiz](../README.md).

## Stack

- **React 19** + **Vite 8** + **TypeScript** (strict)
- **Tailwind CSS v4** (configuração via CSS, `@theme`) + **shadcn/ui** sobre **Radix UI**
  (checkbox, dialog, label, popover, select) e **cmdk**
- **React Router 7** (rotas com code-splitting por `lazy`)
- **TanStack Query 5** (estado de servidor) + **Zustand 5** (autenticação)
- **TanStack Form** + **Zod 4** (formulários e validação) via o `FormComponent` único,
  config-driven — não usamos `react-hook-form`
- **TanStack Table** (`DataTable` reutilizável — sort/filtro por coluna/paginação)
- **Recharts** (gráficos do dashboard)
- **Axios** (cliente HTTP com refresh transparente de JWT)
- **react-leaflet 5** + **Leaflet** sobre tiles do **OpenStreetMap**
- Geocodificação via **Nominatim** (OSM) e busca de endereço por CEP via **BrasilAPI**,
  ambos com cache em `localStorage`

## Arquitetura — MVVM

A separação é estrita: a **View** nunca chama serviços diretamente; ela consome um
**ViewModel** (hook), que orquestra o **Model** (tipos + serviços de dados).

```
src/
├── components/ui/       # primitivas shadcn/ui geradas pela CLI (chart, table, dialog, select…)
│                        # — não editar à mão, regenerar via `pnpm dlx shadcn@latest add <componente>`
├── models/              # MODEL — sem React
│   ├── types/           # tipos de domínio (auth, collectionPoint, beneficiary, withdrawal, review, geo)
│   ├── api/             # httpClient (axios + refresh de JWT), problemDetails, tokenStorage
│   └── services/        # auth, collectionPoint, beneficiary, withdrawal, review, geocoding, cep
├── viewmodels/          # VIEWMODEL — hooks (estado + comandos) + schemas.ts (Zod)
│                        # useAuth, useLogin, useCollectionPoints(Map), useBeneficiaries,
│                        # useRegisterWithdrawal, useReviews, useDonations, useNotices, useUsers,
│                        # useActivityFeed
├── views/               # VIEW — componentes/páginas (sem regra de negócio)
│   ├── components/ui/       # wrappers de View sobre os primitivos shadcn (Button, Card, feedback…)
│   ├── components/form/     # FormComponent único, config-driven (TanStack Form)
│   ├── components/data-table/  # DataTable reutilizável (TanStack Table)
│   ├── components/modal/    # ModalComponent
│   ├── components/map/      # mapa OpenStreetMap (react-leaflet) + picker de localização
│   ├── components/reviews/  # StarRating, PointReviewsModal
│   ├── components/collection-points/  # StockEditorModal
│   ├── layouts/          # AppLayout (área autenticada)
│   └── pages/             # Map, Login, Dashboard, CollectionPoints, Stock,
│                          # Beneficiaries, RegisterWithdrawal, Donations, Notices, Users, NotFound
├── stores/               # Zustand (sessão de autenticação)
├── lib/                  # env (validado por Zod), queryClient, logger, csv, utils
└── app/                  # router, providers, ProtectedRoute, lazyWithReload
```

## Desenvolvimento

O gerenciador de pacotes é o **pnpm** (fixado via `packageManager` no `package.json`;
use o corepack: `corepack enable`).

```bash
pnpm install
pnpm run dev     # http://localhost:5173  (proxy /api -> http://localhost:8080)
```

O backend não expõe CORS; em dev o Vite faz proxy de `/api` para a API .NET
(veja `VITE_API_PROXY_TARGET` no `.env`). Suba a API antes (`make dev` na raiz, ou
`docker compose up api postgres`).

Scripts: `pnpm run dev`, `pnpm run build`, `pnpm run preview`, `pnpm run lint`, `pnpm run test` (Vitest),
`pnpm run test:watch`.

### Hot reload no Docker

Pela raiz do monorepo, `make dev` (equivale a
`docker compose -f docker-compose.yml -f docker-compose.dev.yml up`): Vite + HMR em http://localhost:5445.

## Variáveis de ambiente

Copie `.env.example` para `.env`. Principais:

| Variável | Descrição |
| --- | --- |
| `VITE_API_URL` | Base da API no navegador (dev: `/api`). |
| `VITE_API_PROXY_TARGET` | Alvo do proxy de dev do Vite. |
| `VITE_NOMINATIM_URL` | Endpoint do geocoder OSM. |
| `VITE_BRASILAPI_URL` | Endpoint da BrasilAPI (autopreenchimento por CEP). |
| `VITE_MAP_DEFAULT_LAT/LNG/ZOOM` | Centro/zoom inicial do mapa. |

## Sobre o mapa, CEP e a geocodificação

O backend guarda **endereço textual**, não coordenadas. O frontend geocodifica cada
endereço via **Nominatim (OpenStreetMap)**, respeitando o limite de 1 req/s e cacheando
o resultado em `localStorage`. Para produção em escala, o ideal é persistir lat/long no
backend (evita depender do Nominatim e acelera o carregamento do mapa).

No formulário de cadastro de ponto/família, o CEP é resolvido via **BrasilAPI**
(`cepService`), preenchendo rua/bairro/cidade/UF e uma posição aproximada — separado da
geocodificação do Nominatim, que é o passo final para o pino exato no mapa.

## Build de produção (Docker)

`docker build -t doacao-frontend ./frontend` gera uma imagem **nginx** que serve os
estáticos e faz proxy de `/api` para o serviço `api`. No monorepo, use `make dev` (desenvolvimento) ou
`make prod` (produção, atrás do nginx de borda) na raiz para subir tudo.

## Testes

`pnpm run test` roda o Vitest (schemas Zod e páginas com Testing Library). O CI executa lint, testes e build.
