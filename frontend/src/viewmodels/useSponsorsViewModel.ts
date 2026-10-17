import { useQueryClient } from '@tanstack/react-query'
import { useApiList, useApiMutation } from '@/lib/apiHooks'
import { queryKeys } from '@/lib/queryClient'
import { sponsorService } from '@/models/services'
import type { Sponsor } from '@/models/types'
import type { CreateSponsorForm, UpdateSponsorForm } from './schemas'

function websiteOrNull(websiteUrl: string | undefined): string | null {
  return websiteUrl?.trim() || null
}

function useInvalidateSponsors() {
  const queryClient = useQueryClient()
  return () => {
    queryClient.invalidateQueries({ queryKey: queryKeys.sponsors.all })
    queryClient.invalidateQueries({ queryKey: queryKeys.sponsors.public })
  }
}

/** Patrocinadores ativos da página pública. */
export function usePublicSponsors() {
  const { items, ...rest } = useApiList<Sponsor>({
    queryKey: queryKeys.sponsors.public,
    queryFn: ({ signal }) => sponsorService.listPublic(signal),
  })

  return { sponsors: items, ...rest }
}

/** Todos os patrocinadores, para a tela de gestão (Admin). */
export function useSponsors() {
  const { items, ...rest } = useApiList<Sponsor>({
    queryKey: queryKeys.sponsors.all,
    queryFn: ({ signal }) => sponsorService.list(signal),
  })

  return { sponsors: items, ...rest }
}

/** Cadastro de patrocinador: envia a logo e cria o patrocinador com ela (Admin). */
export function useCreateSponsor() {
  const invalidateSponsors = useInvalidateSponsors()

  const { mutate, ...rest } = useApiMutation<Sponsor, CreateSponsorForm>({
    mutationFn: async ({ logo, websiteUrl, ...fields }) => {
      const uploaded = await sponsorService.uploadLogo(logo)
      return sponsorService.create({
        ...fields,
        websiteUrl: websiteOrNull(websiteUrl),
        logoFileId: uploaded.id,
      })
    },
    onSuccess: invalidateSponsors,
  })

  return { create: mutate, ...rest }
}

/** Edição de patrocinador; a logo só é reenviada se uma nova for escolhida (Admin). */
export function useUpdateSponsor() {
  const invalidateSponsors = useInvalidateSponsors()

  const { mutate, ...rest } = useApiMutation<Sponsor, { id: string; values: UpdateSponsorForm }>({
    mutationFn: async ({ id, values: { logo, websiteUrl, ...fields } }) => {
      const logoFileId = logo ? (await sponsorService.uploadLogo(logo)).id : undefined
      return sponsorService.update(id, {
        ...fields,
        websiteUrl: websiteOrNull(websiteUrl),
        logoFileId,
      })
    },
    onSuccess: invalidateSponsors,
  })

  return { update: mutate, ...rest }
}

/** Remoção definitiva de patrocinador (Admin). */
export function useDeleteSponsor() {
  const invalidateSponsors = useInvalidateSponsors()

  const { mutate, ...rest } = useApiMutation<void, string>({
    mutationFn: (id) => sponsorService.remove(id),
    onSuccess: invalidateSponsors,
  })

  return { remove: mutate, ...rest }
}
