---
name: nova-migration
description: Cria uma migration do EF Core via container do SDK .NET 9 (o host não tem dotnet instalado) e corrige a posse dos arquivos gerados. Use sempre que um agregado, Value Object ou mapeamento EF mudar e o schema precisar ser versionado.
---

Gera uma nova migration do EF Core para o `DoacaoApp.Infrastructure`, do jeito descrito em
`CLAUDE.md` (ambiente sem SDK .NET local), e deixa os arquivos prontos para revisão — sem aplicar
no banco.

## Passos

1. **Confirme o diretório**: os comandos abaixo assumem `cwd` na raiz do monorepo (onde está
   `docker-compose.yml`), não em `backend/`. Se não estiver lá, ajuste os `-v` dos volumes.

2. **Nome da migration**: use o argumento passado ao skill. Se vier vazio ou genérico, pergunte ao
   usuário um nome descritivo em PascalCase (ex.: `AddReviewReportCount`, não `Update1`).

3. **Gere a migration** dentro do container do SDK (usa o volume `doacao-nuget` para não perder o
   cache de pacotes entre execuções):

   ```bash
   docker run --rm -v "$PWD/backend":/src -v doacao-nuget:/root/.nuget -w /src mcr.microsoft.com/dotnet/sdk:9.0 \
     bash -c "dotnet tool install -g dotnet-ef --version 9.0.0 >/dev/null; export PATH=\$PATH:/root/.dotnet/tools; \
     dotnet ef migrations add <Nome> -p src/DoacaoApp.Infrastructure -s src/DoacaoApp.Api -o Persistence/Migrations"
   ```

   Esse passo já falha aqui se algum mapeamento EF estiver errado (Value Object sem conversão,
   coleção sem `PropertyAccessMode.Field`, etc.) — se der erro, não tente contornar; leia a mensagem
   e confira a seção "Convenções de mapeamento EF" do `CLAUDE.md` antes de tentar de novo.

4. **Corrija a posse dos arquivos** (o container escreve como `root`):

   ```bash
   docker run --rm -v "$PWD/backend":/src alpine chown -R "$(stat -c '%u:%g' backend/DoacaoApp.sln)" /src
   ```

5. **Mostre o que foi gerado** (`git status` / `git diff` em
   `backend/src/DoacaoApp.Infrastructure/Persistence/Migrations/`) para o usuário revisar o
   `Up`/`Down` antes de qualquer commit.

## O que este skill NÃO faz

- **Não roda `dotnet ef database update`** — aplicar a migration muda o schema do banco de verdade
  e exige confirmação explícita do usuário (o comando já fica na lista `ask` do
  `.claude/settings.local.json` por esse motivo). Se o usuário pedir para aplicar, rode-o
  separadamente e só depois de ele confirmar.
- Não commita nada automaticamente.
