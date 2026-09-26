import { useMemo, useState, type FormEvent } from 'react'
import { RotateCcw, Save } from 'lucide-react'
import {
  cycleTypeLabels,
  itemCategoryLabels,
  itemCategoryValues,
  type CollectionPoint,
  type CycleType,
  type ItemCategory,
} from '@/models/types'
import { cn } from '@/lib/utils'
import { Checkbox } from '@/components/ui/checkbox'
import { useUpdateStockSettings } from '@/viewmodels'
import { Alert, Button, Card, FormField, Select } from '@/views/components/ui'
import { categoryIcons } from './categoryIcons'
import {
  MAX_QUANTITY,
  DEFAULT_MAX_WITHDRAWALS_PER_CYCLE,
  MAX_WITHDRAWALS_PER_CYCLE,
  countChangedSettings,
  isRuleChanged,
  parseQuantity,
  ruleError,
  settingError,
  toStockSettingItems,
  toStockSettings,
  toWithdrawalRule,
  type ResolvedStockSetting,
  type StockSettings,
  type WithdrawalRuleDraft,
} from './stockDraft'
import { useWarnBeforeLeaving } from './useWarnBeforeLeaving'

interface StockSettingsEditorProps {
  point: CollectionPoint
  canEdit: boolean
}

const numberInput =
  'focus:border-brand-500 focus:ring-brand-500/10 h-10 w-full [appearance:textfield] rounded-xl border border-stone-200 px-3 text-center text-sm font-semibold text-stone-900 tabular-nums transition-all focus:ring-4 focus:outline-none disabled:bg-stone-50 disabled:text-stone-400 aria-invalid:border-red-300 [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none'

const rowGrid = 'grid grid-cols-2 items-center gap-x-4 gap-y-3 sm:grid-cols-[1fr_7rem_7rem_7rem]'

function describePendingChanges(changedCategories: number, ruleChanged: boolean): string {
  const parts: string[] = []
  if (changedCategories > 0) {
    parts.push(
      `${changedCategories} ${changedCategories === 1 ? 'categoria alterada' : 'categorias alteradas'}`,
    )
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
  category,
  setting,
  disabled,
  onChange,
}: {
  category: ItemCategory
  setting: ResolvedStockSetting
  disabled: boolean
  onChange: (next: ResolvedStockSetting) => void
}) {
  const label = itemCategoryLabels[category]
  const Icon = categoryIcons[category]
  const error = settingError(setting)
  const numbersDisabled = disabled || !setting.isTracked

  return (
    <li className="border-t border-stone-100 py-3 first:border-t-0">
      <div className={rowGrid}>
        <div className="flex min-w-0 items-center gap-3 sm:order-1">
          <span className="grid size-10 shrink-0 place-items-center rounded-xl bg-stone-100 text-stone-600">
            <Icon className="size-5" />
          </span>
          <p className="truncate text-sm font-semibold text-stone-900">{label}</p>
        </div>

        <label className="flex items-center justify-end gap-2 text-xs font-medium text-stone-500 sm:order-4 sm:justify-center">
          <span className="sm:sr-only">Acompanhar</span>
          <Checkbox
            checked={setting.isTracked}
            disabled={disabled}
            aria-label={`Acompanhar ${label}`}
            onCheckedChange={(checked) => onChange({ ...setting, isTracked: checked === true })}
          />
        </label>

        <NumberField
          label="Meta"
          ariaLabel={`Meta de ${label}`}
          value={setting.goal}
          disabled={numbersDisabled}
          invalid={!!error}
          className="sm:order-2"
          onChange={(goal) => onChange({ ...setting, goal })}
        />
        <NumberField
          label="Aviso"
          ariaLabel={`Aviso de ${label}`}
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

export function StockSettingsEditor({ point, canEdit }: StockSettingsEditorProps) {
  const { updateStockSettings, isPending, error, reset } = useUpdateStockSettings()
  const savedSettings = useMemo(() => toStockSettings(point), [point])
  const savedRule = useMemo(() => toWithdrawalRule(point), [point])
  const [draft, setDraft] = useState<StockSettings>(savedSettings)
  const [ruleDraft, setRuleDraft] = useState<WithdrawalRuleDraft>(savedRule)
  const [justSaved, setJustSaved] = useState(false)

  const changedCount = countChangedSettings(savedSettings, draft)
  const ruleChanged = isRuleChanged(savedRule, ruleDraft)
  const isDirty = changedCount > 0 || ruleChanged
  const currentRuleError = ruleError(ruleDraft)
  const hasInvalidSetting =
    currentRuleError !== null ||
    itemCategoryValues.some((category) => settingError(draft[category]) !== null)
  useWarnBeforeLeaving(isDirty)

  function changeSetting(category: ItemCategory, next: ResolvedStockSetting) {
    setJustSaved(false)
    reset()
    setDraft((previous) => ({ ...previous, [category]: next }))
  }

  function changeRule(next: WithdrawalRuleDraft) {
    setJustSaved(false)
    reset()
    setRuleDraft(next)
  }

  function discardChanges() {
    setJustSaved(false)
    reset()
    setDraft(savedSettings)
    setRuleDraft(savedRule)
  }

  function restoreDefaults() {
    setJustSaved(false)
    reset()
    setDraft(toStockSettings({ ...point, stockSettings: [] }))
    setRuleDraft({ cycleType: 'Annual', maxWithdrawalsPerCycle: DEFAULT_MAX_WITHDRAWALS_PER_CYCLE })
  }

  async function save(event: FormEvent) {
    event.preventDefault()
    if (!isDirty || hasInvalidSetting || isPending) return
    await updateStockSettings({
      id: point.id,
      input: {
        items: toStockSettingItems(draft),
        cycleType: ruleDraft.cycleType,
        maxWithdrawalsPerCycle: ruleDraft.maxWithdrawalsPerCycle,
      },
    })
    setJustSaved(true)
  }

  return (
    <form onSubmit={save} className="space-y-4">
      <Card className="space-y-4 p-5">
        <div>
          <h3 className="text-base font-bold text-stone-900">Regra de retirada</h3>
          <p className="mt-1 text-sm text-stone-500">
            Quantas vezes cada família pode retirar neste ponto em cada ciclo. O histórico é somado
            entre todos os pontos do município, mas o limite aplicado é o do ponto onde a retirada
            acontece.
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
              className="focus:border-brand-500 focus:ring-brand-500/10 h-11 w-full rounded-xl border border-stone-200 px-3.5 text-sm text-stone-900 tabular-nums transition-all focus:ring-4 focus:outline-none disabled:bg-stone-50 disabled:text-stone-400 aria-invalid:border-red-300"
            />
          </FormField>
        </div>
      </Card>

      <Card className="space-y-2 p-5">
        <h3 className="text-base font-bold text-stone-900">Metas e avisos do estoque</h3>
        <ul className="space-y-1 text-sm text-stone-500">
          <li>
            <strong className="font-semibold text-stone-700">Meta:</strong> quantidade que o ponto
            quer manter. Use 0 para não ter meta.
          </li>
          <li>
            <strong className="font-semibold text-stone-700">Aviso:</strong> a partir dessa
            quantidade (ou menos) a categoria aparece como “Estoque baixo” e entra na lista de
            reposição.
          </li>
          <li>
            <strong className="font-semibold text-stone-700">Acompanhar:</strong> desmarque as
            categorias que o ponto não trabalha; elas somem da aba Estoque enquanto estiverem
            zeradas.
          </li>
        </ul>
      </Card>

      {error && <Alert tone="error">{error.displayMessage}</Alert>}
      {justSaved && !isDirty && <Alert tone="success">Configurações de estoque salvas.</Alert>}

      <Card className="p-5">
        <div
          className={cn(
            rowGrid,
            'hidden pb-2 text-xs font-semibold tracking-wide text-stone-400 uppercase sm:grid',
          )}
        >
          <span>Categoria</span>
          <span className="text-center">Meta</span>
          <span className="text-center">Aviso</span>
          <span className="text-center">Acompanhar</span>
        </div>
        <ul>
          {itemCategoryValues.map((category) => (
            <SettingRow
              key={category}
              category={category}
              setting={draft[category]}
              disabled={!canEdit || isPending}
              onChange={(next) => changeSetting(category, next)}
            />
          ))}
        </ul>
      </Card>

      {canEdit ? (
        <div className="shadow-lift sticky bottom-20 z-10 flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-stone-200 bg-white/95 p-3 backdrop-blur-md lg:bottom-4">
          <Button
            type="button"
            variant="ghost"
            size="sm"
            leftIcon={<RotateCcw className="size-4" />}
            onClick={restoreDefaults}
            disabled={isPending}
          >
            Restaurar padrões
          </Button>
          <div className="flex items-center gap-2">
            <p className="text-sm font-medium text-stone-600">
              {isDirty
                ? describePendingChanges(changedCount, ruleChanged)
                : 'Nenhuma alteração pendente'}
            </p>
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
