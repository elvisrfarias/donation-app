import { useState } from 'react'
import { CheckCircle2, Clock, HandHeart, MapPin, Plus } from 'lucide-react'
import { appendSuggestion } from '@/lib/donationText'
import { isValidPhone, maskPhone } from '@/lib/phoneInput'
import { whatsappUrl } from '@/lib/whatsapp'
import {
  MAX_DONATION_DESCRIPTION_LENGTH,
  MAX_DONOR_NAME_LENGTH,
  type CollectionPoint,
} from '@/models/types'
import { usePledgeDonation } from '@/viewmodels'
import { ModalComponent } from '@/views/components/modal'
import { Alert, Button, FormField, Input, Textarea } from '@/views/components/ui'
import { WhatsAppIcon } from '@/views/components/ui/WhatsAppIcon'

interface Props {
  point: CollectionPoint | null
  onClose: () => void
}

interface FieldErrors {
  donorName?: string
  donorPhone?: string
  description?: string
}

const NEXT_STEPS = [
  'Separe o que você vai doar.',
  'Leve ao endereço do ponto, no horário de funcionamento.',
  'O responsável confirma o recebimento.',
]

function validate(donorName: string, donorPhone: string, description: string): FieldErrors {
  const errors: FieldErrors = {}
  if (!donorName.trim()) errors.donorName = 'Informe seu nome.'
  if (!description.trim()) errors.description = 'Descreva o que você vai doar.'
  if (donorPhone.trim() && !isValidPhone(donorPhone)) {
    errors.donorPhone = 'Telefone inválido. Informe DDD e número, ou deixe em branco.'
  }
  return errors
}

function suggestionsFor(point: CollectionPoint): { needs: string[]; accepted: string[] } {
  const needs = point.needs
  const accepted = point.categories
    .map((category) => category.name)
    .filter((name) => !needs.some((need) => need.toLowerCase() === name.toLowerCase()))
  return { needs, accepted }
}

/**
 * Modal público "Quero doar": a pessoa avisa o ponto do que pretende entregar, e o gestor
 * confirma o recebimento depois. Dá à comunidade um caminho direto para doar, não só avaliar.
 */
export function PledgeDonationModal({ point, onClose }: Props) {
  return (
    <ModalComponent
      open={!!point}
      onOpenChange={(open) => !open && onClose()}
      title={point ? `Quero doar` : undefined}
      description="Avise o ponto do que você vai levar. O responsável confirma quando receber."
      size="md"
    >
      {point && <PledgeDonationBody key={point.id} point={point} onClose={onClose} />}
    </ModalComponent>
  )
}

function PointSummary({ point }: { point: CollectionPoint }) {
  return (
    <div className="flex items-center gap-3 rounded-2xl bg-stone-50 p-3 ring-1 ring-stone-200 ring-inset">
      {point.imageUrl ? (
        <img src={point.imageUrl} alt="" className="size-14 shrink-0 rounded-xl object-cover" />
      ) : (
        <span className="bg-brand-50 text-brand-600 grid size-14 shrink-0 place-items-center rounded-xl">
          <HandHeart className="size-6" aria-hidden />
        </span>
      )}
      <div className="min-w-0 space-y-0.5">
        <p className="truncate font-semibold text-stone-900">{point.name}</p>
        <p className="flex items-center gap-1.5 text-xs text-stone-600">
          <MapPin className="size-3.5 shrink-0" aria-hidden />
          <span className="truncate">
            {point.address.street}, {point.address.number} · {point.address.city}/
            {point.address.state}
          </span>
        </p>
        {point.openingHours && (
          <p className="flex items-center gap-1.5 text-xs text-stone-600">
            <Clock className="size-3.5 shrink-0" aria-hidden />
            <span className="truncate">{point.openingHours}</span>
          </p>
        )}
      </div>
    </div>
  )
}

function SuggestionChips({
  label,
  tone,
  items,
  onPick,
}: {
  label: string
  tone: 'amber' | 'brand'
  items: string[]
  onPick: (item: string) => void
}) {
  if (items.length === 0) return null

  const chipTone =
    tone === 'amber'
      ? 'bg-amber-50 text-amber-800 ring-amber-200 hover:bg-amber-100'
      : 'bg-brand-50 text-brand-700 ring-brand-100 hover:bg-brand-100'

  return (
    <div className="space-y-1.5">
      <p className="text-xs font-semibold text-stone-600">{label}</p>
      <div className="flex flex-wrap gap-1.5">
        {items.map((item) => (
          <button
            key={item}
            type="button"
            onClick={() => onPick(item)}
            aria-label={`Adicionar ${item} à descrição`}
            className={`inline-flex min-h-9 items-center gap-1 rounded-full px-3 text-xs font-semibold ring-1 transition-colors ring-inset ${chipTone}`}
          >
            <Plus className="size-3" aria-hidden />
            {item}
          </button>
        ))}
      </div>
    </div>
  )
}

function PledgeSuccess({
  point,
  description,
  onClose,
}: {
  point: CollectionPoint
  description: string
  onClose: () => void
}) {
  return (
    <div className="space-y-5 py-1" role="status">
      <div className="flex flex-col items-center gap-2 text-center">
        <span className="grid size-14 place-items-center rounded-full bg-emerald-50 text-emerald-600">
          <CheckCircle2 className="size-8" aria-hidden />
        </span>
        <h3 className="text-lg font-bold text-stone-900">Obrigado por ajudar!</h3>
        <p className="text-sm text-stone-600">
          {point.name} foi avisado sobre: <strong>{description}</strong>
        </p>
      </div>

      <PointSummary point={point} />

      <ol className="space-y-2 text-sm text-stone-700">
        {NEXT_STEPS.map((step, index) => (
          <li key={step} className="flex items-start gap-3">
            <span className="bg-brand-50 text-brand-700 grid size-6 shrink-0 place-items-center rounded-full text-xs font-bold">
              {index + 1}
            </span>
            <span className="pt-0.5">{step}</span>
          </li>
        ))}
      </ol>

      <div className="flex flex-col gap-2 sm:flex-row">
        <a
          href={whatsappUrl(point.contact.phone)}
          target="_blank"
          rel="noopener noreferrer"
          className="inline-flex h-11 flex-1 items-center justify-center gap-2 rounded-full bg-emerald-600 px-5 text-sm font-semibold text-white transition-colors hover:bg-emerald-700"
        >
          <WhatsAppIcon className="size-4" />
          Combinar entrega pelo WhatsApp
        </a>
        <Button variant="secondary" onClick={onClose}>
          Fechar
        </Button>
      </div>
    </div>
  )
}

function PledgeDonationBody({ point, onClose }: { point: CollectionPoint; onClose: () => void }) {
  const vm = usePledgeDonation(point.id)
  const [donorName, setDonorName] = useState('')
  const [donorPhone, setDonorPhone] = useState('')
  const [description, setDescription] = useState('')
  const [errors, setErrors] = useState<FieldErrors>({})
  const suggestions = suggestionsFor(point)

  function handleSubmit() {
    const found = validate(donorName, donorPhone, description)
    setErrors(found)
    if (Object.keys(found).length > 0) return

    vm.pledge({
      donorName: donorName.trim(),
      donorPhone: donorPhone.trim() || null,
      description: description.trim(),
    })
  }

  function pickSuggestion(item: string) {
    setDescription((current) => appendSuggestion(current, item))
    setErrors((current) => ({ ...current, description: undefined }))
  }

  if (vm.pledged) {
    return <PledgeSuccess point={point} description={description.trim()} onClose={onClose} />
  }

  return (
    <form
      noValidate
      className="space-y-4 py-1"
      onSubmit={(event) => {
        event.preventDefault()
        handleSubmit()
      }}
    >
      <PointSummary point={point} />

      {vm.error && <Alert tone="error">{vm.error.displayMessage}</Alert>}

      <FormField label="Seu nome" htmlFor="pledge-name" error={errors.donorName} required>
        <Input
          id="pledge-name"
          value={donorName}
          onChange={(event) => setDonorName(event.target.value)}
          maxLength={MAX_DONOR_NAME_LENGTH}
          autoComplete="name"
          invalid={!!errors.donorName}
          aria-invalid={!!errors.donorName}
        />
      </FormField>

      <FormField
        label="O que você vai doar"
        htmlFor="pledge-description"
        error={errors.description}
        required
      >
        <SuggestionChips
          label="Esse ponto precisa agora"
          tone="amber"
          items={suggestions.needs}
          onPick={pickSuggestion}
        />
        <SuggestionChips
          label="Esse ponto aceita"
          tone="brand"
          items={suggestions.accepted}
          onPick={pickSuggestion}
        />
        <Textarea
          id="pledge-description"
          value={description}
          onChange={(event) => setDescription(event.target.value)}
          maxLength={MAX_DONATION_DESCRIPTION_LENGTH}
          placeholder="Ex.: 2 cestas básicas e roupas de inverno infantis"
          invalid={!!errors.description}
          aria-invalid={!!errors.description}
        />
        <p className="text-right text-xs text-stone-500 tabular-nums" aria-hidden>
          {description.length}/{MAX_DONATION_DESCRIPTION_LENGTH}
        </p>
      </FormField>

      <FormField
        label="Telefone"
        htmlFor="pledge-phone"
        error={errors.donorPhone}
        hint="Opcional. Só o responsável pelo ponto verá, para combinar a entrega."
      >
        <Input
          id="pledge-phone"
          value={donorPhone}
          onChange={(event) => setDonorPhone(maskPhone(event.target.value))}
          inputMode="tel"
          autoComplete="tel"
          placeholder="(11) 98765-4321"
          invalid={!!errors.donorPhone}
          aria-invalid={!!errors.donorPhone}
        />
      </FormField>

      <Button type="submit" variant="brand" size="lg" className="w-full" loading={vm.isPledging}>
        <HandHeart className="size-4" aria-hidden />
        Avisar o ponto
      </Button>
    </form>
  )
}
