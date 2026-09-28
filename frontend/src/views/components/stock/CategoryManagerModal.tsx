import { useEffect, useState, type FormEvent } from 'react'
import { Check, Pencil, Plus, Save, Tags, Trash2 } from 'lucide-react'
import type { Category, CollectionPoint } from '@/models/types'
import { cn } from '@/lib/utils'
import { useCreateCategory, useRemoveCategory, useUpdateCategory } from '@/viewmodels'
import { Alert, Button, EmptyState, FormField } from '@/views/components/ui'
import { ModalComponent } from '@/views/components/modal'
import { FALLBACK_ICON, ICON_BY_KEY, ICON_PALETTE } from './categoryIcons'

interface CategoryManagerModalProps {
  point: CollectionPoint
  canEdit: boolean
  open: boolean
  /** Abre a modal já no formulário de criação (ex.: vindo do estado vazio do catálogo de estoque). */
  startInCreation: boolean
  onClose: () => void
}

interface CategoryFormState {
  name: string
  icon: string
}

function emptyForm(): CategoryFormState {
  return { name: '', icon: ICON_PALETTE[0].key }
}

function formFromCategory(category: Category): CategoryFormState {
  return { name: category.name, icon: category.icon }
}

function validate(form: CategoryFormState): string | null {
  if (form.name.trim().length === 0) return 'Informe o nome da categoria.'
  if (form.name.trim().length > 120) return 'O nome pode ter no máximo 120 caracteres.'
  return null
}

function CategoryRow({
  category,
  canEdit,
  isDeleting,
  deleteErrorMessage,
  onEdit,
  onDelete,
}: {
  category: Category
  canEdit: boolean
  isDeleting: boolean
  deleteErrorMessage: string | null
  onEdit: () => void
  onDelete: () => void
}) {
  const Icon = ICON_BY_KEY[category.icon] ?? FALLBACK_ICON

  return (
    <li className="rounded-2xl border border-stone-200 bg-white p-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex min-w-0 items-center gap-3">
          <span className="grid size-9 shrink-0 place-items-center rounded-xl bg-stone-100 text-stone-600">
            <Icon className="size-4.5" />
          </span>
          <p className="truncate text-sm font-bold text-stone-900">{category.name}</p>
        </div>
        {canEdit && (
          <div className="flex shrink-0 items-center gap-1">
            <Button variant="ghost" size="sm" leftIcon={<Pencil className="size-4" />} onClick={onEdit}>
              Editar
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

function IconPicker({
  value,
  disabled,
  onChange,
}: {
  value: string
  disabled: boolean
  onChange: (key: string) => void
}) {
  return (
    <div role="radiogroup" aria-label="Ícone da categoria" className="flex flex-wrap gap-2">
      {ICON_PALETTE.map(({ key, label, Icon }) => {
        const isSelected = key === value
        return (
          <button
            key={key}
            type="button"
            role="radio"
            aria-checked={isSelected}
            aria-label={label}
            title={label}
            disabled={disabled}
            onClick={() => onChange(key)}
            className={cn(
              'relative grid size-11 shrink-0 place-items-center rounded-xl ring-1 ring-inset transition-colors disabled:opacity-40',
              isSelected
                ? 'bg-brand-600 text-white ring-brand-600'
                : 'bg-white text-stone-600 ring-stone-200 hover:bg-brand-50 hover:text-brand-700 hover:ring-brand-200',
            )}
          >
            <Icon className="size-5" />
            {isSelected && (
              <Check className="absolute -top-1 -right-1 size-3.5 rounded-full bg-white text-brand-600" />
            )}
          </button>
        )
      })}
    </div>
  )
}

export function CategoryManagerModal({
  point,
  canEdit,
  open,
  startInCreation,
  onClose,
}: CategoryManagerModalProps) {
  const [mode, setMode] = useState<'list' | 'form'>('list')
  const [editingCategory, setEditingCategory] = useState<Category | null>(null)
  const [form, setForm] = useState<CategoryFormState>(emptyForm())
  const [deleteTargetId, setDeleteTargetId] = useState<string | null>(null)

  const { createCategory, isPending: isCreating, error: createError, reset: resetCreate } =
    useCreateCategory()
  const { updateCategory, isPending: isUpdating, error: updateError, reset: resetUpdate } =
    useUpdateCategory()
  const { removeCategory, isPending: isDeleting, error: removeError, reset: resetRemove } =
    useRemoveCategory()

  useEffect(() => {
    if (!open) return
    resetCreate()
    resetUpdate()
    resetRemove()
    // Reinicia o rascunho sempre que a modal reabre (componente persistente, não um remount) —
    // reset intencional, não sincronização com uma fonte externa.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setDeleteTargetId(null)
    if (startInCreation) {
      setEditingCategory(null)
      setForm(emptyForm())
      setMode('form')
    } else {
      setMode('list')
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open])

  const categories = [...point.categories].sort((a, b) => a.name.localeCompare(b.name, 'pt-BR'))

  const isSubmitting = isCreating || isUpdating
  const submitError = createError ?? updateError
  const validationError = validate(form)

  function openCreateForm() {
    setEditingCategory(null)
    setForm(emptyForm())
    setMode('form')
  }

  function openEditForm(category: Category) {
    setEditingCategory(category)
    setForm(formFromCategory(category))
    setMode('form')
  }

  async function submitForm(event: FormEvent) {
    event.preventDefault()
    if (validationError || isSubmitting) return

    const input = { name: form.name.trim(), icon: form.icon }

    if (editingCategory) {
      await updateCategory({ pointId: point.id, categoryId: editingCategory.id, input })
    } else {
      await createCategory({ pointId: point.id, input })
    }
    setMode('list')
  }

  async function confirmDelete(category: Category) {
    if (!window.confirm(`Remover a categoria "${category.name}"?`)) return
    resetRemove()
    setDeleteTargetId(category.id)
    try {
      await removeCategory({ pointId: point.id, categoryId: category.id })
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
          ? 'Gerenciar categorias'
          : editingCategory
            ? `Editar categoria · ${editingCategory.name}`
            : 'Nova categoria'
      }
      description={
        mode === 'list'
          ? 'Crie, edite ou remova as categorias do catálogo deste ponto.'
          : 'Escolha um nome e um ícone para a categoria.'
      }
      size="md"
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
              Nova categoria
            </Button>
          )}

          {categories.length === 0 ? (
            <EmptyState
              icon={<Tags className="size-6" />}
              title="Nenhuma categoria cadastrada"
              description="Crie a primeira categoria deste ponto."
            />
          ) : (
            <ul className="space-y-2">
              {categories.map((category) => (
                <CategoryRow
                  key={category.id}
                  category={category}
                  canEdit={canEdit}
                  isDeleting={isDeleting && deleteTargetId === category.id}
                  deleteErrorMessage={
                    deleteTargetId === category.id ? (removeError?.displayMessage ?? null) : null
                  }
                  onEdit={() => openEditForm(category)}
                  onDelete={() => void confirmDelete(category)}
                />
              ))}
            </ul>
          )}
        </div>
      ) : (
        <form onSubmit={submitForm} className="space-y-4 py-1">
          {submitError && <Alert tone="error">{submitError.displayMessage}</Alert>}

          <FormField label="Nome da categoria" htmlFor="category-name" required>
            <input
              id="category-name"
              type="text"
              value={form.name}
              maxLength={120}
              disabled={isSubmitting}
              onChange={(event) => setForm((previous) => ({ ...previous, name: event.target.value }))}
              placeholder="Ex.: Alimentos"
              className="focus:border-brand-500 focus:ring-brand-500/10 h-11 w-full rounded-xl border border-stone-200 px-3.5 text-sm text-stone-900 transition-all focus:ring-4 focus:outline-none disabled:bg-stone-50"
            />
          </FormField>

          <FormField label="Ícone" htmlFor="category-icon-picker">
            <IconPicker
              value={form.icon}
              disabled={isSubmitting}
              onChange={(icon) => setForm((previous) => ({ ...previous, icon }))}
            />
          </FormField>

          {validationError && <p className="text-xs font-medium text-red-600">{validationError}</p>}

          <div className="flex items-center gap-2">
            <Button type="button" variant="ghost" onClick={() => setMode('list')} disabled={isSubmitting}>
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
              {editingCategory ? 'Salvar categoria' : 'Criar categoria'}
            </Button>
          </div>
        </form>
      )}
    </ModalComponent>
  )
}
