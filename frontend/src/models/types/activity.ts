export type ActivityKind = 'Donation' | 'Notice' | 'Review' | 'LowStock'

/** Acontecimento recente de um ponto (espelha ActivityItemResponse do backend). */
export interface ActivityItem {
  kind: ActivityKind
  title: string
  description: string
  occurredAtUtc: string
}

export interface ActivityFeed {
  items: ActivityItem[]
}
