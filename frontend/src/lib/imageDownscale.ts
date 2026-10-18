const WEBP_TYPE = 'image/webp'
const WEBP_QUALITY = 0.85
const ANIMATED_TYPES = ['image/gif']

/** Lado maior das imagens enviadas: suficiente para telas de alta densidade nos tamanhos em que aparecem. */
export const PHOTO_MAX_DIMENSION = 1600
export const LOGO_MAX_DIMENSION = 800

/** Novas dimensões para caber em `maxDimension` mantendo a proporção (nunca amplia). */
export function fitWithin(
  width: number,
  height: number,
  maxDimension: number,
): { width: number; height: number } {
  const scale = Math.min(1, maxDimension / Math.max(width, height))
  return { width: Math.round(width * scale), height: Math.round(height * scale) }
}

function webpFileName(name: string): string {
  return `${name.replace(/\.[^.]+$/, '') || 'imagem'}.webp`
}

function canDownscaleInBrowser(): boolean {
  return typeof createImageBitmap === 'function' && typeof OffscreenCanvas === 'function'
}

/**
 * Reduz e converte para WebP antes do envio, no próprio navegador: uma foto de celular de vários MB vira
 * algumas centenas de KB e a página do ponto carrega muito mais rápido. GIFs (que podem ser animados), navegadores
 * sem suporte e casos em que o resultado ficaria maior que o original seguem com o arquivo original.
 */
export async function downscaleImage(file: File, maxDimension: number): Promise<File> {
  if (ANIMATED_TYPES.includes(file.type) || !canDownscaleInBrowser()) return file

  try {
    const bitmap = await createImageBitmap(file)
    const size = fitWithin(bitmap.width, bitmap.height, maxDimension)
    const canvas = new OffscreenCanvas(size.width, size.height)
    canvas.getContext('2d')?.drawImage(bitmap, 0, 0, size.width, size.height)
    bitmap.close()

    const blob = await canvas.convertToBlob({ type: WEBP_TYPE, quality: WEBP_QUALITY })
    if (blob.type !== WEBP_TYPE || blob.size >= file.size) return file

    return new File([blob], webpFileName(file.name), { type: WEBP_TYPE })
  } catch {
    return file
  }
}
