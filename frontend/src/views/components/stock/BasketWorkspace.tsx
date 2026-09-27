import { useEffect, useState } from 'react'
import { ListChecks, Plus, ShoppingBasket } from 'lucide-react'
import type { BasketTemplate, CollectionPoint } from '@/models/types'
import { cn } from '@/lib/utils'
import { Badge, Button, Card, EmptyState } from '@/views/components/ui'
import {
  activeBasketTemplates,
  basketsPossibleForItem,
  bottleneckItemIds,
  describeNextBasketGap,
  unitsByItemId,
} from './basketDraft'
import { BasketTemplateManagerModal } from './BasketTemplateManagerModal'

interface BasketWorkspaceProps {
  point: CollectionPoint
  canEdit: boolean
}

function TemplateSelector({
  templates,
  selectedId,
  onSelect,
}: {
  templates: BasketTemplate[]
  selectedId: string | null
  onSelect: (id: string) => void
}) {
  return (
    <div role="radiogroup" aria-label="Receita de cesta" className="flex flex-wrap gap-2">
      {templates.map((template) => {
        const isSelected = template.id === selectedId
        return (
          <button
            key={template.id}
            type="button"
            role="radio"
            aria-checked={isSelected}
            onClick={() => onSelect(template.id)}
            className={cn(
              'inline-flex items-center gap-1.5 rounded-full px-4 py-2 text-sm font-semibold ring-1 ring-inset transition-colors',
              isSelected
                ? 'bg-brand-600 text-white ring-brand-600'
                : 'bg-white text-stone-600 ring-stone-200 hover:bg-brand-50 hover:text-brand-700 hover:ring-brand-200',
            )}
          >
            {template.name}
            <span className={cn('tabular-nums', isSelected ? 'text-white/80' : 'text-stone-400')}>
              ({template.feasibility.completeBaskets})
            </span>
          </button>
        )
      })}
    </div>
  )
}

function BasketHero({ template, units }: { template: BasketTemplate; units: Record<string, string> }) {
  const { completeBaskets, missing } = template.feasibility
  const gapMessage = describeNextBasketGap(missing, units)

  return (
    <Card className="space-y-3 p-6">
      <div className="flex items-center gap-4">
        <span className="from-brand-500 to-brand-700 shadow-brand grid size-14 shrink-0 place-items-center rounded-2xl bg-linear-to-br text-white">
          <ShoppingBasket className="size-7" />
        </span>
        <div>
          <p className="text-4xl font-extrabold tabular-nums text-stone-900">{completeBaskets}</p>
          <p className="text-sm font-semibold text-stone-600">
            {completeBaskets === 1 ? 'cesta completa disponível' : 'cestas completas disponíveis'}
            {' · '}
            {template.name}
          </p>
        </div>
      </div>
      <p className="text-sm text-stone-500">
        {gapMessage ?? 'Você já tem itens suficientes para montar mais uma cesta.'}
      </p>
    </Card>
  )
}

function BasketBreakdownTable({ template }: { template: BasketTemplate }) {
  const { completeBaskets, missing } = template.feasibility
  const bottlenecks = bottleneckItemIds(missing, completeBaskets)

  return (
    <Card className="overflow-hidden p-0">
      <div className="overflow-x-auto">
        <table className="w-full text-sm whitespace-nowrap">
          <thead className="bg-stone-50 text-xs font-semibold tracking-wide text-stone-500 uppercase">
            <tr>
              <th className="px-4 py-3 text-left">Item</th>
              <th className="px-4 py-3 text-right">Precisa por cesta</th>
              <th className="px-4 py-3 text-right">Estoque atual</th>
              <th className="px-4 py-3 text-right">Dá para</th>
              <th className="px-4 py-3 text-left">Status</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-stone-100">
            {missing.map((gap) => {
              const isBottleneck = bottlenecks.has(gap.itemId)
              const possible = basketsPossibleForItem(gap)
              return (
                <tr key={gap.itemId} className={cn(isBottleneck && 'bg-amber-50/60')}>
                  <td
                    className={cn(
                      'px-4 py-3 font-medium text-stone-900',
                      isBottleneck && 'border-l-4 border-amber-500',
                    )}
                  >
                    <span className="flex items-center gap-2">
                      {gap.itemName}
                      {isBottleneck && (
                        <Badge
                          tone="amber"
                          title="Este item é quem limita o total de cestas: quando ele acabar, você não consegue montar mais nenhuma, mesmo sobrando os outros itens da receita."
                        >
                          Limita a cesta
                        </Badge>
                      )}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right tabular-nums text-stone-600">
                    {gap.requiredPerBasket}
                  </td>
                  <td className="px-4 py-3 text-right tabular-nums text-stone-600">
                    {gap.currentQuantity}
                  </td>
                  <td className="px-4 py-3 text-right font-semibold tabular-nums text-stone-900">
                    {Number.isFinite(possible) ? possible : '—'}
                  </td>
                  <td className="px-4 py-3">
                    {gap.neededForNextBasket > 0 ? (
                      <Badge tone="amber">Faltam {gap.neededForNextBasket}</Badge>
                    ) : (
                      <Badge tone="green">OK</Badge>
                    )}
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>
    </Card>
  )
}

export function BasketWorkspace({ point, canEdit }: BasketWorkspaceProps) {
  const templates = point.basketTemplates
  const active = activeBasketTemplates(templates)
  const units = unitsByItemId(point.stockItems)

  const [selectedId, setSelectedId] = useState<string | null>(active[0]?.id ?? null)
  const [managerOpen, setManagerOpen] = useState(false)
  const [managerStartsInCreation, setManagerStartsInCreation] = useState(false)

  useEffect(() => {
    if (selectedId && active.some((template) => template.id === selectedId)) return
    // A receita selecionada deixou de existir/ficou inativa (editada ou removida em "Gerenciar
    // receitas") — cai para a primeira ativa. Reset intencional, não sincronização externa.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setSelectedId(active[0]?.id ?? null)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [active.map((template) => template.id).join(',')])

  function openManager(startInCreation: boolean) {
    setManagerStartsInCreation(startInCreation)
    setManagerOpen(true)
  }

  if (templates.length === 0) {
    return (
      <div className="space-y-4">
        <EmptyState
          icon={<ShoppingBasket className="size-6" />}
          title="Nenhuma receita de cesta cadastrada"
          description="Cadastre uma receita (ex.: arroz, feijão, açúcar e óleo) para ver quantas cestas completas o estoque atual permite montar."
          action={
            canEdit && (
              <Button
                variant="brand"
                leftIcon={<Plus className="size-4" />}
                onClick={() => openManager(true)}
              >
                Criar receita de cesta
              </Button>
            )
          }
        />
        <BasketTemplateManagerModal
          point={point}
          canEdit={canEdit}
          open={managerOpen}
          startInCreation={managerStartsInCreation}
          onClose={() => setManagerOpen(false)}
        />
      </div>
    )
  }

  const selected = active.find((template) => template.id === selectedId) ?? null

  return (
    <div className="space-y-4">
      {active.length > 1 && (
        <TemplateSelector templates={active} selectedId={selectedId} onSelect={setSelectedId} />
      )}

      {active.length === 0 && (
        <EmptyState
          icon={<ShoppingBasket className="size-6" />}
          title="Nenhuma receita ativa"
          description="Todas as receitas deste ponto estão desativadas. Ative uma em “Gerenciar receitas” para ver a viabilidade de montagem."
        />
      )}

      {selected && (
        <>
          <BasketHero template={selected} units={units} />
          <BasketBreakdownTable template={selected} />
        </>
      )}

      <div className="flex justify-end">
        <Button
          type="button"
          variant="secondary"
          leftIcon={<ListChecks className="size-4" />}
          onClick={() => openManager(false)}
        >
          Gerenciar receitas
        </Button>
      </div>

      <BasketTemplateManagerModal
        point={point}
        canEdit={canEdit}
        open={managerOpen}
        startInCreation={managerStartsInCreation}
        onClose={() => setManagerOpen(false)}
      />
    </div>
  )
}
