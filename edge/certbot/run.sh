#!/bin/sh
# =============================================================================
#  Certificados TLS do nginx de borda (Let's Encrypt, validação HTTP-01 via webroot).
#
#  - Primeiro boot sem certificado: gera um autoassinado temporário em /certs para o
#    nginx conseguir subir, e logo em seguida emite o definitivo pelo webroot.
#  - Depois, tenta renovar a cada 12h (o certbot só renova a menos de 30 dias do fim).
#  - O deploy hook copia o certificado para /certs/<domínio>/, legível pelo nginx
#    (uid 101), que recarrega sozinho ao detectar a troca.
# =============================================================================
set -eu

: "${CERT_DOMAINS:?defina CERT_DOMAINS (domínios separados por espaço)}"
: "${ACME_EMAIL:?defina ACME_EMAIL}"

NGINX_UID=101

install_cert() {
  # $1 = domínio, $2 = fullchain, $3 = privkey
  mkdir -p "/certs/$1"
  install -m 0644 -o root -g "$NGINX_UID" "$2" "/certs/$1/fullchain.pem.new"
  install -m 0640 -o root -g "$NGINX_UID" "$3" "/certs/$1/privkey.pem.new"
  mv "/certs/$1/privkey.pem.new" "/certs/$1/privkey.pem"
  mv "/certs/$1/fullchain.pem.new" "/certs/$1/fullchain.pem"
  chown root:"$NGINX_UID" "/certs/$1" && chmod 0750 "/certs/$1"
}

if [ "${1:-}" = "deploy-hook" ]; then
  # Chamado pelo certbot após emitir/renovar: RENEWED_LINEAGE=/etc/letsencrypt/live/<nome>
  name="$(basename "$RENEWED_LINEAGE")"
  install_cert "$name" "$RENEWED_LINEAGE/fullchain.pem" "$RENEWED_LINEAGE/privkey.pem"
  echo "[certbot] certificado de $name instalado em /certs/$name"
  exit 0
fi

for domain in $CERT_DOMAINS; do
  if [ ! -s "/certs/$domain/fullchain.pem" ]; then
    echo "[certbot] $domain sem certificado — gerando autoassinado temporário"
    tmp="$(mktemp -d)"
    openssl req -x509 -nodes -newkey rsa:2048 -days 7 -subj "/CN=$domain" \
      -keyout "$tmp/privkey.pem" -out "$tmp/fullchain.pem" 2>/dev/null
    install_cert "$domain" "$tmp/fullchain.pem" "$tmp/privkey.pem"
    rm -rf "$tmp"
  fi
done

# Dá tempo ao nginx de subir (ele espera os certificados acima) antes da validação HTTP-01.
sleep 20

while :; do
  for domain in $CERT_DOMAINS; do
    certbot certonly --webroot -w /var/www/certbot \
      --cert-name "$domain" -d "$domain" \
      --non-interactive --agree-tos --no-eff-email -m "$ACME_EMAIL" \
      --key-type ecdsa --keep-until-expiring \
      --deploy-hook "/opt/certbot/run.sh deploy-hook" \
      || echo "[certbot] falha ao emitir/renovar $domain — nova tentativa em 12h" >&2
  done
  sleep 12h
done
