# Deploy em VPS KVM (Hostinger)

Produção roda numa única VPS com Docker Compose. Só o **Caddy** publica portas (80/443) e emite o certificado
Let's Encrypt sozinho; banco, MinIO, API e frontend ficam na rede interna.

```
internet ─► Caddy ─┬─ APP_DOMAIN     ─► frontend (nginx) ─► /api ─► api ─► postgres
                   └─ STORAGE_DOMAIN ─► minio (URLs pré-assinadas das imagens)
```

## Pré-requisitos

- VPS Ubuntu com acesso SSH por chave.
- Dois registros DNS do tipo A apontando para o IP da VPS: um para `APP_DOMAIN` (app) e outro para
  `STORAGE_DOMAIN` (arquivos).

## Passo a passo

1. **DNS**: aponte `APP_DOMAIN` e `STORAGE_DOMAIN` para o IP da VPS.
2. **Servidor**: instale Docker + plugin compose e ative o firewall:
   `ufw allow OpenSSH && ufw allow 80 && ufw allow 443 && ufw enable`.
   Use login SSH por chave e desative a senha. Habilite também firewall e snapshots no painel da Hostinger.
3. **Código**: `git clone` do repositório; `cp .env.prod.example .env.prod` e preencha tudo. Gere cada
   segredo com `openssl rand -base64 48`.
4. **Primeiro boot**: defina `SeedData__Enabled=true` com uma senha forte para o admin (isso também cria
   pontos de demonstração), suba com `make prod`, entre, **troque a senha** e volte a flag para `false`.
5. **Subir**: `make prod`.
6. **Backup**: agende `make backup` (diário) no cron do host e copie `backups/` para fora da VPS (rclone/S3).
   A pasta contém dados reais e não é versionada.
7. **Atualizar**: `git pull && make prod`.

## Variáveis que precisam de atenção

| Variável | Observação |
|---|---|
| `APP_DOMAIN`, `STORAGE_DOMAIN`, `ACME_EMAIL` | Usadas pelo Caddy; sem DNS correto o certificado não é emitido |
| `Jwt__Secret` | ≥ 32 caracteres; trocar invalida as sessões |
| `POSTGRES_PASSWORD` | Deve ser a mesma usada em `ConnectionStrings__Database` |
| `MINIO_ROOT_USER` / `MINIO_ROOT_PASSWORD` | Credenciais do bucket |
| `SeedData__Enabled` | `true` só no primeiro boot |

## Operação

| Tarefa | Comando |
|---|---|
| Logs da API | `make prod-logs` |
| Status dos containers | `make ps` |
| Backup de banco e arquivos | `make backup` (ou `backup-db` / `backup-storage`) |
| Restaurar o banco | `make restore-db file=backups/db-AAAAMMDD-HHMMSS.dump` |
| Restaurar os arquivos | `make restore-storage file=backups/storage-AAAAMMDD-HHMMSS.tar.gz` |
| Zerar tudo (destrutivo) | `make prod-reset CONFIRM=yes` — faz backup antes |

Teste o `make restore-db` periodicamente: um backup que nunca foi restaurado não é garantia.

## Problemas comuns

- **Certificado não emite**: confira o DNS (`dig +short $APP_DOMAIN`) e se as portas 80/443 estão abertas.
- **Imagens não carregam**: `Storage__PublicUrl` deve ser `https://$STORAGE_DOMAIN` (o compose de produção já define).
- **API não sobe**: veja `make prod-logs`; segredo ausente ou connection string divergente da senha do Postgres.
