# Usabilidade: heurísticas de Nielsen, Material Design e acessibilidade

Avaliação heurística feita sobre a interface **como está no código**, sem teste com usuários. Serve de
roteiro para a etapa "verificação e validação"; os resultados com a comunidade entram em
[validacao-com-usuarios.md](validacao-com-usuarios.md). **Não é** um laudo de acessibilidade: contraste e
leitores de tela ainda precisam ser testados com ferramenta (veja "Próximos passos").

## Heurísticas de Nielsen

| # | Heurística | Como o sistema atende | Lacuna |
|---|---|---|---|
| 1 | Visibilidade do estado do sistema | Spinners de carregamento, mensagens de sucesso ("Aviso publicado no mural"), botões com estado de envio, feed de atividade que atualiza sozinho | — |
| 2 | Correspondência com o mundo real | Texto em português simples ("Quero doar", "Precisa agora", "Recebi"), ícones por categoria | Termos como "ciclo de retirada" pedem explicação para novos gestores |
| 3 | Controle e liberdade do usuário | Modais fecham com Esc e com o X; doação prometida pode ser cancelada pelo gestor; aviso pode ser retirado | — (cancelar doação e retirar aviso pedem confirmação em dois passos, com "Voltar") |
| 4 | Consistência e padrões | Mesmos componentes em todo o app (`Button`, `Card`, `Alert`, `Badge`, `FormField`, `ModalComponent`) | — |
| 5 | Prevenção de erros | Botão "Publicar aviso" desabilitado sem título e mensagem; limites de caracteres nos campos; backend recusa retirada acima do limite e doação a ponto inativo | — (o telefone é validado no formulário antes de enviar, além do backend) |
| 6 | Reconhecimento em vez de memorização | Campos com rótulo visível e dica ("Opcional. Só o responsável pelo ponto verá"), lista de itens do estoque no recebimento | — |
| 7 | Flexibilidade e eficiência | Filtro por cidade e ordenação por avaliação no mapa; atalhos no painel; navegação por teclado | Sem atalhos de teclado próprios |
| 8 | Estética e design minimalista | Telas com um objetivo cada; informação secundária em texto menor | O painel do gestor já concentra muito conteúdo |
| 9 | Ajudar a reconhecer e corrigir erros | Erros da API chegam em linguagem natural (`displayMessage` de ProblemDetails) e aparecem junto ao formulário | — |
| 10 | Ajuda e documentação | Seções "Sobre" e "Como funciona" na página pública; dicas nos campos | — (as páginas Doações e Mural têm o bloco recolhível "Como esta tela funciona") |

## Material Design

O projeto **não usa a biblioteca Material Design**: a interface é própria, sobre Tailwind CSS e Radix UI
(shadcn). O que existe é aderência a princípios do Material Design 3, não uso do kit.

| Princípio do Material Design 3 | Situação |
|---|---|
| Sistema de design com tokens (cor, forma, tipografia) | Atende em parte: tokens de cor da marca (`--brand`) e raios arredondados padronizados |
| Elevação e superfícies | Atende: `Card` com sombra leve, modais com sobreposição |
| Estados de interação (hover, foco, pressionado, desabilitado) | Atende: variantes de `Button` e foco visível global |
| Alvo de toque mínimo (44 a 48 px) | Atende no celular: `Button size="sm"` e as ações do cartão no mapa têm 44px abaixo do breakpoint `sm`; em telas maiores voltam ao tamanho compacto |
| Componentes padronizados | Atende com componentes próprios equivalentes (diálogos, abas, seletores, chips/badges) |

Se o curso exigir Material Design de fato, o caminho é trocar a camada de componentes (por exemplo, MUI ou
Material Web) ou documentar este mapeamento como adaptação.

## Acessibilidade

Presente no código:

- **Navegação por teclado** em toda a interface, com foco visível global.
- **Link "Pular para o conteúdo"** no mapa público e na área logada.
- **Rótulos ligados aos campos** (`label` com `htmlFor`) nos formulários novos, incluindo o grupo de
  opções do público do aviso (`fieldset` com `legend`).
- **Regiões `aria-live`** nas listas que mudam sozinhas (feed de atividade, doações, mural).
- **`role="alert"`** nos erros de validação do formulário "Quero doar".
- **Texto alternativo** para os ícones que carregam sentido (tipo de atividade no feed usa texto só para
  leitor de tela).
- **`prefers-reduced-motion`** respeitado: animações caem para 0,01 ms.
- **Contraste (WCAG AA, mínimo 4,5:1)**, calculado sobre fundo branco: cor da marca `#2563eb` 5,17:1; verde `emerald-700` 5,48:1; âmbar `amber-700` 5,02:1; texto secundário `stone-500` 4,80:1. O texto auxiliar usava `stone-400` (2,52:1, reprovado) e foi trocado por `stone-500` em todas as telas, exceto na seção de apoio ao projeto da landing, que tem alterações suas em andamento.
- **Selo de pendências** no menu (Doações) com texto para leitor de tela, para o gestor perceber doações novas sem abrir a página.
- **Landmarks**: `main` com id, `nav` com `aria-label`.

## Próximos passos

1. Rodar o Lighthouse ou o axe nas telas para confirmar o contraste de cada combinação real (o cálculo acima cobre as cores-base, não cada composição de fundo).
2. Testar as telas novas com um leitor de tela (NVDA ou VoiceOver). Isso depende de uma pessoa com o leitor instalado.
3. Aplicar o questionário de usabilidade (por exemplo, SUS) nas validações presenciais.
