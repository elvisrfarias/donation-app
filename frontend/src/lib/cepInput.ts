const CEP_DIGITS = 8
const CEP_PREFIX_DIGITS = 5

export function onlyCepDigits(text: string): string {
  return text.replace(/\D/g, '').slice(0, CEP_DIGITS)
}

export function maskCep(text: string): string {
  const digits = onlyCepDigits(text)
  if (digits.length <= CEP_PREFIX_DIGITS) return digits
  return `${digits.slice(0, CEP_PREFIX_DIGITS)}-${digits.slice(CEP_PREFIX_DIGITS)}`
}

export function isCompleteCep(text: string): boolean {
  return onlyCepDigits(text).length === CEP_DIGITS
}
