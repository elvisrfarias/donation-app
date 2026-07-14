import { formatAddress, type CollectionPoint } from '@/models/types'

/** Link do Google Maps para o ponto (usa coordenadas quando houver; senão o endereço). */
export function pointMapsUrl(point: CollectionPoint): string {
  const { latitude, longitude } = point.address
  const query =
    latitude != null && longitude != null
      ? `${latitude},${longitude}`
      : formatAddress(point.address)
  return `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(query)}`
}

/** Mensagem de divulgação do ponto para compartilhar (WhatsApp/redes). */
export function buildPointShareText(point: CollectionPoint): string {
  const lines = [
    `🙌 Ponto de doação: ${point.name}`,
    point.description || null,
    `📍 ${formatAddress(point.address)}`,
    point.openingHours ? `🕒 ${point.openingHours}` : null,
    point.needs.length > 0 ? `🎁 Precisa agora: ${point.needs.join(', ')}` : null,
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
      await navigator.share({ title: point.name, text })
    } catch {
      // Usuário cancelou a folha nativa — não faz fallback.
    }
    return
  }
  window.open(`https://wa.me/?text=${encodeURIComponent(text)}`, '_blank', 'noopener')
}
