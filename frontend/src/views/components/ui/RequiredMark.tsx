const REQUIRED_MARK_COLOR = '#dc2626'

/**
 * Asterisco de campo obrigatório. A cor é fixa (não segue o tema da marca) para que o
 * sinal de obrigatório seja sempre o mesmo em todo o sistema.
 */
export function RequiredMark() {
  return (
    <span
      aria-hidden="true"
      data-required-mark
      className="ml-1 font-bold after:content-['*']"
      style={{ color: REQUIRED_MARK_COLOR }}
    />
  )
}
