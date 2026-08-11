import { useMutation } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import type { ApiError } from '@/models/api/problemDetails'
import { authService } from '@/models/services'
import type { LoginInput } from '@/models/types/auth'
import { useAuthStore } from '@/stores/auth'

/** ViewModel da tela de login: encapsula a chamada, a gravação da sessão e o aviso de sessão expirada. */
export function useLoginViewModel() {
  const setSession = useAuthStore((s) => s.setSession)
  const clearSessionExpiredMessage = useAuthStore((s) => s.clearSessionExpiredMessage)
  const [sessionExpiredMessage] = useState(() => useAuthStore.getState().sessionExpiredMessage)

  useEffect(() => {
    clearSessionExpiredMessage()
  }, [clearSessionExpiredMessage])

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
    sessionExpiredMessage,
  }
}
