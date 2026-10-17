import { http } from '@/models/api/httpClient'
import type { CreateSponsorInput, Sponsor, UpdateSponsorInput, UploadedImage } from '@/models/types'

export const sponsorService = {
  /** Patrocinadores ativos para a página pública, do Diamante à Prata (anônimo). */
  async listPublic(signal?: AbortSignal): Promise<Sponsor[]> {
    const { data } = await http.get<Sponsor[]>('/public/sponsors', { signal })
    return data
  },

  /** Todos os patrocinadores, ativos e inativos (requer role Admin). */
  async list(signal?: AbortSignal): Promise<Sponsor[]> {
    const { data } = await http.get<Sponsor[]>('/sponsors', { signal })
    return data
  },

  async create(input: CreateSponsorInput): Promise<Sponsor> {
    const { data } = await http.post<Sponsor>('/sponsors', input)
    return data
  },

  async update(id: string, input: UpdateSponsorInput): Promise<Sponsor> {
    const { data } = await http.put<Sponsor>(`/sponsors/${id}`, input)
    return data
  },

  async remove(id: string): Promise<void> {
    await http.delete(`/sponsors/${id}`)
  },

  /** Envia a logo como arquivo público; o id devolvido vai no cadastro/edição do patrocinador. */
  async uploadLogo(file: File): Promise<UploadedImage> {
    const form = new FormData()
    form.append('file', file)
    form.append('visibility', 'Public')
    form.append('ownerType', 'Sponsor')
    const { data } = await http.post<UploadedImage>('/files', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    return data
  },
}
