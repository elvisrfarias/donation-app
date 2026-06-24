import { AxiosError } from 'axios'

export type FieldErrors = Record<string, string[]>

interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  errors?: FieldErrors
}

/**
 * Erro normalizado da API. O backend retorna RFC7807 ProblemDetails (incluindo
 * `errors` em respostas de validação 400). Toda a UI lida apenas com esta classe.
 */
export class ApiError extends Error {
  readonly status: number
  readonly title: string
  readonly detail?: string
  readonly fieldErrors?: FieldErrors

  constructor(params: {
    status: number
    title: string
    detail?: string
    fieldErrors?: FieldErrors
  }) {
    super(params.detail || params.title)
    this.name = 'ApiError'
    this.status = params.status
    this.title = params.title
    this.detail = params.detail
    this.fieldErrors = params.fieldErrors
  }

  get isNetwork(): boolean {
    return this.status === 0
  }
  get isUnauthorized(): boolean {
    return this.status === 401
  }
  get isForbidden(): boolean {
    return this.status === 403
  }
  get isNotFound(): boolean {
    return this.status === 404
  }
  get isConflict(): boolean {
    return this.status === 409
  }
  get isValidation(): boolean {
    return this.status === 400 && !!this.fieldErrors
  }

  /** Mensagem amigável: primeiro erro de validação, senão detalhe, senão título. */
  get displayMessage(): string {
    if (this.fieldErrors) {
      const first = Object.values(this.fieldErrors)[0]?.[0]
      if (first) return first
    }
    return this.detail || this.title
  }
}

export function toApiError(error: unknown): ApiError {
  if (error instanceof ApiError) return error

  if (error instanceof AxiosError) {
    const res = error.response
    if (res) {
      const data = (res.data ?? {}) as ProblemDetails
      return new ApiError({
        status: res.status,
        title: data.title || res.statusText || 'Erro na requisição',
        detail: data.detail,
        fieldErrors: normalizeFieldErrors(data.errors),
      })
    }
    return new ApiError({
      status: 0,
      title: 'Falha de conexão',
      detail: 'Não foi possível conectar à API. Verifique se o backend está no ar.',
    })
  }

  return new ApiError({
    status: 0,
    title: 'Erro inesperado',
    detail: error instanceof Error ? error.message : undefined,
  })
}

function normalizeFieldErrors(errors?: FieldErrors): FieldErrors | undefined {
  if (!errors || typeof errors !== 'object') return undefined
  const out: FieldErrors = {}
  for (const [k, v] of Object.entries(errors)) {
    const key = k.charAt(0).toLowerCase() + k.slice(1)
    out[key] = Array.isArray(v) ? v : [String(v)]
  }
  return Object.keys(out).length ? out : undefined
}
