import { whatsappUrl } from '@/lib/whatsapp'
import { WhatsAppIcon } from '@/views/components/ui/WhatsAppIcon'

interface WhatsAppLinkProps {
  phone: string
  personName: string
}

export function WhatsAppLink({ phone, personName }: WhatsAppLinkProps) {
  return (
    <a
      href={whatsappUrl(phone)}
      target="_blank"
      rel="noopener noreferrer"
      aria-label={`Conversar com ${personName} no WhatsApp (${phone})`}
      title="Abrir conversa no WhatsApp"
      className="inline-grid size-7 shrink-0 place-items-center rounded-full bg-emerald-50 text-emerald-700 ring-1 ring-emerald-200 transition-colors ring-inset hover:bg-emerald-600 hover:text-white"
    >
      <WhatsAppIcon className="size-4" />
    </a>
  )
}
