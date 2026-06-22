import { useMutation, useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '@/lib/queryClient'
import type { ApiError } from '@/models/api/problemDetails'
import { withdrawalService } from '@/models/services'
import type { RegisterWithdrawalInput, Withdrawal } from '@/models/types'

/** ViewModel do registro de retirada — a regra de equidade é aplicada no backend. */
export function useRegisterWithdrawal() {
  const queryClient = useQueryClient()

  const mutation = useMutation<Withdrawal, ApiError, RegisterWithdrawalInput>({
    mutationFn: (input) => withdrawalService.register(input),
    onSuccess: (withdrawal) => {
      queryClient.invalidateQueries({
        queryKey: queryKeys.families.withdrawals(withdrawal.familyId),
      })
      queryClient.invalidateQueries({ queryKey: queryKeys.families.byId(withdrawal.familyId) })
    },
  })

  return {
    register: mutation.mutateAsync,
    isPending: mutation.isPending,
    error: mutation.error,
    lastWithdrawal: mutation.data,
    reset: mutation.reset,
  }
}
