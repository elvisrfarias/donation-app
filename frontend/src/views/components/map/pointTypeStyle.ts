import type { CSSProperties } from 'react'
import { Building2, Church, Landmark } from 'lucide-react'
import type { CollectionPointType } from '@/models/types'
import { colorByType } from './markerIcon'

export const pointTypeIcon: Record<CollectionPointType, typeof Building2> = {
  Association: Building2,
  Church,
  PublicEntity: Landmark,
}

/**
 * Fundo claro com o texto escurecido da cor do tipo de ponto. O texto fica bem mais escuro que a cor do
 * marcador para manter contraste de leitura (WCAG AA) mesmo nas cores mais claras da paleta.
 */
export function pointTypeSoftStyle(type: CollectionPointType): CSSProperties {
  const color = colorByType[type]
  return {
    backgroundColor: `color-mix(in oklab, ${color}, white 90%)`,
    color: `color-mix(in oklab, ${color}, black 45%)`,
    boxShadow: `inset 0 0 0 1px color-mix(in oklab, ${color}, white 80%)`,
  }
}
