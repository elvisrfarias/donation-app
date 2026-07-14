import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '@/lib/queryClient'
import type { ApiError } from '@/models/api/problemDetails'
import { reviewService } from '@/models/services'
import type { CreateReviewInput, Review } from '@/models/types'

/**
 * ViewModel das avaliações de um ponto de coleta: lista as avaliações da comunidade e
 * permite registrar uma nova (nota + comentário), atualizando também a listagem de pontos
 * para refletir a nova média/ranking. Passar `pointId = null` desabilita a query.
 */
export function useReviewsViewModel(pointId: string | null) {
  const queryClient = useQueryClient()

  const reviewsQuery = useQuery<Review[], ApiError>({
    queryKey: pointId ? queryKeys.collectionPoints.reviews(pointId) : ['reviews', 'disabled'],
    queryFn: ({ signal }) => reviewService.listByPoint(pointId as string, signal),
    enabled: !!pointId,
  })

  const mutation = useMutation<Review, ApiError, CreateReviewInput>({
    mutationFn: (input) => reviewService.create(pointId as string, input),
    onSuccess: () => {
      if (pointId) {
        queryClient.invalidateQueries({ queryKey: queryKeys.collectionPoints.reviews(pointId) })
      }
      queryClient.invalidateQueries({ queryKey: queryKeys.collectionPoints.public })
    },
  })

  const reportMutation = useMutation<void, ApiError, string>({
    mutationFn: (reviewId) => reviewService.report(reviewId),
  })

  return {
    reviews: reviewsQuery.data ?? [],
    isLoading: reviewsQuery.isLoading,
    isError: reviewsQuery.isError,
    error: reviewsQuery.error,
    submit: mutation.mutateAsync,
    isSubmitting: mutation.isPending,
    submitError: mutation.error,
    submitted: mutation.isSuccess,
    resetSubmit: mutation.reset,
    report: reportMutation.mutateAsync,
  }
}
