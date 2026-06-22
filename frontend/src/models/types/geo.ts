export interface LatLng {
  lat: number
  lng: number
}

/** Um item de domínio acrescido da posição geográfica resolvida (ou null se não geocodificável). */
export interface Geocoded<T> {
  item: T
  position: LatLng | null
}
