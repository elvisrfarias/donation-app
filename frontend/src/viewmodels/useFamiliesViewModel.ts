import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useApiList, useApiMutation } from '@/lib/apiHooks'
import { queryKeys } from '@/lib/queryClient'
import type { ApiError } from '@/models/api/problemDetails'
import { familyService } from '@/models/services'
import type {
  AddBeneficiaryKinshipInput,
  Beneficiary,
  CreateFamilyInput,
  Family,
  FamilyWithdrawalHistory,
  RegisterBeneficiaryInput,
  UpdateBeneficiaryInput,
} from '@/models/types'

export function useCreateFamily() {
  const queryClient = useQueryClient()

  const { mutate, data, ...rest } = useApiMutation<Family, CreateFamilyInput>({
    mutationFn: (input) => familyService.create(input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.families.all })
    },
  })

  return { create: mutate, createdFamily: data, ...rest }
}

/** Lista todas as famílias com seus beneficiários. */
export function useFamilies() {
  const { items, ...rest } = useApiList<Family>({
    queryKey: queryKeys.families.all,
    queryFn: ({ signal }) => familyService.list(signal),
  })

  return { families: items, ...rest }
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

export function useRegisterBeneficiary() {
  const queryClient = useQueryClient()

  const { mutate, ...rest } = useApiMutation<
    Beneficiary,
    { familyId: string } & RegisterBeneficiaryInput
  >({
    mutationFn: ({ familyId, ...input }) => familyService.registerBeneficiary(familyId, input),
    onSuccess: (_, { familyId }) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.families.byId(familyId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.families.all })
    },
  })

  return { register: mutate, ...rest }
}

export function useUpdateBeneficiary() {
  const queryClient = useQueryClient()

  const { mutate, ...rest } = useApiMutation<
    Beneficiary,
    { familyId: string; beneficiaryId: string; input: UpdateBeneficiaryInput }
  >({
    mutationFn: ({ familyId, beneficiaryId, input }) =>
      familyService.updateBeneficiary(familyId, beneficiaryId, input),
    onSuccess: (_, { familyId }) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.families.byId(familyId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.families.all })
    },
  })

  return { update: mutate, ...rest }
}

/**
 * Registra um vínculo de parentesco de um beneficiário com outro (por CPF). Um vínculo
 * "Cônjuge" entre famílias diferentes pode mover o beneficiário para uma família nova —
 * por isso invalida a lista inteira de famílias, não só a de origem.
 */
export function useAddBeneficiaryKinship() {
  const queryClient = useQueryClient()

  const { mutate, ...rest } = useApiMutation<
    Beneficiary,
    { familyId: string; beneficiaryId: string; input: AddBeneficiaryKinshipInput }
  >({
    mutationFn: ({ familyId, beneficiaryId, input }) =>
      familyService.addBeneficiaryKinship(familyId, beneficiaryId, input),
    onSuccess: (updatedBeneficiary, { familyId }) => {
      queryClient.invalidateQueries({ queryKey: queryKeys.families.byId(familyId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.families.byId(updatedBeneficiary.familyId) })
      queryClient.invalidateQueries({ queryKey: queryKeys.families.all })
    },
  })

  return { addKinship: mutate, ...rest }
}
