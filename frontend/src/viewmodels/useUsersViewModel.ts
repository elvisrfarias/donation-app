import { useQueryClient } from '@tanstack/react-query'
import { useApiList, useApiMutation } from '@/lib/apiHooks'
import { queryKeys } from '@/lib/queryClient'
import { userService } from '@/models/services'
import type { CreateUserInput, UpdateUserInput, UserAccount } from '@/models/types'

/** Lista de usuários gerenciais (Admin). */
export function useUsers() {
  const { items, ...rest } = useApiList<UserAccount>({
    queryKey: queryKeys.users.all,
    queryFn: ({ signal }) => userService.list(signal),
  })

  return { users: items, ...rest }
}

/** Mutação de criação de usuário gerencial (Admin). */
export function useCreateUser() {
  const queryClient = useQueryClient()

  const { mutate, ...rest } = useApiMutation<void, CreateUserInput>({
    mutationFn: (input) => userService.create(input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.users.all })
    },
  })

  return { create: mutate, ...rest }
}

/** Mutação de atualização de usuário gerencial (Admin). */
export function useUpdateUser() {
  const queryClient = useQueryClient()

  const { mutate, ...rest } = useApiMutation<UserAccount, { id: string; values: UpdateUserInput }>({
    mutationFn: ({ id, values }) => userService.update(id, values),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.users.all })
    },
  })

  return { update: mutate, ...rest }
}

/** Mutação de desativação de usuário gerencial (Admin). */
export function useDeactivateUser() {
  const queryClient = useQueryClient()

  const { mutate, ...rest } = useApiMutation<void, string>({
    mutationFn: (id) => userService.deactivate(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.users.all })
    },
  })

  return { deactivate: mutate, ...rest }
}
