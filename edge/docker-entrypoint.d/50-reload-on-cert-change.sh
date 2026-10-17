#!/bin/sh
# Recarrega o nginx quando o certbot grava um certificado novo em /certs (renovação).
# Roda em segundo plano; confere a cada minuto e só recarrega se a config validar.
set -eu

(
  last="$(cat /certs/*/*.pem 2>/dev/null | md5sum)"
  while sleep 60; do
    current="$(cat /certs/*/*.pem 2>/dev/null | md5sum)"
    if [ "$current" != "$last" ]; then
      if nginx -t -q; then
        nginx -s reload && echo "[certs] certificado alterado — nginx recarregado"
        last="$current"
      else
        echo "[certs] certificado alterado, mas nginx -t falhou — mantendo o atual" >&2
      fi
    fi
  done
) &
