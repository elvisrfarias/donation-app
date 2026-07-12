import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { queryKeys } from '@/lib/queryClient'
import type { ApiError } from '@/models/api/problemDetails'
import { familyService } from '@/models/services'
import type {
  Beneficiary,
  CreateFamilyInput,
  Family,
  FamilyWithdrawalHistory,
  RegisterBeneficiaryInput,
  UpdateBeneficiaryInput,
} from '@/models/types'

export function useCreateFamily() {
  const mutation = useMutation<Family, ApiError, CreateFamilyInput>({
    mutationFn: (input) => familyService.create(input),
  })

  return {
    create: mutation.mutateAsync,
    isPending: mutation.isPending,
    error: mutation.error,
    createdFamily: mutation.data,
    reset: mutation.reset,
  }
}

/** Carrega uma família e seu histórico de retiradas (saldo do ciclo vigente). */
export function useFamily(familyId: string | null) {
  const enabled = !!familyId
  const id = familyId ?? ''

  const familyQuery = useQuery<Family, ApiError>({
    queryKey: queryKeys.families.byId(id),
    queryFn: () => familyService.getById(id),
    enabled,
  })

  const historyQuery = useQuery<FamilyWithdrawalHistory, ApiError>({
    queryKey: queryKeys.families.withdrawals(id),
    queryFn: () => familyService.getWithdrawalHistory(id),
    enabled,
  })

  return {
    family: familyQuery.data ?? null,
    history: historyQuery.data ?? null,
    isLoading: enabled && (familyQuery.isLoading || historyQuery.isLoading),
    isFetching: familyQuery.isFetching || historyQuery.isFetching,
    error: familyQuery.error ?? historyQuery.error ?? null,
    notFound: familyQuery.error?.isNotFound ?? false,
  }
}

export function useRegisterBeneficiary(familyId: string) {
  const queryClient = useQueryClient()

  const mutation = useMutation<Beneficiary, ApiError, RegisterBeneficiaryInput>({
    mutationFn: (input) => familyService.registerBeneficiary(familyId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.families.byId(familyId) })
    },
  })

  return {
    register: mutation.mutateAsync,
    isPending: mutation.isPending,
    error: mutation.error,
    reset: mutation.reset,
  }
}

export function useUpdateBeneficiary(familyId: string) {
  const queryClient = useQueryClient()

  const mutation = useMutation<
    Beneficiary,
    ApiError,
    { beneficiaryId: string; input: UpdateBeneficiaryInput }
  >({
    mutationFn: ({ beneficiaryId, input }) =>
      familyService.updateBeneficiary(familyId, beneficiaryId, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.families.byId(familyId) })
    },
  })

  return {
    update: mutation.mutateAsync,
    isPending: mutation.isPending,
    error: mutation.error,
    reset: mutation.reset,
  }
}
