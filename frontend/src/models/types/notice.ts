export type NoticeAudience = 'Public' | 'Managers'

/** Aviso do mural de um ponto de coleta (espelha NoticeResponse do backend). */
export interface Notice {
  id: string
  collectionPointId: string
  pointName?: string | null
  title: string
  message: string
  audience: NoticeAudience
  authorName: string
  createdAtUtc: string
}

export interface PublishNoticeInput {
  title: string
  message: string
  audience: NoticeAudience
}

export const noticeAudienceLabels: Record<NoticeAudience, string> = {
  Public: 'Comunidade',
  Managers: 'Só gestores',
}

export const MAX_NOTICE_TITLE_LENGTH = 120
export const MAX_NOTICE_MESSAGE_LENGTH = 1000
