import { TileLayer } from 'react-leaflet'

const OPEN_STREET_MAP_TILE_URL = 'https://tile.openstreetmap.org/{z}/{x}/{y}.png'
const OPEN_STREET_MAP_ATTRIBUTION =
  '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
const OPEN_STREET_MAP_MAX_ZOOM = 19

export function BaseMapTiles() {
  return (
    <TileLayer
      attribution={OPEN_STREET_MAP_ATTRIBUTION}
      url={OPEN_STREET_MAP_TILE_URL}
      maxZoom={OPEN_STREET_MAP_MAX_ZOOM}
    />
  )
}
