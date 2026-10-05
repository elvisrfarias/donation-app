import { useMemo, useState, type FormEvent } from 'react'
import { PackagePlus, Save, Tags } from 'lucide-react'
import type { CollectionPoint, StockItem } from '@/models/types'
import { useCreateStockItem, useUpdateStockItem } from '@/viewmodels'
import { Alert, Button, ComboBoxInput, EmptyState, FormField, Select } from '@/views/components/ui'
import { ModalComponent } from '@/views/components/modal'
import { CategoryManagerModal } from './CategoryManagerModal'
import { DEFAULT_ALERT_THRESHOLD, MAX_QUANTITY, clampQuantity, parseQuantity } from './stockDraft'

const UNIT_SUGGESTIONS = ['kg', 'un', 'L', 'pacote', 'caixa', 'g']

interface StockItemFormValues {
  name: string
  unit: string
  categoryId: string
  initialQuantity: number
  goal: number
  alertThreshold: number
}

function defaultValues(firstCategoryId: string): StockItemFormValues {
  return {
    name: '',
    unit: '',
    categoryId: firstCategoryId,
    initialQuantity: 0,
    goal: 0,
    alertThreshold: DEFAULT_ALERT_THRESHOLD,
  }
}

function valuesFromItem(item: StockItem): StockItemFormValues {
  return {
    name: item.name,
    unit: item.unit,
    categoryId: item.categoryId,
    initialQuantity: item.quantity,
    goal: item.goal,
    alertThreshold: item.alertThreshold,
  }
}

function validate(values: StockItemFormValues): string | null {
  if (values.name.trim().length === 0) return 'Informe o nome do item.'
  if (values.name.trim().length > 120) return 'O nome pode ter no máximo 120 caracteres.'
  if (values.unit.trim().length === 0) return 'Informe a unidade do item.'
  if (values.unit.trim().length > 20) return 'A unidade pode ter no máximo 20 caracteres.'
  if (values.goal > 0 && values.alertThreshold > values.goal) {
    return 'O aviso não pode ser maior que a meta.'
  }
  return null
}

interface StockItemFormModalProps {
  point: CollectionPoint
  /** Item a editar (metadados apenas — a quantidade se edita na aba Estoque). `null` cadastra um novo. */
  item: StockItem | null
  open: boolean
  onClose: () => void
}

export function StockItemFormModal({ point, item, open, onClose }: StockItemFormModalProps) {
  const isEditing = item !== null
  const categories = point.categories
  const {
    createStockItem,
    isPending: isCreating,
    error: createError,
    reset: resetCreate,
  } = useCreateStockItem()
  const {
    updateStockItem,
    isPending: isUpdating,
    error: updateError,
    reset: resetUpdate,
  } = useUpdateStockItem()

  const [values, setValues] = useState<StockItemFormValues>(() =>
    item ? valuesFromItem(item) : defaultValues(categories[0]?.id ?? ''),
  )
  const [categoryManagerOpen, setCategoryManagerOpen] = useState(false)

  // Se o catálogo de categorias estava vazio quando o formulário abriu (categoryId ainda ''), e uma
  // categoria acabou de ser criada (ex.: via "Criar categoria" abaixo), usa a primeira como padrão —
  // derivado no render, sem precisar sincronizar o rascunho via efeito.
  const selectedCategoryId =
    values.categoryId !== '' ? values.categoryId : (categories[0]?.id ?? '')

  const isPending = isCreating || isUpdating
  const error = createError ?? updateError
  const noCategoriesYet = categories.length === 0
  const validationError = noCategoriesYet
    ? 'Cadastre ao menos uma categoria antes de adicionar um item.'
    : validate({ ...values, categoryId: selectedCategoryId })

  function set<K extends keyof StockItemFormValues>(key: K, value: StockItemFormValues[K]) {
    setValues((previous) => ({ ...previous, [key]: value }))
  }

  function handleOpenChange(nextOpen: boolean) {
    if (!nextOpen) {
      resetCreate()
      resetUpdate()
      onClose()
    }
  }

  async function submit(event: FormEvent) {
    event.preventDefault()
    if (validationError || isPending) return

    const name = values.name.trim()
    const unit = values.unit.trim()

    if (isEditing && item) {
      await updateStockItem({
        pointId: point.id,
        stockItemId: item.id,
        input: {
          name,
          unit,
          categoryId: selectedCategoryId,
          goal: values.goal,
          alertThreshold: values.alertThreshold,
          isTracked: item.isTracked,
        },
      })
    } else {
      await createStockItem({
        pointId: point.id,
        input: {
          name,
          unit,
          categoryId: selectedCategoryId,
          initialQuantity: values.initialQuantity,
          goal: values.goal,
          alertThreshold: values.alertThreshold,
          isTracked: true,
        },
      })
    }
    onClose()
  }

  const conflictMessage = useMemo(
    () =>
      error?.isConflict
        ? 'Já existe um item com este nome neste ponto. Escolha outro nome.'
        : (error?.displayMessage ?? null),
    [error],
  )

  return (
    <ModalComponent
      open={open}
      onOpenChange={handleOpenChange}
      title={isEditing ? `Editar item · ${item?.name}` : 'Adicionar item de estoque'}
      description={
        isEditing
          ? 'Ajuste nome, unidade, categoria e metas. A quantidade se edita na aba Estoque.'
          : 'Cadastre um novo item no catálogo de estoque deste ponto.'
      }
      size="md"
    >
      <form onSubmit={submit} className="space-y-4 py-1">
        {conflictMessage && <Alert tone="error">{conflictMessage}</Alert>}

        <FormField label="Nome" htmlFor="stock-item-name" required>
          <input
            id="stock-item-name"
            type="text"
            value={values.name}
            maxLength={120}
            disabled={isPending}
            onChange={(event) => set('name', event.target.value)}
            className="focus:border-brand-500 focus:ring-brand-500/10 h-11 w-full rounded-xl border border-stone-200 px-3.5 text-sm text-stone-900 transition-all focus:ring-4 focus:outline-none disabled:bg-stone-50"
            placeholder="Ex.: Arroz 1kg"
          />
        </FormField>

        <div className="grid gap-4 sm:grid-cols-2">
          <FormField label="Unidade" htmlFor="stock-item-unit" required>
            <ComboBoxInput
              id="stock-item-unit"
              value={values.unit}
              maxLength={20}
              disabled={isPending}
              onValueChange={(value) => set('unit', value)}
              suggestions={UNIT_SUGGESTIONS}
              placeholder="Ex.: kg, un, pacote"
            />
          </FormField>

          <FormField label="Categoria" htmlFor="stock-item-category" required>
            <Select
              id="stock-item-category"
              value={selectedCategoryId}
              disabled={isPending || noCategoriesYet}
              onValueChange={(value) => set('categoryId', value)}
            >
              {categories.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.name}
                </option>
              ))}
            </Select>
          </FormField>
        </div>

        {noCategoriesYet && (
          <EmptyState
            icon={<Tags className="size-6" />}
            title="Nenhuma categoria cadastrada"
            description="Crie a primeira categoria deste ponto antes de adicionar um item de estoque."
            action={
              <Button
                type="button"
                variant="secondary"
                size="sm"
                onClick={() => setCategoryManagerOpen(true)}
              >
                Criar categoria
              </Button>
            }
          />
        )}

        <div className={isEditing ? 'grid gap-4 sm:grid-cols-2' : 'grid gap-4 sm:grid-cols-3'}>
          {!isEditing && (
            <FormField label="Quantidade inicial" htmlFor="stock-item-initial-quantity">
              <input
                id="stock-item-initial-quantity"
                type="number"
                min={0}
                max={MAX_QUANTITY}
                inputMode="numeric"
                value={values.initialQuantity}
                disabled={isPending}
                onFocus={(event) => event.target.select()}
                onChange={(event) => set('initialQuantity', parseQuantity(event.target.value))}
                className="focus:border-brand-500 focus:ring-brand-500/10 h-11 w-full rounded-xl border border-stone-200 px-3.5 text-center text-sm font-semibold text-stone-900 tabular-nums transition-all focus:ring-4 focus:outline-none disabled:bg-stone-50"
              />
            </FormField>
          )}
          <FormField label="Meta" htmlFor="stock-item-goal" hint="0 = sem meta">
            <input
              id="stock-item-goal"
              type="number"
              min={0}
              max={MAX_QUANTITY}
              inputMode="numeric"
              value={values.goal}
              disabled={isPending}
              onFocus={(event) => event.target.select()}
              onChange={(event) => set('goal', parseQuantity(event.target.value))}
              className="focus:border-brand-500 focus:ring-brand-500/10 h-11 w-full rounded-xl border border-stone-200 px-3.5 text-center text-sm font-semibold text-stone-900 tabular-nums transition-all focus:ring-4 focus:outline-none disabled:bg-stone-50"
            />
          </FormField>
          <FormField label="Aviso" htmlFor="stock-item-alert" hint="estoque baixo abaixo disso">
            <input
              id="stock-item-alert"
              type="number"
              min={0}
              max={MAX_QUANTITY}
              inputMode="numeric"
              value={values.alertThreshold}
              disabled={isPending}
              onFocus={(event) => event.target.select()}
              onChange={(event) =>
                set('alertThreshold', clampQuantity(Number(event.target.value) || 0))
              }
              className="focus:border-brand-500 focus:ring-brand-500/10 h-11 w-full rounded-xl border border-stone-200 px-3.5 text-center text-sm font-semibold text-stone-900 tabular-nums transition-all focus:ring-4 focus:outline-none disabled:bg-stone-50"
            />
          </FormField>
        </div>

        {validationError && !noCategoriesYet && (
          <p className="text-xs font-medium text-red-600">{validationError}</p>
        )}

        <Button
          type="submit"
          variant="brand"
          leftIcon={isEditing ? <Save className="size-4" /> : <PackagePlus className="size-4" />}
          loading={isPending}
          disabled={!!validationError || isPending}
          className="w-full"
        >
          {isEditing ? 'Salvar item' : 'Adicionar item'}
        </Button>
      </form>

      <CategoryManagerModal
        point={point}
        canEdit
        open={categoryManagerOpen}
        startInCreation={noCategoriesYet}
        onClose={() => setCategoryManagerOpen(false)}
      />
    </ModalComponent>
  )
}
