import { useEffect, useRef, type RefObject } from 'react'
import L from 'leaflet'
import { MapContainer, Marker, Popup, TileLayer, useMap, ZoomControl } from 'react-leaflet'
import { env } from '@/lib/env'
import {
  collectionPointTypeLabels,
  formatAddress,
  type CollectionPoint,
  type Geocoded,
  type LatLng,
} from '@/models/types'
import { Badge } from '@/views/components/ui'
import { createMarkerIcon } from './markerIcon'

interface Props {
  points: Geocoded<CollectionPoint>[]
  selectedId: string | null
  onSelect: (id: string | null) => void
}

type LocatedPoint = Geocoded<CollectionPoint> & { position: LatLng }

type MarkerRegistry = RefObject<Record<string, L.Marker>>

function hasPosition(point: Geocoded<CollectionPoint>): point is LocatedPoint {
  return point.position !== null
}

/** Anima a câmera até o ponto selecionado na lista e abre o seu tooltip. */
function FlyToSelected({
  points,
  selectedId,
  markerRefs,
}: Pick<Props, 'points' | 'selectedId'> & { markerRefs: MarkerRegistry }) {
  const map = useMap()
  useEffect(() => {
    if (!selectedId) return
    const target = points.find((p) => p.item.id === selectedId)
    if (target?.position) {
      map.flyTo([target.position.lat, target.position.lng], Math.max(map.getZoom(), 15), {
        duration: 0.8,
      })
      markerRefs.current[selectedId]?.openPopup()
    }
  }, [selectedId, points, map, markerRefs])
  return null
}

/** Enquadra todos os pontos localizados na primeira vez que aparecem. */
function FitToPoints({ points }: Pick<Props, 'points'>) {
  const map = useMap()
  const fitted = useRef(false)
  useEffect(() => {
    if (fitted.current) return
    const located = points.filter((p) => p.position)
    if (located.length === 0) return
    fitted.current = true
    if (located.length === 1) {
      const { lat, lng } = located[0].position!
      map.setView([lat, lng], 14)
      return
    }
    const bounds = L.latLngBounds(located.map((p) => [p.position!.lat, p.position!.lng]))
    map.fitBounds(bounds, { padding: [64, 64], maxZoom: 15 })
  }, [points, map])
  return null
}

export function CollectionPointsMap({ points, selectedId, onSelect }: Props) {
  const located = points.filter(hasPosition)
  const markerRefs = useRef<Record<string, L.Marker>>({})

  return (
    <MapContainer
      center={env.map.defaultCenter}
      zoom={env.map.defaultZoom}
      scrollWheelZoom
      zoomControl={false}
      className="size-full bg-stone-100"
    >
      <TileLayer
        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> &copy; <a href="https://carto.com/attributions">CARTO</a>'
        url="https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png"
        subdomains="abcd"
        maxZoom={20}
      />
      <ZoomControl position="bottomright" />

      {located.map(({ item, position }) => (
        <Marker
          key={item.id}
          position={[position!.lat, position!.lng]}
          icon={createMarkerIcon(item.type, item.id === selectedId)}
          eventHandlers={{ click: () => onSelect(item.id) }}
          zIndexOffset={item.id === selectedId ? 1000 : 0}
          ref={(instance) => {
            if (instance) markerRefs.current[item.id] = instance
            else delete markerRefs.current[item.id]
          }}
        >
          <Popup>
            <div className="min-w-60 space-y-2 p-3.5">
              {item.imageUrl && (
                <img
                  src={item.imageUrl}
                  alt={item.name}
                  loading="lazy"
                  className="h-28 w-full rounded-lg object-cover"
                />
              )}
              <div className="flex items-start justify-between gap-2">
                <h3 className="text-sm font-bold text-stone-900">{item.name}</h3>
                <Badge tone="brand">{collectionPointTypeLabels[item.type]}</Badge>
              </div>
              <p className="text-sm text-stone-600">{formatAddress(item.address)}</p>
              <div className="border-t border-stone-100 pt-2 text-xs text-stone-500">
                <p className="font-semibold text-stone-700">{item.contact.responsibleName}</p>
                <p className="mt-0.5">
                  {item.contact.phone} · {item.contact.email}
                </p>
              </div>
            </div>
          </Popup>
        </Marker>
      ))}

      <FitToPoints points={points} />
      <FlyToSelected points={points} selectedId={selectedId} markerRefs={markerRefs} />
    </MapContainer>
  )
}
