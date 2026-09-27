import { useQueryClient } from '@tanstack/react-query'
import { useApiMutation } from '@/lib/apiHooks'
import { queryKeys } from '@/lib/queryClient'
import { withdrawalService } from '@/models/services'
import type { RegisterWithdrawalInput, UpdateWithdrawalInput, Withdrawal } from '@/models/types'

function invalidateWithdrawalQueries(
  queryClient: ReturnType<typeof useQueryClient>,
  withdrawal: Withdrawal,
) {
  queryClient.invalidateQueries({
    queryKey: queryKeys.beneficiaries.withdrawals(withdrawal.beneficiaryId),
  })
  queryClient.invalidateQueries({
    queryKey: queryKeys.beneficiaries.byId(withdrawal.beneficiaryId),
  })
  queryClient.invalidateQueries({
    queryKey: queryKeys.collectionPoints.dashboard(withdrawal.pointId),
  })
}

/** ViewModel do registro de retirada — a regra de equidade é aplicada no backend. */
export function useRegisterWithdrawal() {
  const queryClient = useQueryClient()

  const { mutate, data, ...rest } = useApiMutation<Withdrawal, RegisterWithdrawalInput>({
    mutationFn: (input) => withdrawalService.register(input),
    onSuccess: (withdrawal) => invalidateWithdrawalQueries(queryClient, withdrawal),
  })

  return { register: mutate, lastWithdrawal: data, ...rest }
}

/**
 * ViewModel da edição de retirada — só a mais recente do beneficiário pode ser editada; o
 * backend recusa (409) quando não for.
 */
export function useUpdateWithdrawal() {
  const queryClient = useQueryClient()

  const { mutate, data, ...rest } = useApiMutation<
    Withdrawal,
    { withdrawalId: string; input: UpdateWithdrawalInput }
  >({
    mutationFn: ({ withdrawalId, input }) => withdrawalService.update(withdrawalId, input),
    onSuccess: (withdrawal) => invalidateWithdrawalQueries(queryClient, withdrawal),
  })

  return { update: mutate, updatedWithdrawal: data, ...rest }
}
