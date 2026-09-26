import { useState } from 'react'
import { Boxes, Store } from 'lucide-react'
import { useAuth, useCollectionPointsForManagement } from '@/viewmodels'
import { StockWorkspace } from '@/views/components/stock/StockWorkspace'
import { Alert, Card, EmptyState, RequiredMark, SectionTitle, Select } from '@/views/components/ui'

function AdminPointPicker({
  selectedId,
  onSelect,
}: {
  selectedId: string
  onSelect: (id: string) => void
}) {
  const { points, isLoading, isError, error } = useCollectionPointsForManagement()

  return (
    <Card className="space-y-2 p-5">
      <label htmlFor="stock-point" className="text-sm font-semibold text-stone-700">
        Ponto de coleta
        <RequiredMark />
      </label>
      {isError && <Alert tone="error">{error?.displayMessage}</Alert>}
      <Select
        id="stock-point"
        value={selectedId}
        onValueChange={onSelect}
        disabled={isLoading || points.length === 0}
      >
        <option value="">{isLoading ? 'Carregando pontos…' : 'Selecione um ponto…'}</option>
        {points.map((point) => (
          <option key={point.id} value={point.id}>
            {point.name} — {point.address.city}/{point.address.state}
          </option>
        ))}
      </Select>
    </Card>
  )
}

function AdminStock() {
  const [selectedId, setSelectedId] = useState('')

  return (
    <>
      <AdminPointPicker selectedId={selectedId} onSelect={setSelectedId} />
      {selectedId ? (
        <StockWorkspace pointId={selectedId} canEdit />
      ) : (
        <EmptyState
          icon={<Store className="size-6" />}
          title="Selecione um ponto"
          description="Escolha o ponto de coleta para ver e ajustar o estoque."
        />
      )}
    </>
  )
}

function PointManagerStock({ pointId }: { pointId: string | null }) {
  if (!pointId) {
    return (
      <Alert tone="warning" title="Seu usuário não está vinculado a um ponto">
        Peça a um administrador para vincular você a um ponto de coleta.
      </Alert>
    )
  }

  return <StockWorkspace pointId={pointId} canEdit />
}

export function StockPage() {
  const { user, isAdmin, isManager } = useAuth()

  return (
    <div className="mx-auto w-full max-w-4xl space-y-6">
      <SectionTitle
        icon={<Boxes className="size-5" />}
        title="Estoque"
        description="Informe o que o ponto tem disponível agora. Aparece como “Disponível agora” no mapa público."
      />

      {isAdmin && <AdminStock />}
      {!isAdmin && isManager && <PointManagerStock pointId={user?.collectionPointId ?? null} />}
      {!isManager && (
        <Alert tone="info">Apenas administradores e gestores de ponto gerenciam o estoque.</Alert>
      )}
    </div>
  )
}
