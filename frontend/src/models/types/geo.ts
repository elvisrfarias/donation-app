export interface LatLng {
  lat: number
  lng: number
}

/** Um item de domínio acrescido da posição geográfica resolvida (ou null se não geocodificável). */
export interface Geocoded<T> {
  item: T
  position: LatLng | null
  /** Verdadeiro quando só foi possível localizar a cidade (o pin marca o centro dela, não o endereço). */
  approximate?: boolean
}

/** Posição resolvida para um endereço, indicando se ela é só a da cidade. */
export interface GeocodedAddress {
  position: LatLng
  approximate: boolean
}
