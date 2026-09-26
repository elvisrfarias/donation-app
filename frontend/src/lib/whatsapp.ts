const BRAZIL_COUNTRY_CODE = '55'

export function whatsappUrl(phone: string): string {
  const digits = phone.replace(/\D/g, '')
  return `https://wa.me/${BRAZIL_COUNTRY_CODE}${digits}`
}
