import { formatAddress, type CollectionPoint } from '@/models/types'

function pointMapsQuery(point: CollectionPoint): string {
  const { latitude, longitude } = point.address
  return latitude != null && longitude != null
    ? `${latitude},${longitude}`
    : formatAddress(point.address)
}

/** Caminho da página pública do ponto, a que o Google indexa e que vai nos compartilhamentos. */
export function pointPagePath(point: Pick<CollectionPoint, 'id' | 'slug'>): string {
  return `/pontos/${point.id}/${point.slug}`
}

export function pointPageUrl(point: Pick<CollectionPoint, 'id' | 'slug'>): string {
  return new URL(pointPagePath(point), window.location.origin).href
}

/** Link do Google Maps para o ponto (usa coordenadas quando houver; senão o endereço). */
export function pointMapsUrl(point: CollectionPoint): string {
  return `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(pointMapsQuery(point))}`
}

/** Link do Google Maps com a rota até o ponto, partindo da localização atual de quem abrir. */
export function pointDirectionsUrl(point: CollectionPoint): string {
  return `https://www.google.com/maps/dir/?api=1&destination=${encodeURIComponent(pointMapsQuery(point))}`
}

/** Mensagem de divulgação do ponto para compartilhar (WhatsApp/redes). */
export function buildPointShareText(point: CollectionPoint): string {
  const lines = [
    `🙌 Ponto de doação: ${point.name}`,
    point.description || null,
    `📍 ${formatAddress(point.address)}`,
    point.openingHours ? `🕒 ${point.openingHours}` : null,
    point.needs.length > 0 ? `🎁 Precisa agora: ${point.needs.join(', ')}` : null,
    `🔗 ${pointPageUrl(point)}`,
    `🗺️ ${pointMapsUrl(point)}`,
  ].filter(Boolean)
  return lines.join('\n')
}

/**
 * Compartilha o ponto: usa a folha de compartilhamento nativa quando disponível
 * (celular) e cai no WhatsApp Web caso contrário.
 */
export async function sharePoint(point: CollectionPoint): Promise<void> {
  const text = buildPointShareText(point)
  if (typeof navigator !== 'undefined' && navigator.share) {
    try {
      await navigator.share({ title: point.name, text, url: pointPageUrl(point) })
    } catch {
      // Usuário cancelou a folha nativa — não faz fallback.
    }
    return
  }
  window.open(`https://wa.me/?text=${encodeURIComponent(text)}`, '_blank', 'noopener')
}
