import { useMutation } from '@tanstack/react-query'
import type { ApiError } from '@/models/api/problemDetails'
import { authService } from '@/models/services'
import type { LoginInput } from '@/models/types/auth'
import { useAuthStore } from '@/stores/auth'

/** ViewModel da tela de login: encapsula a chamada e a gravação da sessão. */
export function useLoginViewModel() {
  const setSession = useAuthStore((s) => s.setSession)

  const mutation = useMutation<void, ApiError, LoginInput>({
    mutationFn: async (input) => {
      const result = await authService.login(input)
      setSession(result)
    },
  })

  return {
    login: mutation.mutateAsync,
    isPending: mutation.isPending,
    error: mutation.error,
  }
}
