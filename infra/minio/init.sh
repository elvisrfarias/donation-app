#!/bin/sh
# =============================================================================
#  Inicialização do MinIO (roda uma vez a cada `up`, antes da API).
#
#  - Garante o bucket das imagens.
#  - Se MINIO_APP_ACCESS_KEY/MINIO_APP_SECRET_KEY estiverem definidos, cria (ou atualiza)
#    um usuário só para a API, com permissão apenas de ler/gravar/apagar objetos do
#    bucket — a API deixa de usar a credencial root do MinIO.
#  Idempotente: rodar de novo só reaplica o mesmo estado.
# =============================================================================
set -eu

: "${MINIO_ROOT_USER:?}" "${MINIO_ROOT_PASSWORD:?}" "${STORAGE_BUCKET:?}"
export HOME=/tmp

mc alias set local http://minio:9000 "$MINIO_ROOT_USER" "$MINIO_ROOT_PASSWORD" >/dev/null
mc mb --ignore-existing "local/$STORAGE_BUCKET"

if [ -z "${MINIO_APP_ACCESS_KEY:-}" ] || [ -z "${MINIO_APP_SECRET_KEY:-}" ]; then
  echo "[minio-init] MINIO_APP_* não definidos — a API usa a credencial root (aceitável só em dev)."
  exit 0
fi

cat > /tmp/policy.json <<POLICY
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": ["s3:GetBucketLocation", "s3:ListBucket"],
      "Resource": ["arn:aws:s3:::${STORAGE_BUCKET}"]
    },
    {
      "Effect": "Allow",
      "Action": ["s3:GetObject", "s3:PutObject", "s3:DeleteObject"],
      "Resource": ["arn:aws:s3:::${STORAGE_BUCKET}/*"]
    }
  ]
}
POLICY

mc admin policy create local doacao-api /tmp/policy.json >/dev/null
mc admin user add local "$MINIO_APP_ACCESS_KEY" "$MINIO_APP_SECRET_KEY" >/dev/null
mc admin policy attach local doacao-api --user "$MINIO_APP_ACCESS_KEY" >/dev/null 2>&1 \
  || mc admin user info local "$MINIO_APP_ACCESS_KEY" | grep -q 'doacao-api'
echo "[minio-init] bucket '$STORAGE_BUCKET' e usuário restrito da API prontos."
