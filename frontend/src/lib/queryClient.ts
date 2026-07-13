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
  },
  families: {
    all: ['families', 'all'] as const,
    byId: (id: string) => ['families', id] as const,
    withdrawals: (id: string) => ['families', id, 'withdrawals'] as const,
  },
  geocode: (query: string) => ['geocode', query] as const,
}
