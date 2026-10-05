import { useState } from 'react'
import { Check, Copy, Heart } from 'lucide-react'
import { QRCodeSVG } from 'qrcode.react'
import { PIX_COPY_AND_PASTE_CODE, PIX_KEY, PIX_RECEIVER_NAME } from '@/lib/donationPix'
import { Reveal } from '@/views/components/common/Reveal'
import { Button } from '@/views/components/ui'

const COPIED_FEEDBACK_MILLISECONDS = 2500

function useCopyToClipboard() {
  const [copied, setCopied] = useState(false)

  const copy = async (text: string) => {
    await navigator.clipboard.writeText(text)
    setCopied(true)
    setTimeout(() => setCopied(false), COPIED_FEEDBACK_MILLISECONDS)
  }

  return { copied, copy }
}

export function SupportProjectSection() {
  const { copied, copy } = useCopyToClipboard()

  return (
    <section id="apoie" className="mx-auto w-full max-w-6xl px-4 py-16 sm:px-6 lg:py-24">
      <Reveal className="from-brand-600 to-brand-800 shadow-lift relative grid items-center gap-10 overflow-hidden rounded-4xl bg-linear-to-br p-6 sm:p-10 lg:grid-cols-2 lg:p-14">
        <span
          aria-hidden
          className="animate-blob-drift absolute -top-24 -left-20 size-72 rounded-full bg-white/10 blur-3xl"
        />
        <span
          aria-hidden
          className="animate-blob-drift absolute -right-24 -bottom-28 size-80 rounded-full bg-white/10 blur-3xl [animation-delay:-7s]"
        />
        <div className="relative text-white">
          <span className="inline-flex items-center gap-2 rounded-full bg-white/15 px-3 py-1 text-xs font-semibold ring-1 ring-white/25 ring-inset">
            <Heart className="size-3.5 animate-pulse fill-current" />
            Apoie o projeto
          </span>
          <h2 className="mt-4 text-3xl font-extrabold tracking-tight sm:text-4xl">
            Ajude a manter a Bondade no ar
          </h2>
          <p className="mt-4 text-base leading-relaxed text-white/85">
            A Bondade é gratuita para os pontos de coleta e para quem precisa de ajuda. Servidores,
            armazenamento e manutenção têm custo — se o projeto faz sentido para você, qualquer
            valor via Pix ajuda a mantê-lo funcionando.
          </p>
        </div>

        <div className="animate-float-soft relative mx-auto w-full max-w-sm rounded-3xl bg-white p-6 text-center shadow-xl">
          <div className="mx-auto w-fit rounded-2xl bg-stone-50 p-4 ring-1 ring-stone-200">
            <QRCodeSVG
              value={PIX_COPY_AND_PASTE_CODE}
              size={208}
              marginSize={0}
              title="QR code Pix para apoiar o projeto"
            />
          </div>
          <p className="mt-4 text-sm font-semibold text-stone-900">{PIX_RECEIVER_NAME}</p>
          <p className="mt-0.5 text-xs text-stone-500">
            Chave Pix: <span className="font-medium text-stone-700">{PIX_KEY}</span>
          </p>
          <Button
            className="mt-5 w-full"
            size="lg"
            leftIcon={copied ? <Check className="size-4" /> : <Copy className="size-4" />}
            onClick={() => copy(PIX_COPY_AND_PASTE_CODE)}
          >
            {copied ? 'Código copiado!' : 'Copiar código Pix'}
          </Button>
          <p className="mt-3 text-[11px] text-stone-400">
            Abra o app do seu banco, escolha Pix e leia o QR code ou use o Pix copia e cola.
          </p>
        </div>
      </Reveal>
    </section>
  )
}
