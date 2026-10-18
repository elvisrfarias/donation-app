#!/bin/sh
# =============================================================================
#  SEO / prévia de links em runtime. Roda no boot do container nginx.
#
#  Troca o placeholder __SITE_URL__ pela URL pública (SITE_URL) no index.html,
#  robots.txt, sitemap.xml e llms.txt — WhatsApp/Facebook/Google exigem URLs absolutas em
#  og:image, og:url e no sitemap. Parte sempre das cópias originais do build
#  (/usr/share/nginx/seo-templates), então trocar SITE_URL e reiniciar basta.
#
#    SITE_URL="https://dooa.com.br"   (sem barra no final)
# =============================================================================
set -eu

TEMPLATES="/usr/share/nginx/seo-templates"
HTML="/usr/share/nginx/html"
SITE_URL="${SITE_URL%/}"

for file in index.html robots.txt sitemap.xml llms.txt; do
  # Escreve sobre o arquivo existente (o diretório não é gravável pelo usuário nginx).
  sed "s|__SITE_URL__|${SITE_URL}|g" "$TEMPLATES/$file" > "$HTML/$file"
done

if [ -n "$SITE_URL" ]; then
  echo "[seo] SITE_URL aplicado: $SITE_URL"
else
  echo "[seo] SITE_URL não definido — prévias de link (og:image) não vão funcionar."
fi
