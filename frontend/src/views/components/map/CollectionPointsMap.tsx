import { useEffect, useRef, type RefObject } from 'react'
import L from 'leaflet'
import { Clock, HeartHandshake } from 'lucide-react'
import { MapContainer, Marker, Popup, TileLayer, useMap, ZoomControl } from 'react-leaflet'
import { env } from '@/lib/env'
import {
  collectionPointTypeLabels,
  formatAddress,
  itemCategoryLabels,
  type CollectionPoint,
  type Geocoded,
  type LatLng,
} from '@/models/types'
import { Badge } from '@/views/components/ui'
import { StarRating } from '@/views/components/reviews/StarRating'
import { createMarkerIcon } from './markerIcon'

interface Props {
  points: Geocoded<CollectionPoint>[]
  selectedId: string | null
  onSelect: (id: string | null) => void
  userLocation?: LatLng | null
}

const userLocationIcon = L.divIcon({
  className: '',
  html: '<div style="width:16px;height:16px;border-radius:9999px;background:#2563eb;border:3px solid white;box-shadow:0 0 0 4px rgba(37,99,235,.25)"></div>',
  iconSize: [16, 16],
  iconAnchor: [8, 8],
})

/** Centraliza o mapa na localização do usuário quando ela é obtida. */
function FlyToUser({ position }: { position: LatLng | null | undefined }) {
  const map = useMap()
  useEffect(() => {
    if (position) {
      map.flyTo([position.lat, position.lng], Math.max(map.getZoom(), 13), { duration: 0.8 })
    }
  }, [position, map])
  return null
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

export function CollectionPointsMap({ points, selectedId, onSelect, userLocation }: Props) {
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
              {item.description && (
                <p className="line-clamp-3 text-xs text-stone-500">{item.description}</p>
              )}
              {item.reviewCount > 0 && (
                <div className="flex items-center gap-1.5">
                  <StarRating value={item.averageRating} size="sm" />
                  <span className="text-xs font-medium text-stone-500">
                    {item.averageRating.toFixed(1)} ({item.reviewCount})
                  </span>
                </div>
              )}
              {item.familiesServed > 0 && (
                <p className="flex items-center gap-1.5 text-xs font-medium text-emerald-600">
                  <HeartHandshake className="size-3.5" />
                  {item.familiesServed}{' '}
                  {item.familiesServed === 1 ? 'família atendida' : 'famílias atendidas'}
                </p>
              )}
              {item.openingHours && (
                <p className="flex items-center gap-1.5 text-xs text-stone-500">
                  <Clock className="size-3.5 shrink-0" />
                  {item.openingHours}
                </p>
              )}
              {item.needs.length > 0 && (
                <div>
                  <p className="text-[10px] font-bold uppercase tracking-wide text-amber-600">
                    Precisa agora
                  </p>
                  <div className="mt-1 flex flex-wrap gap-1">
                    {item.needs.map((need) => (
                      <span
                        key={need}
                        className="rounded-full bg-amber-50 px-2 py-0.5 text-[11px] font-medium text-amber-700 ring-1 ring-inset ring-amber-200"
                      >
                        {need}
                      </span>
                    ))}
                  </div>
                </div>
              )}
              {item.stock.length > 0 && (
                <div>
                  <p className="text-[10px] font-bold uppercase tracking-wide text-emerald-600">
                    Disponível agora
                  </p>
                  <div className="mt-1 flex flex-wrap gap-1">
                    {item.stock.map((s) => (
                      <span
                        key={s.category}
                        className="rounded-full bg-emerald-50 px-2 py-0.5 text-[11px] font-medium text-emerald-700 ring-1 ring-inset ring-emerald-200"
                      >
                        {itemCategoryLabels[s.category]}: {s.quantity}
                      </span>
                    ))}
                  </div>
                </div>
              )}
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

      {userLocation && (
        <Marker position={[userLocation.lat, userLocation.lng]} icon={userLocationIcon}>
          <Popup>Você está aqui</Popup>
        </Marker>
      )}

      <FitToPoints points={points} />
      <FlyToSelected points={points} selectedId={selectedId} markerRefs={markerRefs} />
      <FlyToUser position={userLocation} />
    </MapContainer>
  )
}
