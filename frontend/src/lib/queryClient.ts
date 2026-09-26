import { QueryClient } from '@tanstack/react-query'

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      gcTime: 5 * 60_000,
      retry: 1,
      refetchOnWindowFocus: false,
    },
    mutations: {
      retry: 0,
    },
  },
})

/** Chaves de query centralizadas — evita strings mágicas espalhadas. */
export const queryKeys = {
  collectionPoints: {
    public: ['collection-points', 'public'] as const,
    management: ['collection-points', 'management'] as const,
    detail: (pointId: string) => ['collection-points', 'detail', pointId] as const,
    reviews: (pointId: string) => ['collection-points', pointId, 'reviews'] as const,
    dashboard: (pointId: string, from?: string, to?: string) =>
      from && to
        ? (['collection-points', pointId, 'dashboard', from, to] as const)
        : (['collection-points', pointId, 'dashboard'] as const),
  },
  beneficiaries: {
    all: ['beneficiaries', 'all'] as const,
    byId: (id: string) => ['beneficiaries', id] as const,
    withdrawals: (id: string, pointId?: string) =>
      pointId
        ? (['beneficiaries', id, 'withdrawals', pointId] as const)
        : (['beneficiaries', id, 'withdrawals'] as const),
  },
  users: {
    all: ['users', 'all'] as const,
  },
  geocode: (query: string) => ['geocode', query] as const,
}
