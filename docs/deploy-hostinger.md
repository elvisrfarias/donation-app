# Deploy em VPS KVM (Hostinger)

1. **DNS**: aponte `APP_DOMAIN` e `STORAGE_DOMAIN` (registros A) para o IP da VPS.
2. **Servidor** (Ubuntu): instale Docker + plugin compose e ative o firewall:
   `ufw allow OpenSSH && ufw allow 80 && ufw allow 443 && ufw enable`.
   Use login SSH por chave e desative senha. Habilite também o firewall/snapshots no painel da Hostinger.
3. **Código**: `git clone` do repositório; `cp .env.prod.example .env.prod` e preencha tudo (`openssl rand -base64 48`).
4. **Primeiro boot**: defina `SeedData__Enabled=true` com senha forte do admin (isso também cria pontos de demonstração), suba com `make prod`, entre, troque a senha e volte para `false`.
5. **Subir**: `make prod`. O Caddy emite o certificado Let's Encrypt sozinho.
6. **Backup**: agende no cron do host `make backup` (diário) e copie `backups/` para fora da VPS (rclone/S3). Teste `make restore-db` periodicamente.
7. **Atualizar**: `git pull && make prod`.
