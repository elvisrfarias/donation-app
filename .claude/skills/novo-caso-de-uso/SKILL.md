---
name: novo-caso-de-uso
description: Gera o esqueleto de um novo caso de uso CQRS (Command ou Query, com Validator e Handler no mesmo arquivo — vertical slice) seguindo o padrão da Application layer deste projeto. Use ao adicionar uma nova operação de negócio (ex. "desativar ponto de coleta", "listar retiradas por período").
---

Cria um novo arquivo em `backend/src/DoacaoApp.Application/<Área>/{Commands,Queries}/<Nome>.cs`
com Command/Query + Validator (se fizer sentido) + Handler, no padrão vertical-slice já usado no
projeto.

## Antes de gerar

1. **Identifique área, nome e tipo** a partir do argumento do skill (formato livre, ex.
   "CollectionPoints DeactivatePoint command"). Se faltar algo essencial — a área
   (`CollectionPoints`, `Families`, `Reviews`, `Storage`, `Authentication`), o nome do caso de uso
   em PascalCase, ou se é Command (muda estado) ou Query (só lê) — pergunte ao usuário em vez de
   adivinhar; isso decide em qual pasta e com qual formato o arquivo nasce.

2. **Leia 1–2 arquivos existentes na mesma pasta** (`Commands/` ou `Queries/` da área escolhida)
   antes de escrever qualquer coisa. Referências boas para copiar o estilo exato:
   - Command com validação de coleção: `CollectionPoints/Commands/UpdateCollectionPointStock.cs`
   - Command simples: `Families/Commands/RegisterBeneficiary.cs`
   - Query sem validator (validação via Value Object): `Families/Queries/FindFamilyByCpf.cs`

3. **Verifique se o arquivo já existe** — nunca sobrescreva um caso de uso existente sem perguntar.

## Convenções obrigatórias (ver também `CLAUDE.md`)

- Um arquivo só, três tipos: `<Nome>Command` (ou `Query`) + `<Nome>Validator` (quando há campos
  para validar além de Value Objects) + `<Nome>Handler`, todos `sealed`.
- `IRequest<ErrorOr<TResponse>>` — nunca lança exception para erro de negócio.
- Handler com **primary constructor** injetando repositório/`IUnitOfWork`/`IDateTimeProvider`
  (nunca `DateTime.UtcNow` direto).
- Erros de domínio vêm de `Domain/Errors/DomainErrors.cs` — se o erro não existir ainda, adicione
  lá (catálogo centralizado), não crie `Error.Custom(...)` solto no handler.
- Validator usa **FluentValidation** (`RuleFor`, `RuleForEach`); validação de formato que já é
  responsabilidade de um Value Object (Cpf, Email, etc.) fica pro `X.Create(...)` do domínio, não
  duplique no validator.
- **Sem primitive obsession**: se um dos parâmetros do Command/Query tem regra de validação,
  formato ou igualdade própria (não só "obrigatório"/"tamanho máximo"), ele deveria ser — ou já
  existir como — um Value Object, não um `string`/`int` cru.
- `/// <summary>` no Command/Query descrevendo a intenção e quem pode chamar — é a única forma de
  comentário aceita neste repositório. **Nenhum comentário `//`/`/* */` dentro do corpo dos métodos.**
- Nomes descritivos (`familyWithdrawalsInCurrentCycle`, não `count`) — sem abreviações obscuras.
- Se a resposta precisa de um novo shape, adicione o `record` em `<Área>/Contracts/`, seguindo o
  `ToResponse()` existente na área.

## Depois de gerar

Isso cria só o caso de uso — não crie controller, endpoint, migration nem teste automaticamente.
Ao terminar, diga ao usuário os próximos passos manuais:

1. Expor via `Controller` ou Minimal API endpoint (com o `[Authorize(Roles = ...)]` certo).
2. Adicionar a rota na tabela de Endpoints do `README.md` raiz (documentamos isso recentemente —
   não deixe o catálogo ficar defasado de novo).
3. **TDD**: se o caso de uso implementa ou altera uma regra de negócio (não é CRUD trivial), o
   teste em `DoacaoApp.Application.Tests`/`DoacaoApp.Domain.Tests` (padrão de
   `RegisterWithdrawalHandlerTests`) não é opcional nem "depois" — deveria ter sido a primeira coisa
   escrita, antes deste handler. Se ainda não existe, ofereça para escrevê-lo agora.
