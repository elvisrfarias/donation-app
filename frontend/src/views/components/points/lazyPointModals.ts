import { lazy } from 'react'

/**
 * Modais das páginas públicas carregados só quando abertos: trazem formulários e validação (zod) que a
 * primeira pintura do mapa não precisa.
 */
export const PledgeDonationModal = lazy(() =>
  import('@/views/components/donations/PledgeDonationModal').then((m) => ({
    default: m.PledgeDonationModal,
  })),
)

export const PointReviewsModal = lazy(() =>
  import('@/views/components/reviews/PointReviewsModal').then((m) => ({
    default: m.PointReviewsModal,
  })),
)
