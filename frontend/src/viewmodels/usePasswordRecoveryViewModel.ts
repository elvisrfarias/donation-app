import { useApiMutation } from '@/lib/apiHooks'
import { authService } from '@/models/services'
import type { CompletePasswordResetInput, RequestPasswordResetInput } from '@/models/types/auth'

/** Pede o envio do link de redefinição de senha para o e-mail informado. */
export function useRequestPasswordReset() {
  const { mutate, ...rest } = useApiMutation<void, RequestPasswordResetInput>({
    mutationFn: (input) => authService.requestPasswordReset(input),
  })

  return { requestPasswordReset: mutate, ...rest }
}

/** Define a nova senha com o token recebido no link do e-mail. */
export function useCompletePasswordReset() {
  const { mutate, ...rest } = useApiMutation<void, CompletePasswordResetInput>({
    mutationFn: (input) => authService.completePasswordReset(input),
  })

  return { completePasswordReset: mutate, ...rest }
}
