import { useMemo, useState, type FormEvent } from 'react'
import { ChevronDown, Save } from 'lucide-react'
import { cycleTypeLabels, type CollectionPoint, type CycleType } from '@/models/types'
import { cn } from '@/lib/utils'
import { Checkbox } from '@/components/ui/checkbox'
import { useUpdateStockItemSettings, useUpdateWithdrawalRule } from '@/viewmodels'
import { Alert, Button, Card, FormField, Select } from '@/views/components/ui'
import { FALLBACK_ICON, ICON_BY_KEY } from './categoryIcons'
import {
  MAX_QUANTITY,
  MAX_WITHDRAWALS_PER_CYCLE,
  changedSettingIds,
  groupByCategory,
  isRuleChanged,
  mergeSettingsDraft,
  parseQuantity,
  ruleError,
  settingError,
  toSettingEntries,
  toSettingsDraft,
  toWithdrawalRule,
  type ItemSetting,
  type SettingsDraft,
  type StockItemCategoryGroup,
  type WithdrawalRuleDraft,
} from './stockDraft'
import { useWarnBeforeLeaving } from './useWarnBeforeLeaving'

interface StockSettingsEditorProps {
  point: CollectionPoint
  canEdit: boolean
}

const numberInput =
  'focus:border-brand-500 focus:ring-brand-500/10 h-10 w-full [appearance:textfield] rounded-xl border border-stone-200 px-3 text-center text-sm font-semibold text-stone-900 tabular-nums transition-all focus:ring-4 focus:outline-none disabled:bg-stone-50 disabled:text-stone-500 aria-invalid:border-red-300 [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none'

const rowGrid = 'grid grid-cols-2 items-center gap-x-4 gap-y-3 sm:grid-cols-[1fr_7rem_7rem_7rem]'

function describePendingChanges(changedItems: number, ruleChanged: boolean): string {
  const parts: string[] = []
  if (changedItems > 0) {
    parts.push(`${changedItems} ${changedItems === 1 ? 'item alterado' : 'itens alterados'}`)
  }
  if (ruleChanged) parts.push('regra de retirada alterada')
  return parts.join(' · ')
}

function NumberField({
  label,
  value,
  disabled,
  invalid,
  ariaLabel,
  className,
  onChange,
}: {
  label: string
  value: number
  disabled: boolean
  invalid: boolean
  ariaLabel: string
  className?: string
  onChange: (value: number) => void
}) {
  return (
    <label className={cn('flex flex-col gap-1 text-[11px] font-medium text-stone-500', className)}>
      <span className="sm:sr-only">{label}</span>
      <input
        type="number"
        min={0}
        max={MAX_QUANTITY}
        inputMode="numeric"
        value={value}
        disabled={disabled}
        aria-invalid={invalid}
        aria-label={ariaLabel}
        onChange={(event) => onChange(parseQuantity(event.target.value))}
        onFocus={(event) => event.target.select()}
        className={numberInput}
      />
    </label>
  )
}

function SettingRow({
  itemName,
  setting,
  disabled,
  onChange,
}: {
  itemName: string
  setting: ItemSetting
  disabled: boolean
  onChange: (next: ItemSetting) => void
}) {
  const error = settingError(setting)
  const numbersDisabled = disabled || !setting.isTracked

  return (
    <li className="border-t border-stone-100 py-3 first:border-t-0">
      <div className={rowGrid}>
        <div className="flex min-w-0 items-center gap-3 sm:order-1">
          <p className="truncate text-sm font-semibold text-stone-900">{itemName}</p>
        </div>

        <label className="flex items-center justify-end gap-2 text-xs font-medium text-stone-500 sm:order-4 sm:justify-center">
          <span className="sm:sr-only">Acompanhar</span>
          <Checkbox
            checked={setting.isTracked}
            disabled={disabled}
            aria-label={`Acompanhar ${itemName}`}
            onCheckedChange={(checked) => onChange({ ...setting, isTracked: checked === true })}
          />
        </label>

        <NumberField
          label="Meta"
          ariaLabel={`Meta de ${itemName}`}
          value={setting.goal}
          disabled={numbersDisabled}
          invalid={!!error}
          className="sm:order-2"
          onChange={(goal) => onChange({ ...setting, goal })}
        />
        <NumberField
          label="Aviso"
          ariaLabel={`Aviso de ${itemName}`}
          value={setting.alertThreshold}
          disabled={numbersDisabled}
          invalid={!!error}
          className="sm:order-3"
          onChange={(alertThreshold) => onChange({ ...setting, alertThreshold })}
        />
      </div>
      {error && <p className="mt-1.5 text-xs font-medium text-red-600">{error}</p>}
    </li>
  )
}

function SettingsCategorySection({
  group,
  draft,
  disabled,
  expanded,
  onToggle,
  onChange,
}: {
  group: StockItemCategoryGroup
  draft: SettingsDraft
  disabled: boolean
  expanded: boolean
  onToggle: () => void
  onChange: (itemId: string, next: ItemSetting) => void
}) {
  const Icon = ICON_BY_KEY[group.category.icon] ?? FALLBACK_ICON

  return (
    <Card className="p-0">
      <button
        type="button"
        onClick={onToggle}
        aria-expanded={expanded}
        className="flex w-full items-center gap-3 p-4 text-left"
      >
        <span className="grid size-9 shrink-0 place-items-center rounded-xl bg-stone-100 text-stone-600">
          <Icon className="size-4.5" />
        </span>
        <span className="min-w-0 flex-1">
          <span className="block truncate text-sm font-bold text-stone-900">
            {group.category.name}
          </span>
          <span className="block text-xs text-stone-500">
            {group.items.length} {group.items.length === 1 ? 'item' : 'itens'}
          </span>
        </span>
        <ChevronDown
          className={cn(
            'size-4 shrink-0 text-stone-500 transition-transform',
            expanded && 'rotate-180',
          )}
        />
      </button>
      {expanded && (
        <div className="border-t border-stone-100 p-4">
          <div
            className={cn(
              rowGrid,
              'hidden pb-2 text-xs font-semibold tracking-wide text-stone-500 uppercase sm:grid',
            )}
          >
            <span>Item</span>
            <span className="text-center">Meta</span>
            <span className="text-center">Aviso</span>
            <span className="text-center">Acompanhar</span>
          </div>
          <ul>
            {group.items.map((item) => (
              <SettingRow
                key={item.id}
                itemName={item.name}
                setting={draft[item.id]}
                disabled={disabled}
                onChange={(next) => onChange(item.id, next)}
              />
            ))}
          </ul>
        </div>
      )}
    </Card>
  )
}

export function StockSettingsEditor({ point, canEdit }: StockSettingsEditorProps) {
  const {
    updateStockItemSettings,
    isPending: isSavingSettings,
    error: settingsError,
    reset: resetSettings,
  } = useUpdateStockItemSettings()
  const {
    updateWithdrawalRule,
    isPending: isSavingRule,
    error: withdrawalRuleError,
    reset: resetRule,
  } = useUpdateWithdrawalRule()

  const items = point.stockItems
  const savedSettings = useMemo(() => toSettingsDraft(items), [items])
  const savedRule = useMemo(() => toWithdrawalRule(point), [point])
  const [draft, setDraft] = useState<SettingsDraft>(savedSettings)
  const [ruleDraft, setRuleDraft] = useState<WithdrawalRuleDraft>(savedRule)
  const [justSaved, setJustSaved] = useState(false)
  const [collapsedOverrides, setCollapsedOverrides] = useState<Record<string, boolean>>({})

  const isPending = isSavingSettings || isSavingRule
  const error = settingsError ?? withdrawalRuleError

  const visibleSettings = useMemo(() => mergeSettingsDraft(draft, items), [draft, items])
  const changedIds = changedSettingIds(savedSettings, visibleSettings)
  const ruleChanged = isRuleChanged(savedRule, ruleDraft)
  const isDirty = changedIds.length > 0 || ruleChanged
  const currentRuleError = ruleError(ruleDraft)
  const hasInvalidSetting =
    currentRuleError !== null ||
    items.some((item) => settingError(visibleSettings[item.id]) !== null)
  useWarnBeforeLeaving(isDirty)

  const groups = useMemo(
    () => groupByCategory(items, point.categories).filter((group) => group.items.length > 0),
    [items, point.categories],
  )

  function isExpanded(categoryId: string): boolean {
    return collapsedOverrides[categoryId] !== true
  }

  function toggleCategory(categoryId: string) {
    setCollapsedOverrides((previous) => ({ ...previous, [categoryId]: isExpanded(categoryId) }))
  }

  function changeSetting(itemId: string, next: ItemSetting) {
    setJustSaved(false)
    resetSettings()
    setDraft((previous) => ({ ...previous, [itemId]: next }))
  }

  function changeRule(next: WithdrawalRuleDraft) {
    setJustSaved(false)
    resetRule()
    setRuleDraft(next)
  }

  function discardChanges() {
    setJustSaved(false)
    resetSettings()
    resetRule()
    setDraft(savedSettings)
    setRuleDraft(savedRule)
  }

  async function save(event: FormEvent) {
    event.preventDefault()
    if (!isDirty || hasInvalidSetting || isPending) return

    if (changedIds.length > 0) {
      await updateStockItemSettings({
        pointId: point.id,
        input: { items: toSettingEntries(visibleSettings) },
      })
    }
    if (ruleChanged) {
      await updateWithdrawalRule({ pointId: point.id, input: ruleDraft })
    }
    setJustSaved(true)
  }

  return (
    <form onSubmit={save} className="space-y-4">
      <Card className="space-y-4 p-5">
        <div>
          <h3 className="text-base font-bold text-stone-900">Regra de retirada</h3>
          <p className="mt-1 text-sm text-stone-500">
            Quantas vezes cada família pode retirar cesta básica neste ponto em cada ciclo. O
            histórico é somado entre todos os pontos do município, mas o limite aplicado é o do
            ponto onde a retirada acontece.
          </p>
        </div>
        <div className="grid gap-4 sm:grid-cols-2">
          <FormField label="Ciclo" htmlFor="withdrawal-cycle-type" required>
            <Select
              id="withdrawal-cycle-type"
              value={ruleDraft.cycleType}
              disabled={!canEdit || isPending}
              onValueChange={(value) => changeRule({ ...ruleDraft, cycleType: value as CycleType })}
            >
              {Object.entries(cycleTypeLabels).map(([value, label]) => (
                <option key={value} value={value}>
                  {label}
                </option>
              ))}
            </Select>
          </FormField>
          <FormField
            label="Máximo por família em cada ciclo"
            required
            htmlFor="withdrawal-cycle-max"
            error={currentRuleError ?? undefined}
          >
            <input
              id="withdrawal-cycle-max"
              type="number"
              min={1}
              max={MAX_WITHDRAWALS_PER_CYCLE}
              inputMode="numeric"
              value={ruleDraft.maxWithdrawalsPerCycle}
              disabled={!canEdit || isPending}
              aria-invalid={currentRuleError !== null}
              onChange={(event) =>
                changeRule({
                  ...ruleDraft,
                  maxWithdrawalsPerCycle: parseQuantity(event.target.value),
                })
              }
              onFocus={(event) => event.target.select()}
              className="focus:border-brand-500 focus:ring-brand-500/10 h-11 w-full rounded-xl border border-stone-200 px-3.5 text-sm text-stone-900 tabular-nums transition-all focus:ring-4 focus:outline-none disabled:bg-stone-50 disabled:text-stone-500 aria-invalid:border-red-300"
            />
          </FormField>
        </div>
      </Card>

      <Card className="space-y-2 p-5">
        <h3 className="text-base font-bold text-stone-900">Metas e avisos do estoque</h3>
        <ul className="space-y-1 text-sm text-stone-500">
          <li>
            <strong className="font-semibold text-stone-700">Meta:</strong> quantidade que o ponto
            quer manter deste item. Use 0 para não ter meta.
          </li>
          <li>
            <strong className="font-semibold text-stone-700">Aviso:</strong> a partir dessa
            quantidade (ou menos) o item aparece como “Estoque baixo” e entra na lista de reposição.
          </li>
          <li>
            <strong className="font-semibold text-stone-700">Acompanhar:</strong> desmarque itens
            que o ponto não quer que contem nos indicadores de meta/aviso.
          </li>
        </ul>
      </Card>

      {error && <Alert tone="error">{error.displayMessage}</Alert>}
      {justSaved && !isDirty && <Alert tone="success">Configurações de estoque salvas.</Alert>}

      {items.length === 0 ? (
        <Alert tone="info" title="Catálogo vazio">
          Adicione itens ao catálogo na aba Estoque para configurar metas e avisos.
        </Alert>
      ) : (
        <div className="space-y-3">
          {groups.map((group) => (
            <SettingsCategorySection
              key={group.category.id}
              group={group}
              draft={visibleSettings}
              disabled={!canEdit || isPending}
              expanded={isExpanded(group.category.id)}
              onToggle={() => toggleCategory(group.category.id)}
              onChange={changeSetting}
            />
          ))}
        </div>
      )}

      {canEdit ? (
        <div className="shadow-lift sticky bottom-20 z-10 flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-stone-200 bg-white/95 p-3 backdrop-blur-md lg:bottom-4">
          <p className="pl-2 text-sm font-medium text-stone-600">
            {isDirty
              ? describePendingChanges(changedIds.length, ruleChanged)
              : 'Nenhuma alteração pendente'}
          </p>
          <div className="flex items-center gap-2">
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={discardChanges}
              disabled={!isDirty || isPending}
            >
              Descartar
            </Button>
            <Button
              type="submit"
              variant="brand"
              leftIcon={<Save className="size-4" />}
              loading={isPending}
              disabled={!isDirty || hasInvalidSetting}
            >
              {isPending ? 'Salvando…' : 'Salvar configurações'}
            </Button>
          </div>
        </div>
      ) : (
        <p className="text-sm text-stone-500">
          Apenas administradores e gestores de ponto podem configurar o estoque.
        </p>
      )}
    </form>
  )
}
