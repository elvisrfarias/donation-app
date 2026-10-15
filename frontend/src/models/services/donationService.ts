import { http } from '@/models/api/httpClient'
import type { Donation, PledgeDonationInput, ReceiveDonationInput } from '@/models/types'

export const donationService = {
  /** Avisa um ponto de que alguém pretende doar (anônimo). */
  async pledge(pointId: string, input: PledgeDonationInput): Promise<Donation> {
    const { data } = await http.post<Donation>(
      `/public/collection-points/${pointId}/donations`,
      input,
    )
    return data
  },

  /** Doações do ponto, das mais recentes às mais antigas (gestão). */
  async listByPoint(pointId: string, signal?: AbortSignal): Promise<Donation[]> {
    const { data } = await http.get<Donation[]>(`/collection-points/${pointId}/donations`, {
      signal,
    })
    return data
  },

  /** Confirma o recebimento e, se informado, soma os itens ao estoque (gestão). */
  async receive(
    pointId: string,
    donationId: string,
    input: ReceiveDonationInput,
  ): Promise<Donation> {
    const { data } = await http.post<Donation>(
      `/collection-points/${pointId}/donations/${donationId}/receive`,
      input,
    )
    return data
  },

  /** Cancela uma doação prometida (gestão). */
  async cancel(pointId: string, donationId: string): Promise<Donation> {
    const { data } = await http.post<Donation>(
      `/collection-points/${pointId}/donations/${donationId}/cancel`,
    )
    return data
  },
}
