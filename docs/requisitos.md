# Especificação de requisitos

Derivada do sistema implementado. Atores: **Visitante** (anônimo), **Gestor de ponto** (`PointManager`),
**Administrador** (`Admin`).

## Requisitos funcionais

| ID | Requisito | Ator | Endpoint / tela |
|---|---|---|---|
| RF01 | Listar pontos de coleta ativos, com endereço, contato, horário, necessidades e avaliação | Visitante | `GET /api/public/collection-points`, mapa |
| RF02 | Filtrar pontos por cidade e ordenar pelos mais bem avaliados | Visitante | mapa; `?sort=top-rated` |
| RF03 | Avaliar um ponto com nota (1 a 5) e comentário | Visitante | `POST /api/public/collection-points/{id}/reviews` |
| RF04 | Denunciar avaliação inadequada | Visitante | `POST /api/public/reviews/{id}/report` |
| RF05 | Avisar que pretende doar a um ponto | Visitante | `POST /api/public/collection-points/{id}/donations` |
| RF06 | Ler os avisos públicos de um ponto | Visitante | `GET /api/public/collection-points/{id}/notices` |
| RF07 | Autenticar-se e renovar a sessão | Gestor, Admin | `/api/auth/*` |
| RF08 | Cadastrar e editar pontos de coleta | Admin | `/api/collection-points` |
| RF09 | Manter o catálogo de estoque (itens, categorias, metas e avisos de estoque baixo) | Gestor, Admin | `/api/collection-points/{id}/stock-items/*` |
| RF10 | Cadastrar beneficiários e vinculá-los a pontos | Gestor, Admin | `/api/beneficiaries` |
| RF11 | Registrar retirada, respeitando o limite do ciclo somado entre todos os pontos | Gestor, Admin | `POST /api/withdrawals` |
| RF12 | Corrigir a retirada mais recente do beneficiário | Gestor, Admin | `PUT /api/withdrawals/{id}` |
| RF13 | Listar as doações prometidas ao ponto | Gestor, Admin | `GET /api/collection-points/{id}/donations` |
| RF14 | Confirmar o recebimento de uma doação, somando itens ao estoque | Gestor, Admin | `POST .../donations/{id}/receive` |
| RF15 | Cancelar uma doação prometida | Gestor, Admin | `POST .../donations/{id}/cancel` |
| RF16 | Publicar aviso no mural, para a comunidade ou só para gestores | Gestor, Admin | `POST /api/collection-points/{id}/notices` |
| RF17 | Retirar um aviso do mural | Gestor, Admin | `DELETE .../notices/{id}` |
| RF18 | Ver o mural da rede (avisos de todos os pontos) | Gestor, Admin | `GET /api/notices` |
| RF19 | Ver o feed de atividade recente do ponto | Gestor, Admin | `GET .../activity`, painel |
| RF20 | Moderar avaliações (ocultar e reexibir) | Admin | `/api/reviews/{id}/hide` e `/restore` |
| RF21 | Acompanhar indicadores de retiradas e atendimentos por período | Gestor, Admin | `GET .../dashboard` |

## Regras de negócio

| ID | Regra |
|---|---|
| RN01 | Um beneficiário não excede o limite de cestas do ciclo somando as retiradas de **todos** os pontos; o limite aplicado é o do ponto onde a retirada acontece. |
| RN02 | Só se retira o que tem quantidade positiva no estoque do próprio ponto. |
| RN03 | Só a retirada mais recente do beneficiário pode ser corrigida, e só pelo ponto que a fez. |
| RN04 | Uma doação só é prometida a um ponto **ativo**. |
| RN05 | Uma doação prometida fecha uma única vez: recebida **ou** cancelada. |
| RN06 | Receber uma doação com itens soma ao estoque em tudo-ou-nada: se um item é inválido, nada muda e a doação continua prometida. |
| RN07 | O gestor só age no próprio ponto; o administrador age em qualquer um. |
| RN08 | Avaliações são moderadas por ocultação lógica, nunca por exclusão. |
| RN09 | Avisos "só gestores" nunca aparecem na API pública. |

## Requisitos não funcionais

| ID | Requisito | Como é atendido |
|---|---|---|
| RNF01 | Segurança de acesso | JWT com refresh rotativo, senhas com BCrypt, roles e escopo por ponto, limite de requisições nas rotas de autenticação |
| RNF02 | Privacidade | Telefone do doador só aparece na área autenticada do ponto; dados de beneficiários só para quem está vinculado |
| RNF03 | Rastreabilidade | Toda retirada fica no histórico; exclusões são lógicas; log estruturado com Serilog e correlation id |
| RNF04 | Manutenibilidade | Clean Architecture, DDD e CQRS; regras de negócio cobertas por testes de domínio e de aplicação |
| RNF05 | Usabilidade | Ver [usabilidade-nielsen-material.md](usabilidade-nielsen-material.md) |
| RNF06 | Acessibilidade | Navegação por teclado, foco visível, link "pular para o conteúdo", rótulos nos campos, regiões `aria-live`, respeito a `prefers-reduced-motion` |
| RNF07 | Portabilidade | Docker Compose sobe a stack inteira (PostgreSQL, MinIO, API, frontend) |
| RNF08 | Disponibilidade | Health checks em `/health` e `/health/ready` |
