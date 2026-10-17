import { useApiMutation } from '@/lib/apiHooks'
import { authService } from '@/models/services'
import type { ChangePasswordInput } from '@/models/types/auth'
import { useAuthStore } from '@/stores/auth'

/**
 * ViewModel da troca de senha do usuário logado. O backend encerra as demais sessões e devolve um novo
 * par de tokens, que substitui o da sessão atual para o usuário continuar logado.
 */
export function useChangePasswordViewModel() {
  const setSession = useAuthStore((s) => s.setSession)

  const { mutate, ...rest } = useApiMutation<void, ChangePasswordInput>({
    mutationFn: async (input) => {
      const result = await authService.changePassword(input)
      setSession(result)
    },
  })

  return { changePassword: mutate, ...rest }
}
