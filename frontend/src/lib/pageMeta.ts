import { useEffect } from 'react'

export const DEFAULT_PAGE_TITLE = 'Dooa — Pontos de Doação e Coleta'
export const DEFAULT_PAGE_DESCRIPTION =
  'Encontre pontos de doação e coleta perto de você e acompanhe retiradas com equidade entre beneficiários no seu município.'

const INDEXABLE_ROBOTS = 'index, follow, max-image-preview:large'
const NOT_INDEXABLE_ROBOTS = 'noindex, nofollow'

interface PageMeta {
  title: string
  description?: string
  /** Caminho canônico da página (ex.: "/pontos/…"); sem ele, o canonical atual é mantido. */
  canonicalPath?: string
  indexable?: boolean
}

function setMetaContent(name: string, content: string) {
  document.head.querySelector(`meta[name="${name}"]`)?.setAttribute('content', content)
}

function setCanonicalUrl(href: string) {
  let link = document.head.querySelector<HTMLLinkElement>('link[rel="canonical"]')
  if (!link) {
    link = document.createElement('link')
    link.rel = 'canonical'
    document.head.appendChild(link)
  }
  link.href = href
}

/**
 * Mantém título, descrição, canonical e robots coerentes com a página aberta. O HTML inicial já vem com eles
 * do servidor; este hook cobre a navegação dentro do app, que o Google também renderiza.
 */
export function usePageMeta({
  title,
  description = DEFAULT_PAGE_DESCRIPTION,
  canonicalPath,
  indexable = true,
}: PageMeta) {
  useEffect(() => {
    document.title = title
    setMetaContent('description', description)
    setMetaContent('robots', indexable ? INDEXABLE_ROBOTS : NOT_INDEXABLE_ROBOTS)
    if (canonicalPath) setCanonicalUrl(new URL(canonicalPath, window.location.origin).href)
  }, [title, description, canonicalPath, indexable])
}
