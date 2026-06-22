# ============================================================================
#  Build — compila a SPA com Node 24.
# ============================================================================
FROM node:24-alpine AS build
WORKDIR /app

# Cache de dependências: copia manifests primeiro.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
# VITE_API_URL=/api (padrão): o nginx faz o proxy de /api para o serviço da API.
RUN npm run build

# ============================================================================
#  Runtime — nginx servindo os estáticos + proxy reverso para a API.
# ============================================================================
FROM nginx:1.27-alpine AS runtime

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD wget -qO- http://localhost:80/ >/dev/null 2>&1 || exit 1
