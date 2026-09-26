const CPF_DIGITS = 11

export function onlyCpfDigits(text: string): string {
  return text.replace(/\D/g, '').slice(0, CPF_DIGITS)
}

export function maskCpf(text: string): string {
  const digits = onlyCpfDigits(text)
  const block1 = digits.slice(0, 3)
  const block2 = digits.slice(3, 6)
  const block3 = digits.slice(6, 9)
  const checkDigits = digits.slice(9)
  return [block1, block2, block3].filter(Boolean).join('.') + (checkDigits ? `-${checkDigits}` : '')
}

export function isCompleteCpf(text: string): boolean {
  return onlyCpfDigits(text).length === CPF_DIGITS
}
