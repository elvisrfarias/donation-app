import { useEffect } from 'react'
import { MapContainer, Marker, Popup, TileLayer, useMap } from 'react-leaflet'
import { env } from '@/lib/env'
import {
  collectionPointTypeLabels,
  formatAddress,
  type CollectionPoint,
  type Geocoded,
} from '@/models/types'
import { Badge } from '@/views/components/ui'
import { createMarkerIcon } from './markerIcon'

interface Props {
  points: Geocoded<CollectionPoint>[]
  selectedId: string | null
  onSelect: (id: string | null) => void
}

/** Anima a câmera até o ponto selecionado na lista. */
function FlyToSelected({ points, selectedId }: Pick<Props, 'points' | 'selectedId'>) {
  const map = useMap()
  useEffect(() => {
    if (!selectedId) return
    const target = points.find((p) => p.item.id === selectedId)
    if (target?.position) {
      map.flyTo([target.position.lat, target.position.lng], Math.max(map.getZoom(), 14), {
        duration: 0.8,
      })
    }
  }, [selectedId, points, map])
  return null
}

export function CollectionPointsMap({ points, selectedId, onSelect }: Props) {
  const located = points.filter((p) => p.position)

  return (
    <MapContainer
      center={env.map.defaultCenter}
      zoom={env.map.defaultZoom}
      scrollWheelZoom
      className="size-full"
    >
      <TileLayer
        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        url="https://tile.openstreetmap.org/{z}/{x}/{y}.png"
        maxZoom={19}
      />

      {located.map(({ item, position }) => (
        <Marker
          key={item.id}
          position={[position!.lat, position!.lng]}
          icon={createMarkerIcon(item.type, item.id === selectedId)}
          eventHandlers={{ click: () => onSelect(item.id) }}
          zIndexOffset={item.id === selectedId ? 1000 : 0}
        >
          <Popup>
            <div className="min-w-56 space-y-1.5 p-3">
              <div className="flex items-center justify-between gap-2">
                <h3 className="font-semibold text-slate-900">{item.name}</h3>
                <Badge tone="brand">{collectionPointTypeLabels[item.type]}</Badge>
              </div>
              <p className="text-sm text-slate-600">{formatAddress(item.address)}</p>
              <div className="border-t border-slate-100 pt-1.5 text-xs text-slate-500">
                <p className="font-medium text-slate-600">{item.contact.responsibleName}</p>
                <p>
                  {item.contact.phone} · {item.contact.email}
                </p>
              </div>
            </div>
          </Popup>
        </Marker>
      ))}

      <FlyToSelected points={points} selectedId={selectedId} />
    </MapContainer>
  )
}
