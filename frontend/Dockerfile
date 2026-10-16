# ============================================================================
#  Build — compila a SPA com Node 24 (glibc/slim: casa com o package-lock do
#  host e com os binários nativos de Tailwind/lightningcss; evita o problema
#  musl×glibc do alpine no `npm ci`).
# ============================================================================
FROM node:24-slim AS build
WORKDIR /app

# Habilita o pnpm via corepack (versão fixada no campo "packageManager" do package.json).
RUN corepack enable

# Cache de dependências: copia manifests primeiro.
COPY package.json pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile

COPY . .
# VITE_API_URL=/api (padrão): o nginx faz o proxy de /api para o serviço da API.
RUN pnpm run build

# ============================================================================
#  Runtime — nginx servindo os estáticos + proxy reverso para a API.
#  Imagem "unprivileged": todo o processo (master + workers) roda como usuário
#  não-root desde o boot — por isso a porta é 8080, não 80 (privilegiada).
# ============================================================================
FROM nginxinc/nginx-unprivileged:1.27-alpine AS runtime

COPY --chown=nginx:nginx nginx.conf /etc/nginx/conf.d/default.conf
COPY --chown=nginx:nginx --from=build /app/dist /usr/share/nginx/html

# White-label em runtime: gera /theme.css a partir de BRAND_PRIMARY no boot.
# A imagem do nginx executa /docker-entrypoint.d/*.sh antes de iniciar o servidor.
COPY --chmod=755 docker-theme-entrypoint.sh /docker-entrypoint.d/40-theme.sh

# SEO em runtime: troca __SITE_URL__ (og:image, sitemap…) pela URL pública SITE_URL.
# Os originais ficam fora do html/ para o script sempre partir do placeholder.
COPY --from=build /app/dist/index.html /app/dist/robots.txt /app/dist/sitemap.xml /usr/share/nginx/seo-templates/
COPY --chmod=755 docker-seo-entrypoint.sh /docker-entrypoint.d/41-seo.sh

EXPOSE 8080

# 127.0.0.1 (não "localhost"): o nginx escuta em IPv4; localhost pode resolver para ::1.
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD wget -qO- http://127.0.0.1:8080/ >/dev/null 2>&1 || exit 1
