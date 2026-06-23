#!/bin/sh
# =============================================================================
#  White-label em runtime. Roda no boot do container (a imagem oficial do nginx
#  executa todos os /docker-entrypoint.d/*.sh antes de subir o servidor).
#
#  Lê BRAND_PRIMARY e gera /theme.css com a cor da marca. Trocar a variável e
#  reiniciar o container rebrandeia a plataforma inteira — sem recompilar.
#
#  Aceita qualquer cor CSS válida (hex, rgb(), hsl(), oklch()…). Ex.:
#    BRAND_PRIMARY="#7c3aed"   ->  roxo
#    BRAND_PRIMARY="#0d9488"   ->  teal
# =============================================================================
set -eu

TARGET="/usr/share/nginx/html/theme.css"

if [ -n "${BRAND_PRIMARY:-}" ]; then
  printf ':root{--brand:%s !important;}\n' "$BRAND_PRIMARY" > "$TARGET"
  echo "[theme] BRAND_PRIMARY aplicado: $BRAND_PRIMARY"
else
  # Sem override: zera o arquivo para a plataforma usar a cor padrão do bundle.
  printf '/* white-label: defina BRAND_PRIMARY para sobrescrever a cor da marca */\n' > "$TARGET"
  echo "[theme] BRAND_PRIMARY não definido — usando a cor padrão do app."
fi
