import { useQueryClient } from '@tanstack/react-query'
import { useApiMutation } from '@/lib/apiHooks'
import { queryKeys } from '@/lib/queryClient'
import { withdrawalService } from '@/models/services'
import type { RegisterWithdrawalInput, Withdrawal } from '@/models/types'

/** ViewModel do registro de retirada — a regra de equidade é aplicada no backend. */
export function useRegisterWithdrawal() {
  const queryClient = useQueryClient()

  const { mutate, data, ...rest } = useApiMutation<Withdrawal, RegisterWithdrawalInput>({
    mutationFn: (input) => withdrawalService.register(input),
    onSuccess: (withdrawal) => {
      queryClient.invalidateQueries({
        queryKey: queryKeys.beneficiaries.withdrawals(withdrawal.beneficiaryId),
      })
      queryClient.invalidateQueries({
        queryKey: queryKeys.beneficiaries.byId(withdrawal.beneficiaryId),
      })
      queryClient.invalidateQueries({
        queryKey: queryKeys.collectionPoints.dashboard(withdrawal.pointId),
      })
    },
  })

  return { register: mutate, lastWithdrawal: data, ...rest }
}
