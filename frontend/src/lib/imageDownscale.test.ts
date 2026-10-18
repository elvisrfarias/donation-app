import { afterEach, describe, expect, it, vi } from 'vitest'
import { downscaleImage, fitWithin } from './imageDownscale'

afterEach(() => {
  vi.unstubAllGlobals()
})

function stubCanvas(resultSize: number, resultType = 'image/webp') {
  const drawImage = vi.fn()
  vi.stubGlobal(
    'createImageBitmap',
    vi.fn(async () => ({ width: 3840, height: 1651, close: vi.fn() })),
  )
  vi.stubGlobal(
    'OffscreenCanvas',
    class {
      width: number
      height: number

      constructor(width: number, height: number) {
        this.width = width
        this.height = height
      }

      getContext() {
        return { drawImage }
      }
      async convertToBlob() {
        return new Blob([new Uint8Array(resultSize)], { type: resultType })
      }
    },
  )
  return drawImage
}

describe('fitWithin', () => {
  it('reduz pelo lado maior mantendo a proporção', () => {
    expect(fitWithin(3840, 1651, 1600)).toEqual({ width: 1600, height: 688 })
    expect(fitWithin(1000, 3000, 800)).toEqual({ width: 267, height: 800 })
  })

  it('nunca amplia imagens menores', () => {
    expect(fitWithin(640, 480, 1600)).toEqual({ width: 640, height: 480 })
  })
})

describe('downscaleImage', () => {
  it('converte a foto grande em WebP no tamanho máximo', async () => {
    const drawImage = stubCanvas(200_000)
    const original = new File([new Uint8Array(2_383_328)], 'foto.jpg', { type: 'image/jpeg' })

    const result = await downscaleImage(original, 1600)

    expect(result.type).toBe('image/webp')
    expect(result.name).toBe('foto.webp')
    expect(result.size).toBe(200_000)
    expect(drawImage).toHaveBeenCalledWith(expect.anything(), 0, 0, 1600, 688)
  })

  it('mantém o original quando a conversão não diminui o arquivo', async () => {
    stubCanvas(5_000)
    const original = new File([new Uint8Array(4_000)], 'logo.png', { type: 'image/png' })

    expect(await downscaleImage(original, 800)).toBe(original)
  })

  it('mantém GIFs, que podem ser animados', async () => {
    stubCanvas(100)
    const gif = new File([new Uint8Array(50_000)], 'anim.gif', { type: 'image/gif' })

    expect(await downscaleImage(gif, 800)).toBe(gif)
  })

  it('mantém o original em navegadores sem suporte', async () => {
    const original = new File([new Uint8Array(10)], 'foto.jpg', { type: 'image/jpeg' })

    expect(await downscaleImage(original, 1600)).toBe(original)
  })

  it('mantém o original quando o navegador não gera WebP', async () => {
    stubCanvas(100, 'image/png')
    const original = new File([new Uint8Array(50_000)], 'foto.jpg', { type: 'image/jpeg' })

    expect(await downscaleImage(original, 1600)).toBe(original)
  })
})
