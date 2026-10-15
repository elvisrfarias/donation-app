import { useState } from 'react'
import { PackageCheck } from 'lucide-react'
import type { Donation, ReceivedStockEntry } from '@/models/types'
import { useCollectionPoint, useReceiveDonation } from '@/viewmodels'
import { ModalComponent } from '@/views/components/modal'
import { Alert, Button, CenteredSpinner, Input } from '@/views/components/ui'

interface Props {
  pointId: string
  donation: Donation | null
  onClose: () => void
}

/**
 * Confirmação de recebimento: o gestor pode já somar ao estoque do ponto os itens que
 * chegaram, ou só marcar a doação como recebida e ajustar o estoque depois.
 */
export function ReceiveDonationModal({ pointId, donation, onClose }: Props) {
  return (
    <ModalComponent
      open={!!donation}
      onOpenChange={(open) => !open && onClose()}
      title="Confirmar recebimento"
      description={donation ? `${donation.donorName}: ${donation.description}` : undefined}
      size="md"
    >
      {donation && (
        <ReceiveDonationBody
          key={donation.id}
          pointId={pointId}
          donation={donation}
          onDone={onClose}
        />
      )}
    </ModalComponent>
  )
}

function parseReceivedQuantity(raw: string | undefined): number {
  const quantity = Number.parseInt(raw ?? '', 10)
  return Number.isInteger(quantity) && quantity > 0 ? quantity : 0
}

function toReceivedEntries(quantities: Record<string, string>): ReceivedStockEntry[] {
  return Object.entries(quantities)
    .map(([stockItemId, raw]) => ({ stockItemId, quantity: parseReceivedQuantity(raw) }))
    .filter((entry) => entry.quantity > 0)
}

function ReceiveDonationBody({
  pointId,
  donation,
  onDone,
}: {
  pointId: string
  donation: Donation
  onDone: () => void
}) {
  const { point, isLoading } = useCollectionPoint(pointId)
  const receive = useReceiveDonation(pointId)
  const [quantities, setQuantities] = useState<Record<string, string>>({})

  function handleConfirm() {
    receive.mutate(
      { donationId: donation.id, input: { receivedStock: toReceivedEntries(quantities) } },
      { onSuccess: onDone },
    )
  }

  if (isLoading) return <CenteredSpinner label="Carregando estoque…" />

  const stockItems = point?.stockItems ?? []

  return (
    <div className="space-y-4 py-1">
      {receive.error && <Alert tone="error">{receive.error.displayMessage}</Alert>}

      {stockItems.length > 0 ? (
        <fieldset className="space-y-2">
          <legend className="text-sm font-semibold text-stone-700">
            Somar ao estoque (opcional)
          </legend>
          <p className="text-xs text-stone-500">
            Informe só o que chegou. Os itens em branco não mudam.
          </p>
          <ul className="divide-y divide-stone-100 rounded-2xl ring-1 ring-stone-200 ring-inset">
            {stockItems.map((item) => {
              const added = parseReceivedQuantity(quantities[item.id])
              return (
                <li key={item.id} className="flex items-center gap-3 px-3 py-2.5">
                  <div className="min-w-0 flex-1">
                    <label
                      htmlFor={`received-${item.id}`}
                      className="block truncate text-sm font-medium text-stone-800"
                    >
                      {item.name}
                    </label>
                    <p className="text-xs text-stone-500 tabular-nums">
                      Tem {item.quantity} {item.unit}
                      {added > 0 && (
                        <span className="font-semibold text-emerald-700">
                          {' '}
                          → passa a {item.quantity + added}
                        </span>
                      )}
                    </p>
                  </div>
                  <Input
                    id={`received-${item.id}`}
                    type="number"
                    min={0}
                    inputMode="numeric"
                    className="w-24 text-right"
                    value={quantities[item.id] ?? ''}
                    placeholder="0"
                    onChange={(event) =>
                      setQuantities((current) => ({ ...current, [item.id]: event.target.value }))
                    }
                  />
                </li>
              )
            })}
          </ul>
        </fieldset>
      ) : (
        <p className="text-sm text-stone-500">
          O ponto ainda não tem itens de estoque cadastrados. A doação será só marcada como
          recebida.
        </p>
      )}

      <Button
        variant="brand"
        leftIcon={<PackageCheck className="size-4" />}
        loading={receive.isPending}
        onClick={handleConfirm}
      >
        Confirmar recebimento
      </Button>
    </div>
  )
}
