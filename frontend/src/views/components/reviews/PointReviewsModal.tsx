import { useState } from 'react'
import { Clock, Flag, HeartHandshake, MessageSquare, Send, Star } from 'lucide-react'
import type { CollectionPoint } from '@/models/types'
import { useReviewsViewModel } from '@/viewmodels'
import { ModalComponent } from '@/views/components/modal'
import { Alert, Button, CenteredSpinner, EmptyState, Input, Textarea } from '@/views/components/ui'
import { PublicNoticesList } from '@/views/components/notices/PublicNoticesList'
import { StarRating } from './StarRating'

interface Props {
  point: CollectionPoint | null
  onClose: () => void
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('pt-BR', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  })
}

/**
 * Modal público de avaliações de um ponto de coleta: mostra a média e os comentários
 * da comunidade e traz o formulário para quem já doou registrar sua própria avaliação.
 */
export function PointReviewsModal({ point, onClose }: Props) {
  return (
    <ModalComponent
      open={!!point}
      onOpenChange={(open) => !open && onClose()}
      title={point ? `Avaliações · ${point.name}` : undefined}
      description="Veja o que a comunidade achou e deixe a sua avaliação de quem já doou aqui."
      size="lg"
    >
      {point && <PointReviewsBody key={point.id} point={point} />}
    </ModalComponent>
  )
}

/**
 * Corpo do modal — remontado (via `key={point.id}`) a cada ponto selecionado, o que
 * reinicia naturalmente o formulário sem precisar de efeito.
 */
function PointReviewsBody({ point }: { point: CollectionPoint }) {
  const vm = useReviewsViewModel(point.id)

  const [authorName, setAuthorName] = useState('')
  const [rating, setRating] = useState(0)
  const [comment, setComment] = useState('')
  const [formError, setFormError] = useState<string | null>(null)
  const [reported, setReported] = useState<Set<string>>(new Set())

  async function reportReview(id: string) {
    try {
      await vm.report(id)
    } catch {
      // Denúncia é best-effort; marcamos como denunciada de qualquer forma.
    }
    setReported((prev) => new Set(prev).add(id))
  }

  async function handleSubmit() {
    if (!authorName.trim()) {
      setFormError('Informe seu nome para avaliar.')
      return
    }
    if (rating < 1) {
      setFormError('Selecione uma nota de 1 a 5 estrelas.')
      return
    }
    setFormError(null)
    await vm.submit({ authorName: authorName.trim(), rating, comment: comment.trim() || null })
    setAuthorName('')
    setRating(0)
    setComment('')
  }

  return (
    <div className="space-y-6 py-1">
      {point.description && (
        <p className="text-sm leading-relaxed text-stone-600">{point.description}</p>
      )}

      <PublicNoticesList pointId={point.id} />

      {point.openingHours && (
        <p className="flex items-center gap-2 text-sm text-stone-500">
          <Clock className="size-4 shrink-0 text-stone-500" />
          {point.openingHours}
        </p>
      )}

      {point.needs.length > 0 && (
        <div className="rounded-2xl bg-amber-50 p-3 ring-1 ring-amber-200 ring-inset">
          <p className="text-[11px] font-bold tracking-wide text-amber-700 uppercase">
            Precisa agora
          </p>
          <div className="mt-1.5 flex flex-wrap gap-1.5">
            {point.needs.map((need) => (
              <span
                key={need}
                className="rounded-full bg-white px-2.5 py-1 text-xs font-medium text-amber-700 ring-1 ring-amber-200 ring-inset"
              >
                {need}
              </span>
            ))}
          </div>
        </div>
      )}

      <div className="flex items-center gap-4 rounded-2xl bg-stone-50 p-4 ring-1 ring-stone-200 ring-inset">
        <div className="text-center">
          <p className="text-3xl font-bold text-stone-900 tabular-nums">
            {point.averageRating.toFixed(1)}
          </p>
          <StarRating value={point.averageRating} size="sm" className="mt-1" />
        </div>
        <div className="space-y-1 text-sm text-stone-500">
          <p>
            {point.reviewCount === 0
              ? 'Ainda sem avaliações. Seja o primeiro a avaliar!'
              : `${point.reviewCount} ${point.reviewCount === 1 ? 'avaliação' : 'avaliações'} da comunidade`}
          </p>
          {point.beneficiariesServed > 0 && (
            <p className="inline-flex items-center gap-1.5 font-medium text-emerald-600">
              <HeartHandshake className="size-4" />
              {point.beneficiariesServed}{' '}
              {point.beneficiariesServed === 1 ? 'pessoa atendida' : 'pessoas atendidas'}
            </p>
          )}
        </div>
      </div>

      <div className="space-y-3 rounded-2xl border border-stone-200 p-4">
        <h3 className="flex items-center gap-2 text-sm font-semibold text-stone-800">
          <Star className="size-4 text-amber-500" />
          Deixe sua avaliação
        </h3>

        {vm.submitted && (
          <Alert tone="success">Avaliação registrada. Obrigado por contribuir!</Alert>
        )}
        {vm.submitError && <Alert tone="error">{vm.submitError.displayMessage}</Alert>}
        {formError && <Alert tone="error">{formError}</Alert>}

        <Input
          value={authorName}
          onChange={(e) => setAuthorName(e.target.value)}
          placeholder="Seu nome"
          maxLength={120}
        />
        <div className="flex items-center gap-3">
          <span className="text-sm text-stone-500">Sua nota:</span>
          <StarRating value={rating} onChange={setRating} size="md" />
        </div>
        <Textarea
          value={comment}
          onChange={(e) => setComment(e.target.value)}
          placeholder="Conte como foi sua experiência (opcional)"
          maxLength={1000}
        />
        <Button
          variant="brand"
          leftIcon={<Send className="size-4" />}
          onClick={handleSubmit}
          disabled={vm.isSubmitting}
        >
          {vm.isSubmitting ? 'Enviando…' : 'Enviar avaliação'}
        </Button>
      </div>

      <div className="space-y-3">
        <h3 className="flex items-center gap-2 text-sm font-semibold text-stone-800">
          <MessageSquare className="size-4 text-stone-500" />
          Comentários
        </h3>

        {vm.isLoading ? (
          <CenteredSpinner label="Carregando avaliações…" />
        ) : vm.isError ? (
          <Alert tone="error">{vm.error?.displayMessage}</Alert>
        ) : vm.reviews.length === 0 ? (
          <EmptyState
            icon={<MessageSquare className="size-6" />}
            title="Nenhum comentário ainda"
            description="As avaliações registradas aparecerão aqui."
          />
        ) : (
          <ul className="space-y-3">
            {vm.reviews.map((review) => (
              <li key={review.id} className="rounded-2xl border border-stone-200 p-3.5">
                <div className="flex items-center justify-between gap-3">
                  <p className="font-semibold text-stone-900">{review.authorName}</p>
                  <span className="shrink-0 text-xs text-stone-500">
                    {formatDate(review.createdAtUtc)}
                  </span>
                </div>
                <StarRating value={review.rating} size="sm" className="mt-1" />
                {review.comment && <p className="mt-2 text-sm text-stone-600">{review.comment}</p>}
                <div className="mt-2 flex justify-end">
                  {reported.has(review.id) ? (
                    <span className="text-[11px] text-stone-500">Denúncia enviada</span>
                  ) : (
                    <button
                      type="button"
                      onClick={() => reportReview(review.id)}
                      className="inline-flex items-center gap-1 text-[11px] font-medium text-stone-500 transition-colors hover:text-red-600"
                    >
                      <Flag className="size-3" />
                      Denunciar
                    </button>
                  )}
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  )
}
