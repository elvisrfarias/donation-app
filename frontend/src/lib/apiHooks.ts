import {
  useMutation,
  useQuery,
  type UseMutationOptions,
  type UseQueryOptions,
} from '@tanstack/react-query'
import type { ApiError } from '@/models/api/problemDetails'

/**
 * Shape comum de toda mutação da API neste app — o único ponto que varia entre viewmodels é o
 * nome do verbo exposto ao caller. Centraliza `mutateAsync`/`isPending`/`error`/`reset` num só
 * lugar para não divergir (ex.: um hook novo esquecer o `reset`).
 */
export function useApiMutation<TData, TVariables>(
  options: UseMutationOptions<TData, ApiError, TVariables>,
) {
  const mutation = useMutation<TData, ApiError, TVariables>(options)

  return {
    mutate: mutation.mutateAsync,
    isPending: mutation.isPending,
    error: mutation.error,
    data: mutation.data,
    reset: mutation.reset,
  }
}

/** Shape comum de toda listagem da API neste app — o único ponto que varia é o nome da lista. */
export function useApiList<TData>(options: UseQueryOptions<TData[], ApiError>) {
  const query = useQuery<TData[], ApiError>(options)

  return {
    items: query.data ?? [],
    isLoading: query.isLoading,
    isError: query.isError,
    error: query.error,
    refetch: query.refetch,
  }
}
