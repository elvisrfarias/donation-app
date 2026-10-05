import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { Minus, Pencil, Plus, Power, PowerOff, Save, ShoppingBasket, Trash2, X } from 'lucide-react'
import type { BasketTemplate, CollectionPoint } from '@/models/types'
import { Checkbox } from '@/components/ui/checkbox'
import { cn } from '@/lib/utils'
import {
  useCreateBasketTemplate,
  useRemoveBasketTemplate,
  useUpdateBasketTemplate,
} from '@/viewmodels'
import { Alert, Badge, Button, EmptyState, FormField, Select } from '@/views/components/ui'
import { ModalComponent } from '@/views/components/modal'
import {
  activeBasketTemplates,
  inactiveBasketTemplates,
  previewCompleteBaskets,
  type RecipeDraftItem,
} from './basketDraft'
import { MAX_QUANTITY, clampQuantity, parseQuantity } from './stockDraft'

interface BasketTemplateManagerModalProps {
  point: CollectionPoint
  canEdit: boolean
  open: boolean
  /** Abre a modal já no formulário de criação (ex.: vindo do estado vazio da aba Cestas). */
  startInCreation: boolean
  onClose: () => void
}

interface RecipeFormState {
  name: string
  isActive: boolean
  items: RecipeDraftItem[]
}

function emptyForm(): RecipeFormState {
  return { name: '', isActive: true, items: [] }
}

function formFromTemplate(template: BasketTemplate): RecipeFormState {
  return {
    name: template.name,
    isActive: template.isActive,
    items: template.items.map((item) => ({
      stockItemId: item.stockItemId,
      itemName: item.itemName,
      unit: '',
      quantity: item.quantity,
    })),
  }
}

function validate(form: RecipeFormState): string | null {
  if (form.name.trim().length === 0) return 'Informe o nome da receita.'
  if (form.items.length === 0) return 'Adicione ao menos um item à receita.'
  if (form.items.some((item) => item.quantity <= 0)) {
    return 'A quantidade de cada item deve ser maior que zero.'
  }
  return null
}

function TemplateRow({
  template,
  canEdit,
  isDeleting,
  deleteErrorMessage,
  onEdit,
  onToggleActive,
  onDelete,
}: {
  template: BasketTemplate
  canEdit: boolean
  isDeleting: boolean
  deleteErrorMessage: string | null
  onEdit: () => void
  onToggleActive: () => void
  onDelete: () => void
}) {
  return (
    <li className="rounded-2xl border border-stone-200 bg-white p-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2">
            <p className="truncate text-sm font-bold text-stone-900">{template.name}</p>
            <Badge tone={template.isActive ? 'green' : 'slate'}>
              {template.isActive ? 'Ativa' : 'Inativa'}
            </Badge>
          </div>
          <p className="mt-0.5 text-xs text-stone-500">
            {template.items.length} {template.items.length === 1 ? 'item' : 'itens'} ·{' '}
            {template.feasibility.completeBaskets} cesta(s) completa(s) hoje
          </p>
        </div>
        {canEdit && (
          <div className="flex shrink-0 items-center gap-1">
            <Button
              variant="ghost"
              size="sm"
              leftIcon={<Pencil className="size-4" />}
              onClick={onEdit}
            >
              Editar
            </Button>
            <Button
              variant="ghost"
              size="sm"
              leftIcon={
                template.isActive ? <PowerOff className="size-4" /> : <Power className="size-4" />
              }
              onClick={onToggleActive}
            >
              {template.isActive ? 'Desativar' : 'Ativar'}
            </Button>
            <Button
              variant="ghost"
              size="sm"
              leftIcon={<Trash2 className="size-4" />}
              loading={isDeleting}
              onClick={onDelete}
              className="hover:bg-red-50 hover:text-red-600"
            >
              Excluir
            </Button>
          </div>
        )}
      </div>
      {deleteErrorMessage && (
        <Alert tone="warning" className="mt-3">
          {deleteErrorMessage}
        </Alert>
      )}
    </li>
  )
}

function RecipeItemRow({
  item,
  disabled,
  onChangeQuantity,
  onRemove,
}: {
  item: RecipeDraftItem
  disabled: boolean
  onChangeQuantity: (quantity: number) => void
  onRemove: () => void
}) {
  return (
    <li className="flex items-center gap-3 rounded-xl border border-stone-200 bg-white p-3">
      <div className="min-w-0 flex-1">
        <p className="truncate text-sm font-semibold text-stone-900">{item.itemName}</p>
        {item.unit && <p className="text-xs text-stone-500">{item.unit}</p>}
      </div>
      <div className="flex items-center gap-1.5">
        <button
          type="button"
          aria-label={`Diminuir quantidade de ${item.itemName}`}
          disabled={disabled || item.quantity <= 1}
          onClick={() => onChangeQuantity(clampQuantity(item.quantity - 1))}
          className="grid size-8 shrink-0 place-items-center rounded-lg text-stone-600 ring-1 ring-stone-200 ring-inset hover:bg-stone-50 disabled:opacity-40"
        >
          <Minus className="size-3.5" />
        </button>
        <input
          type="number"
          min={1}
          max={MAX_QUANTITY}
          inputMode="numeric"
          value={item.quantity}
          disabled={disabled}
          aria-label={`Quantidade de ${item.itemName} por cesta`}
          onFocus={(event) => event.target.select()}
          onChange={(event) => onChangeQuantity(Math.max(1, parseQuantity(event.target.value)))}
          className="h-8 w-14 [appearance:textfield] rounded-lg border border-stone-200 text-center text-sm font-semibold tabular-nums [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none"
        />
        <button
          type="button"
          aria-label={`Aumentar quantidade de ${item.itemName}`}
          disabled={disabled}
          onClick={() => onChangeQuantity(clampQuantity(item.quantity + 1))}
          className="grid size-8 shrink-0 place-items-center rounded-lg text-stone-600 ring-1 ring-stone-200 ring-inset hover:bg-stone-50 disabled:opacity-40"
        >
          <Plus className="size-3.5" />
        </button>
      </div>
      <button
        type="button"
        aria-label={`Remover ${item.itemName} da receita`}
        disabled={disabled}
        onClick={onRemove}
        className="grid size-8 shrink-0 place-items-center rounded-lg text-stone-400 hover:bg-red-50 hover:text-red-600"
      >
        <X className="size-4" />
      </button>
    </li>
  )
}

export function BasketTemplateManagerModal({
  point,
  canEdit,
  open,
  startInCreation,
  onClose,
}: BasketTemplateManagerModalProps) {
  const [mode, setMode] = useState<'list' | 'form'>('list')
  const [editingTemplate, setEditingTemplate] = useState<BasketTemplate | null>(null)
  const [form, setForm] = useState<RecipeFormState>(emptyForm())
  const [itemPicker, setItemPicker] = useState('')
  const [deleteTargetId, setDeleteTargetId] = useState<string | null>(null)

  const {
    createBasketTemplate,
    isPending: isCreating,
    error: createError,
    reset: resetCreate,
  } = useCreateBasketTemplate()
  const {
    updateBasketTemplate,
    isPending: isUpdating,
    error: updateError,
    reset: resetUpdate,
  } = useUpdateBasketTemplate()
  const {
    removeBasketTemplate,
    isPending: isDeleting,
    error: removeError,
    reset: resetRemove,
  } = useRemoveBasketTemplate()

  useEffect(() => {
    if (!open) return
    resetCreate()
    resetUpdate()
    resetRemove()
    // Reinicia o rascunho sempre que a modal reabre (componente persistente, não um remount) —
    // reset intencional, não sincronização com uma fonte externa.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setDeleteTargetId(null)
    setItemPicker('')
    if (startInCreation) {
      setEditingTemplate(null)
      setForm(emptyForm())
      setMode('form')
    } else {
      setMode('list')
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open])

  const stockItemsById = useMemo(
    () => new Map(point.stockItems.map((item) => [item.id, item])),
    [point.stockItems],
  )

  const templates = point.basketTemplates
  const orderedTemplates = useMemo(
    () => [...activeBasketTemplates(templates), ...inactiveBasketTemplates(templates)],
    [templates],
  )

  const pickableItems = point.stockItems.filter(
    (item) => !form.items.some((draftItem) => draftItem.stockItemId === item.id),
  )

  const isSubmitting = isCreating || isUpdating
  const submitError = createError ?? updateError
  const validationError = validate(form)
  const previewBaskets = previewCompleteBaskets(form.items, stockItemsById)

  function openCreateForm() {
    setEditingTemplate(null)
    setForm(emptyForm())
    setItemPicker('')
    setMode('form')
  }

  function openEditForm(template: BasketTemplate) {
    setEditingTemplate(template)
    setForm(formFromTemplate(template))
    setItemPicker('')
    setMode('form')
  }

  function addItem(stockItemId: string) {
    const stockItem = point.stockItems.find((item) => item.id === stockItemId)
    if (!stockItem) return
    setForm((previous) => ({
      ...previous,
      items: [
        ...previous.items,
        { stockItemId: stockItem.id, itemName: stockItem.name, unit: stockItem.unit, quantity: 1 },
      ],
    }))
    setItemPicker('')
  }

  function changeItemQuantity(stockItemId: string, quantity: number) {
    setForm((previous) => ({
      ...previous,
      items: previous.items.map((item) =>
        item.stockItemId === stockItemId ? { ...item, quantity } : item,
      ),
    }))
  }

  function removeItem(stockItemId: string) {
    setForm((previous) => ({
      ...previous,
      items: previous.items.filter((item) => item.stockItemId !== stockItemId),
    }))
  }

  async function submitForm(event: FormEvent) {
    event.preventDefault()
    if (validationError || isSubmitting) return

    const payload = {
      name: form.name.trim(),
      items: form.items.map((item) => ({ stockItemId: item.stockItemId, quantity: item.quantity })),
    }

    if (editingTemplate) {
      await updateBasketTemplate({
        pointId: point.id,
        templateId: editingTemplate.id,
        input: { ...payload, isActive: form.isActive },
      })
    } else {
      await createBasketTemplate({ pointId: point.id, input: payload })
    }
    setMode('list')
  }

  async function toggleActive(template: BasketTemplate) {
    await updateBasketTemplate({
      pointId: point.id,
      templateId: template.id,
      input: {
        name: template.name,
        items: template.items.map((item) => ({
          stockItemId: item.stockItemId,
          quantity: item.quantity,
        })),
        isActive: !template.isActive,
      },
    })
  }

  async function confirmDelete(template: BasketTemplate) {
    if (!window.confirm(`Remover a receita "${template.name}"?`)) return
    resetRemove()
    setDeleteTargetId(template.id)
    try {
      await removeBasketTemplate({ pointId: point.id, templateId: template.id })
      setDeleteTargetId(null)
    } catch {
      return
    }
  }

  return (
    <ModalComponent
      open={open}
      onOpenChange={(nextOpen) => !nextOpen && onClose()}
      title={
        mode === 'list'
          ? 'Gerenciar receitas de cesta'
          : editingTemplate
            ? `Editar receita · ${editingTemplate.name}`
            : 'Nova receita de cesta'
      }
      description={
        mode === 'list'
          ? 'Crie, edite, ative/desative ou remova as receitas de cesta básica deste ponto.'
          : 'Monte a receita escolhendo itens do catálogo de estoque e a quantidade de cada um por cesta.'
      }
      size="lg"
    >
      {mode === 'list' ? (
        <div className="space-y-4 py-1">
          {canEdit && (
            <Button
              type="button"
              variant="brand"
              leftIcon={<Plus className="size-4" />}
              onClick={openCreateForm}
            >
              Nova receita
            </Button>
          )}

          {orderedTemplates.length === 0 ? (
            <EmptyState
              icon={<ShoppingBasket className="size-6" />}
              title="Nenhuma receita cadastrada"
              description="Crie a primeira receita de cesta deste ponto."
            />
          ) : (
            <ul className="space-y-2">
              {orderedTemplates.map((template) => (
                <TemplateRow
                  key={template.id}
                  template={template}
                  canEdit={canEdit}
                  isDeleting={isDeleting && deleteTargetId === template.id}
                  deleteErrorMessage={
                    deleteTargetId === template.id ? (removeError?.displayMessage ?? null) : null
                  }
                  onEdit={() => openEditForm(template)}
                  onToggleActive={() => void toggleActive(template)}
                  onDelete={() => void confirmDelete(template)}
                />
              ))}
            </ul>
          )}
        </div>
      ) : (
        <form onSubmit={submitForm} className="space-y-4 py-1">
          {submitError && <Alert tone="error">{submitError.displayMessage}</Alert>}

          <FormField label="Nome da receita" htmlFor="basket-template-name" required>
            <input
              id="basket-template-name"
              type="text"
              value={form.name}
              maxLength={120}
              disabled={isSubmitting}
              onChange={(event) =>
                setForm((previous) => ({ ...previous, name: event.target.value }))
              }
              placeholder="Ex.: Cesta básica padrão"
              className="focus:border-brand-500 focus:ring-brand-500/10 h-11 w-full rounded-xl border border-stone-200 px-3.5 text-sm text-stone-900 transition-all focus:ring-4 focus:outline-none disabled:bg-stone-50"
            />
          </FormField>

          {editingTemplate && (
            <label className="flex items-center gap-2 text-sm font-medium text-stone-700">
              <Checkbox
                checked={form.isActive}
                disabled={isSubmitting}
                onCheckedChange={(checked) =>
                  setForm((previous) => ({ ...previous, isActive: checked === true }))
                }
              />
              Receita ativa
            </label>
          )}

          <FormField
            label="Itens da receita"
            htmlFor="basket-template-item-picker"
            required
            hint="Escolha um item do catálogo de estoque e defina quantos são necessários por cesta."
          >
            <Select
              id="basket-template-item-picker"
              aria-label="Adicionar item à receita"
              value={itemPicker}
              placeholder="Adicionar item…"
              disabled={isSubmitting || pickableItems.length === 0}
              onValueChange={addItem}
            >
              {pickableItems.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.name} ({item.unit})
                </option>
              ))}
            </Select>
          </FormField>

          {form.items.length === 0 ? (
            <Alert tone="info">Nenhum item adicionado ainda.</Alert>
          ) : (
            <ul className="space-y-2">
              {form.items.map((item) => (
                <RecipeItemRow
                  key={item.stockItemId}
                  item={item}
                  disabled={isSubmitting}
                  onChangeQuantity={(quantity) => changeItemQuantity(item.stockItemId, quantity)}
                  onRemove={() => removeItem(item.stockItemId)}
                />
              ))}
            </ul>
          )}

          <Alert tone={previewBaskets > 0 ? 'success' : 'info'} className={cn('font-medium')}>
            {form.items.length === 0
              ? 'Adicione itens para ver a viabilidade desta receita.'
              : `Essa receita permitiria montar ${previewBaskets} ${previewBaskets === 1 ? 'cesta' : 'cestas'} hoje.`}
          </Alert>

          {validationError && <p className="text-xs font-medium text-red-600">{validationError}</p>}

          <div className="flex items-center gap-2">
            <Button
              type="button"
              variant="ghost"
              onClick={() => setMode('list')}
              disabled={isSubmitting}
            >
              Voltar
            </Button>
            <Button
              type="submit"
              variant="brand"
              leftIcon={<Save className="size-4" />}
              loading={isSubmitting}
              disabled={!!validationError || isSubmitting}
              className="flex-1"
            >
              {editingTemplate ? 'Salvar receita' : 'Criar receita'}
            </Button>
          </div>
        </form>
      )}
    </ModalComponent>
  )
}
