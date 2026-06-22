import L from 'leaflet'
import type { CollectionPointType } from '@/models/types'

const colorByType: Record<CollectionPointType, string> = {
  Association: '#4f46e5', // indigo (marca)
  Church: '#0891b2', // cyan
  PublicEntity: '#059669', // emerald
}

/**
 * Cria um marcador SVG inline (DivIcon) — evita o problema clássico de assets
 * quebrados dos ícones padrão do Leaflet com bundlers.
 */
export function createMarkerIcon(type: CollectionPointType, selected = false): L.DivIcon {
  const color = colorByType[type]
  const size = selected ? 44 : 34

  const html = `
    <div class="dz-marker" style="width:${size}px;height:${size}px;">
      <svg viewBox="0 0 24 24" width="${size}" height="${size}" style="filter: drop-shadow(0 2px 4px rgba(0,0,0,.35));">
        <path fill="${color}" stroke="white" stroke-width="1.5"
          d="M12 2C7.6 2 4 5.6 4 10c0 6 8 12 8 12s8-6 8-12c0-4.4-3.6-8-8-8z"/>
        <circle cx="12" cy="10" r="3" fill="white"/>
      </svg>
    </div>`

  return L.divIcon({
    html,
    className: '',
    iconSize: [size, size],
    iconAnchor: [size / 2, size],
    popupAnchor: [0, -size + 6],
  })
}
