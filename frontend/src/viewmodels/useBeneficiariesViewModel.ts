import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useApiList, useApiMutation } from '@/lib/apiHooks'
import { queryKeys } from '@/lib/queryClient'
import type { ApiError } from '@/models/api/problemDetails'
import { beneficiaryService } from '@/models/services'
import type {
  AddBeneficiaryKinshipInput,
  Beneficiary,
  BeneficiaryWithdrawalHistory,
  RegisterBeneficiaryInput,
  UpdateBeneficiaryInput,
} from '@/models/types'

function invalidateBeneficiaries(
  queryClient: ReturnType<typeof useQueryClient>,
  beneficiaryId?: string,
) {
  queryClient.invalidateQueries({ queryKey: queryKeys.beneficiaries.all })
  if (beneficiaryId) {
    queryClient.invalidateQueries({ queryKey: queryKeys.beneficiaries.byId(beneficiaryId) })
  }
}

/** Lista de beneficiários visíveis ao usuário (Admin: todos; gestor: os do próprio ponto). */
export function useBeneficiaries() {
  const { items, ...rest } = useApiList<Beneficiary>({
    queryKey: queryKeys.beneficiaries.all,
    queryFn: ({ signal }) => beneficiaryService.list(signal),
  })

  return { beneficiaries: items, ...rest }
}

/**
 * Histórico de retiradas + saldo do ciclo de um beneficiário, segundo a regra do ponto informado. O
 * gestor só consulta pelo próprio ponto.
 */
export function useBeneficiaryWithdrawalHistory(
  beneficiaryId: string | null,
  pointId: string | null,
) {
  const query = useQuery<BeneficiaryWithdrawalHistory, ApiError>({
    queryKey: queryKeys.beneficiaries.withdrawals(beneficiaryId ?? '', pointId ?? ''),
    queryFn: () => beneficiaryService.getWithdrawalHistory(beneficiaryId ?? '', pointId ?? ''),
    enabled: !!beneficiaryId && !!pointId,
    retry: false,
  })

  return { history: query.data ?? null, isLoading: query.isLoading, error: query.error }
}

export function useRegisterBeneficiary() {
  const queryClient = useQueryClient()

  const { mutate, ...rest } = useApiMutation<Beneficiary, RegisterBeneficiaryInput>({
    mutationFn: (input) => beneficiaryService.register(input),
    onSuccess: () => invalidateBeneficiaries(queryClient),
  })

  return { register: mutate, ...rest }
}

/** Vincula uma pessoa já cadastrada ao ponto; ela passa a aparecer na lista do ponto. */
export function useLinkBeneficiaryToPoint() {
  const queryClient = useQueryClient()

  const { mutate, ...rest } = useApiMutation<
    Beneficiary,
    { beneficiaryId: string; pointId: string }
  >({
    mutationFn: ({ beneficiaryId, pointId }) =>
      beneficiaryService.linkToPoint(beneficiaryId, pointId),
    onSuccess: (_, { beneficiaryId }) => invalidateBeneficiaries(queryClient, beneficiaryId),
  })

  return { linkToPoint: mutate, ...rest }
}

export function useUpdateBeneficiary() {
  const queryClient = useQueryClient()

  const { mutate, ...rest } = useApiMutation<
    Beneficiary,
    { beneficiaryId: string; input: UpdateBeneficiaryInput }
  >({
    mutationFn: ({ beneficiaryId, input }) => beneficiaryService.update(beneficiaryId, input),
    onSuccess: (_, { beneficiaryId }) => invalidateBeneficiaries(queryClient, beneficiaryId),
  })

  return { update: mutate, ...rest }
}

/**
 * O vínculo é espelhado no outro beneficiário (com o tipo inverso), por isso invalida a lista
 * inteira, não só o beneficiário de origem.
 */
export function useAddBeneficiaryKinship() {
  const queryClient = useQueryClient()

  const { mutate, ...rest } = useApiMutation<
    Beneficiary,
    { beneficiaryId: string; input: AddBeneficiaryKinshipInput }
  >({
    mutationFn: ({ beneficiaryId, input }) => beneficiaryService.addKinship(beneficiaryId, input),
    onSuccess: (_, { beneficiaryId }) => invalidateBeneficiaries(queryClient, beneficiaryId),
  })

  return { addKinship: mutate, ...rest }
}
