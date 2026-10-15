import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '@/lib/queryClient'
import type { ApiError } from '@/models/api/problemDetails'
import { donationService } from '@/models/services'
import type { Donation, PledgeDonationInput, ReceiveDonationInput } from '@/models/types'

const DONATIONS_REFRESH_INTERVAL_MS = 60_000

/** Doações do ponto do gestor, atualizadas a cada minuto. Passar `pointId = null` desabilita a query. */
export function usePointDonations(pointId: string | null) {
  const query = useQuery<Donation[], ApiError>({
    queryKey: queryKeys.donations.byPoint(pointId ?? ''),
    queryFn: ({ signal }) => donationService.listByPoint(pointId as string, signal),
    enabled: pointId !== null,
    refetchInterval: DONATIONS_REFRESH_INTERVAL_MS,
  })

  return {
    donations: query.data ?? [],
    isLoading: query.isLoading,
    isError: query.isError,
    error: query.error,
  }
}

/** Registra, de forma pública, que alguém pretende doar a um ponto. */
export function usePledgeDonation(pointId: string) {
  const mutation = useMutation<Donation, ApiError, PledgeDonationInput>({
    mutationFn: (input) => donationService.pledge(pointId, input),
  })

  return {
    pledge: mutation.mutate,
    isPledging: mutation.isPending,
    error: mutation.error,
    pledged: mutation.isSuccess,
    reset: mutation.reset,
  }
}

function useDonationChangeInvalidation(pointId: string) {
  const queryClient = useQueryClient()

  return () =>
    Promise.all([
      queryClient.invalidateQueries({ queryKey: queryKeys.donations.byPoint(pointId) }),
      queryClient.invalidateQueries({ queryKey: queryKeys.activity.byPoint(pointId) }),
      queryClient.invalidateQueries({ queryKey: queryKeys.collectionPoints.detail(pointId) }),
    ])
}

interface ReceiveDonationVariables {
  donationId: string
  input: ReceiveDonationInput
}

/** Confirma o recebimento de uma doação, somando ao estoque o que foi informado. */
export function useReceiveDonation(pointId: string) {
  const invalidate = useDonationChangeInvalidation(pointId)

  return useMutation<Donation, ApiError, ReceiveDonationVariables>({
    mutationFn: ({ donationId, input }) => donationService.receive(pointId, donationId, input),
    onSuccess: invalidate,
  })
}

/** Cancela uma doação prometida. */
export function useCancelDonation(pointId: string) {
  const invalidate = useDonationChangeInvalidation(pointId)

  return useMutation<Donation, ApiError, string>({
    mutationFn: (donationId) => donationService.cancel(pointId, donationId),
    onSuccess: invalidate,
  })
}
