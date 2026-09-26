import { useEffect, useRef, useState } from 'react'
import { Check, MapPin } from 'lucide-react'
import { MapContainer, Marker, useMap, useMapEvents } from 'react-leaflet'
import { env } from '@/lib/env'
import { geocodingService, type ReverseAddress } from '@/models/services'
import type { LatLng } from '@/models/types'
import { ModalComponent } from '@/views/components/modal'
import { Button } from '@/views/components/ui'
import { BaseMapTiles } from './BaseMapTiles'
import { createMarkerIcon } from './markerIcon'

export interface PickedLocation {
  position: LatLng
  address: ReverseAddress
}

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  /** Posição inicial do marcador (ex.: ao editar um ponto já localizado). */
  initialPosition?: LatLng | null
  /** Endereço a geocodificar para centralizar o mapa quando não há posição inicial. */
  initialQuery?: string
  onConfirm: (location: PickedLocation) => void
}

const pickerIcon = createMarkerIcon('Association', true)

const emptyAddress: ReverseAddress = {
  street: '',
  number: '',
  district: '',
  city: '',
  state: '',
  zipCode: '',
}

function ClickToPlace({ onPlace }: { onPlace: (position: LatLng) => void }) {
  useMapEvents({ click: (e) => onPlace({ lat: e.latlng.lat, lng: e.latlng.lng }) })
  return null
}

function RecenterOn({ position }: { position: LatLng | null }) {
  const map = useMap()
  useEffect(() => {
    if (position) map.setView([position.lat, position.lng], Math.max(map.getZoom(), 16))
  }, [position, map])
  return null
}

/** Leaflet precisa recalcular o tamanho ao aparecer dentro do diálogo. */
function InvalidateSize() {
  const map = useMap()
  useEffect(() => {
    const id = setTimeout(() => map.invalidateSize(), 150)
    return () => clearTimeout(id)
  }, [map])
  return null
}

function formatPreview(address: ReverseAddress): string {
  const parts = [
    [address.street, address.number].filter(Boolean).join(', '),
    address.district,
    [address.city, address.state].filter(Boolean).join(' - '),
  ]
  return parts.filter(Boolean).join(' · ')
}

interface BodyProps {
  initialPosition: LatLng | null
  initialQuery?: string
  onCancel: () => void
  onConfirm: (location: PickedLocation) => void
}

/** Corpo do seletor; monta a cada abertura (o diálogo desmonta o conteúdo ao fechar). */
function PickerBody({ initialPosition, initialQuery, onCancel, onConfirm }: BodyProps) {
  const [position, setPosition] = useState<LatLng | null>(initialPosition)
  const [recenter, setRecenter] = useState<LatLng | null>(initialPosition)
  const [address, setAddress] = useState<ReverseAddress | null>(null)
  const [resolving, setResolving] = useState(false)
  const initRef = useRef({ initialPosition, initialQuery })

  function choose(next: LatLng) {
    setPosition(next)
    setResolving(true)
    geocodingService.reverse(next).then((resolved) => {
      setAddress(resolved)
      setResolving(false)
    })
  }

  useEffect(() => {
    const start = initRef.current
    let cancelled = false
    void (async () => {
      let target = start.initialPosition
      if (!target && start.initialQuery) {
        const found = await geocodingService.geocode(start.initialQuery)
        if (cancelled || !found) return
        setPosition(found)
        setRecenter(found)
        target = found
      }
      if (target) {
        const resolved = await geocodingService.reverse(target)
        if (!cancelled) setAddress(resolved)
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  function confirm() {
    if (!position) return
    onConfirm({ position, address: address ?? emptyAddress })
  }

  return (
    <div className="space-y-3">
      <div className="h-[55vh] overflow-hidden rounded-2xl ring-1 ring-stone-200">
        <MapContainer
          center={position ? [position.lat, position.lng] : env.map.defaultCenter}
          zoom={position ? 16 : env.map.defaultZoom}
          scrollWheelZoom
          className="size-full"
        >
          <BaseMapTiles />
          <ClickToPlace onPlace={choose} />
          <RecenterOn position={recenter} />
          <InvalidateSize />
          {position && (
            <Marker
              position={[position.lat, position.lng]}
              icon={pickerIcon}
              draggable
              eventHandlers={{
                dragend: (e) => {
                  const { lat, lng } = e.target.getLatLng()
                  choose({ lat, lng })
                },
              }}
            />
          )}
        </MapContainer>
      </div>

      <div className="flex items-start gap-2 rounded-xl bg-stone-50 px-3 py-2 text-sm text-stone-600 ring-1 ring-stone-200 ring-inset">
        <MapPin className="mt-0.5 size-4 shrink-0 text-stone-400" />
        <span className="min-w-0">
          {position
            ? resolving
              ? 'Localizando endereço…'
              : address && formatPreview(address)
                ? formatPreview(address)
                : `Local selecionado (${position.lat.toFixed(5)}, ${position.lng.toFixed(5)})`
            : 'Nenhum local selecionado ainda. Clique no mapa para marcar.'}
        </span>
      </div>

      <div className="flex justify-end gap-2">
        <Button variant="ghost" onClick={onCancel}>
          Cancelar
        </Button>
        <Button
          variant="brand"
          leftIcon={<Check className="size-4" />}
          disabled={!position}
          onClick={confirm}
        >
          Usar este local
        </Button>
      </div>
    </div>
  )
}

/**
 * Seletor de local no mapa. O usuário clica/arrasta o marcador no ponto exato; ao
 * confirmar, devolve as coordenadas e o endereço resolvido por reverse geocoding.
 */
export function LocationPickerModal({
  open,
  onOpenChange,
  initialPosition,
  initialQuery,
  onConfirm,
}: Props) {
  return (
    <ModalComponent
      open={open}
      onOpenChange={onOpenChange}
      title="Selecionar local no mapa"
      description="Clique no mapa ou arraste o marcador até o endereço exato do ponto de coleta."
      size="xl"
    >
      <PickerBody
        initialPosition={initialPosition ?? null}
        initialQuery={initialQuery}
        onCancel={() => onOpenChange(false)}
        onConfirm={(location) => {
          onConfirm(location)
          onOpenChange(false)
        }}
      />
    </ModalComponent>
  )
}
