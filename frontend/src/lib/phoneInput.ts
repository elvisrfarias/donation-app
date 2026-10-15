const MAX_PHONE_DIGITS = 11
const LANDLINE_DIGITS = 10
const AREA_CODE_DIGITS = 2

export function maskPhone(text: string): string {
  const digits = text.replace(/\D/g, '').slice(0, MAX_PHONE_DIGITS)
  if (digits.length === 0) return ''
  const areaCode = digits.slice(0, AREA_CODE_DIGITS)
  const number = digits.slice(AREA_CODE_DIGITS)
  if (number.length === 0) return `(${areaCode}`
  const prefixLength = digits.length > LANDLINE_DIGITS ? 5 : 4
  const prefix = number.slice(0, prefixLength)
  const suffix = number.slice(prefixLength)
  return `(${areaCode}) ${prefix}${suffix ? `-${suffix}` : ''}`
}

export function isValidPhone(text: string): boolean {
  const digitCount = text.replace(/\D/g, '').length
  return digitCount === LANDLINE_DIGITS || digitCount === MAX_PHONE_DIGITS
}
