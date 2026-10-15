import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '@/lib/queryClient'
import type { ApiError } from '@/models/api/problemDetails'
import { noticeService } from '@/models/services'
import type { Notice, PublishNoticeInput } from '@/models/types'

/** Avisos públicos de um ponto, para a página do mapa. `pointId = null` desabilita a query. */
export function usePublicNotices(pointId: string | null) {
  const query = useQuery<Notice[], ApiError>({
    queryKey: queryKeys.notices.public(pointId ?? ''),
    queryFn: ({ signal }) => noticeService.listPublic(pointId as string, signal),
    enabled: pointId !== null,
  })

  return { notices: query.data ?? [], isLoading: query.isLoading }
}

/** Mural da rede (gestão): avisos de todos os pontos. */
export function useNetworkNotices() {
  const query = useQuery<Notice[], ApiError>({
    queryKey: queryKeys.notices.network,
    queryFn: ({ signal }) => noticeService.listNetwork(signal),
  })

  return {
    notices: query.data ?? [],
    isLoading: query.isLoading,
    isError: query.isError,
    error: query.error,
  }
}

function useNoticeChangeInvalidation(pointId: string) {
  const queryClient = useQueryClient()

  return () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: queryKeys.notices.network }),
      queryClient.invalidateQueries({ queryKey: queryKeys.notices.public(pointId) }),
      queryClient.invalidateQueries({ queryKey: queryKeys.activity.byPoint(pointId) }),
    ])
}

/** Publica um aviso no mural do ponto do gestor. */
export function usePublishNotice(pointId: string) {
  const invalidate = useNoticeChangeInvalidation(pointId)

  return useMutation<Notice, ApiError, PublishNoticeInput>({
    mutationFn: (input) => noticeService.publish(pointId, input),
    onSuccess: invalidate,
  })
}

/** Retira um aviso do mural do ponto do gestor. */
export function useArchiveNotice(pointId: string) {
  const invalidate = useNoticeChangeInvalidation(pointId)

  return useMutation<void, ApiError, string>({
    mutationFn: (noticeId) => noticeService.archive(pointId, noticeId),
    onSuccess: invalidate,
  })
}
