const HEART_PATH =
  'M24 35s-9.2-5.6-12-11.4c-1.9-3.9.2-8.4 4.3-9.3 2.6-.6 5.2.5 6.7 2.6 1.5-2.1 4.1-3.2 6.7-2.6 4.1.9 6.2 5.4 4.3 9.3C33.2 29.4 24 35 24 35Z'

function readBrandColor(): string {
  return getComputedStyle(document.documentElement).getPropertyValue('--brand').trim()
}

function buildFaviconSvg(brandColor: string): string {
  return (
    '<svg xmlns="http://www.w3.org/2000/svg" width="48" height="48" viewBox="0 0 48 48">' +
    `<rect width="48" height="48" rx="13" fill="${brandColor}"/>` +
    `<path d="${HEART_PATH}" fill="#fff"/>` +
    '</svg>'
  )
}

function toDataUri(svg: string): string {
  return `data:image/svg+xml,${encodeURIComponent(svg)}`
}

/** Pinta o favicon e o theme-color com a cor primária (`--brand`), respeitando o white-label em runtime. */
export function applyBrandFavicon(): void {
  const brandColor = readBrandColor()
  if (!brandColor) return

  document
    .querySelector<HTMLLinkElement>('link[rel="icon"]')
    ?.setAttribute('href', toDataUri(buildFaviconSvg(brandColor)))
  document
    .querySelector<HTMLMetaElement>('meta[name="theme-color"]')
    ?.setAttribute('content', brandColor)
}
