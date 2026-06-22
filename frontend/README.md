# Doação App — Frontend

SPA do Doação App: mapa público de pontos de coleta (OpenStreetMap) + área de gestão
(pontos, famílias/beneficiários e registro de retiradas com a regra de equidade).

## Stack

- **React 19** + **Vite 8** + **TypeScript** (strict)
- **Tailwind CSS v4** (configuração via CSS, `@theme`)
- **React Router 7** (rotas com code-splitting por `lazy`)
- **TanStack Query 5** (estado de servidor) + **Zustand 5** (autenticação)
- **React Hook Form** + **Zod 4** (formulários e validação)
- **Axios** (cliente HTTP com refresh transparente de JWT)
- **react-leaflet 5** + **Leaflet** sobre tiles do **OpenStreetMap**
- Geocodificação via **Nominatim** (OSM), com cache em `localStorage`

## Arquitetura — MVVM

A separação é estrita: a **View** nunca chama serviços diretamente; ela consome um
**ViewModel** (hook), que orquestra o **Model** (tipos + serviços de dados).

```
src/
├── models/            # MODEL — sem React
│   ├── types/         # tipos de domínio (espelham os contratos da API)
│   ├── api/           # cliente axios, interceptors, ProblemDetails, tokenStorage
│   └── services/      # auth, collectionPoint, family, withdrawal, geocoding
├── viewmodels/        # VIEWMODEL — hooks (estado + comandos), schemas Zod
├── views/             # VIEW — componentes/páginas (sem regra de negócio)
│   ├── components/ui  # primitivas (Button, Input, Card, Alert…)
│   ├── components/map # mapa OpenStreetMap (react-leaflet)
│   ├── layouts/       # AppLayout (área autenticada)
│   └── pages/         # Map, Login, Dashboard, CollectionPoints, Families, Withdrawal
├── stores/            # Zustand (sessão de autenticação)
├── lib/               # env (validado por Zod), queryClient, utils
└── app/               # router, providers, ProtectedRoute, main
```

## Desenvolvimento

O gerenciador de pacotes é o **pnpm** (fixado via `packageManager` no `package.json`;
use o corepack: `corepack enable`).

```bash
pnpm install
pnpm run dev     # http://localhost:5173  (proxy /api -> http://localhost:8080)
```

O backend não expõe CORS; em dev o Vite faz proxy de `/api` para a API .NET
(veja `VITE_API_PROXY_TARGET` no `.env`). Suba a API antes (`make up` na raiz, ou
`docker compose up api postgres`).

Scripts: `pnpm run dev`, `pnpm run build`, `pnpm run preview`, `pnpm run lint`.

### Hot reload no Docker

```bash
docker compose -f docker-compose.yml -f docker-compose.dev.yml up   # Vite + HMR na 5445
```

## Variáveis de ambiente

Copie `.env.example` para `.env`. Principais:

| Variável | Descrição |
| --- | --- |
| `VITE_API_URL` | Base da API no navegador (dev: `/api`). |
| `VITE_API_PROXY_TARGET` | Alvo do proxy de dev do Vite. |
| `VITE_NOMINATIM_URL` | Endpoint do geocoder OSM. |
| `VITE_MAP_DEFAULT_LAT/LNG/ZOOM` | Centro/zoom inicial do mapa. |

## Sobre o mapa e a geocodificação

O backend guarda **endereço textual**, não coordenadas. O frontend geocodifica cada
endereço via **Nominatim (OpenStreetMap)**, respeitando o limite de 1 req/s e cacheando
o resultado em `localStorage`. Para produção em escala, o ideal é persistir lat/long no
backend (evita depender do Nominatim e acelera o carregamento do mapa).

## Build de produção (Docker)

`docker build -t doacao-frontend ./frontend` gera uma imagem **nginx** que serve os
estáticos e faz proxy de `/api` para o serviço `api`. No monorepo, use `make up` na raiz
para subir tudo (postgres + api + frontend).
