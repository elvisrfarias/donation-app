import L from 'leaflet'
import type { CollectionPointType } from '@/models/types'

export const colorByType: Record<CollectionPointType, string> = {
  Association: '#059669', // emerald (marca)
  Church: '#0d9488', // teal
  PublicEntity: '#d97706', // amber
}

/**
 * Cria um marcador SVG inline (DivIcon) em formato de coração — evita o problema
 * clássico de assets quebrados dos ícones padrão do Leaflet com bundlers. O ponto
 * selecionado ganha um halo pulsante.
 */
export function createMarkerIcon(type: CollectionPointType, selected = false): L.DivIcon {
  const color = colorByType[type]
  const size = selected ? 40 : 30
  const anchorY = Math.round(size * 0.85)

  const halo = selected ? `<span class="dz-halo" style="--dz-color:${color}"></span>` : ''

  const html = `
    <div class="dz-marker" style="width:${size}px;height:${size}px;">
      ${halo}
      <svg viewBox="0 0 24 24" width="${size}" height="${size}" style="filter: drop-shadow(0 3px 5px rgba(28,25,23,.35));">
        <path fill="${color}" stroke="white" stroke-width="1.5" stroke-linejoin="round"
          d="M12 20.3l-1.45-1.32C5.4 14.25 2 11.18 2 7.5 2 4.42 4.42 2 7.5 2c1.74 0 3.41.81 4.5 2.09C13.09 2.81 14.76 2 16.5 2 19.58 2 22 4.42 22 7.5c0 3.68-3.4 6.75-8.55 11.49L12 20.3z"/>
      </svg>
    </div>`

  return L.divIcon({
    html,
    className: '',
    iconSize: [size, size],
    iconAnchor: [size / 2, anchorY],
    popupAnchor: [0, -anchorY + 2],
  })
}
