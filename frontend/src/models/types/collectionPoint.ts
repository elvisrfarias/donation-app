import type { Cycle, CycleType } from './beneficiary'

export type CollectionPointType = 'Association' | 'Church' | 'PublicEntity'

/** Categoria dinâmica do catálogo do ponto (agrupa estoque, checklist de retirada e itens
 *  aceitos no mapa público). CategoryResponse do backend. Ponto novo nasce sem nenhuma. */
export interface Category {
  id: string
  name: string
  icon: string
}

/** Corpo do CreateCategoryCommand. */
export interface CreateCategoryInput {
  name: string
  icon: string
}

/** Corpo do UpdateCategoryCommand. */
export interface UpdateCategoryInput {
  name: string
  icon: string
}

export interface Address {
  street: string
  number: string
  complement?: string | null
  district: string
  city: string
  state: string
  zipCode: string
  /** Coordenadas fixadas no mapa; nulas nos pontos ainda não geolocalizados. */
  latitude?: number | null
  longitude?: number | null
}

export interface Contact {
  responsibleName: string
  email: string
  phone: string
}

/** Classificação de estoque de um item, computada pelo backend (ver `StatusOf` em `CollectionPointResponse`). */
export type StockItemStatus = 'out' | 'low' | 'goal' | 'ok'

/** Um item granular do catálogo de estoque de um ponto (ex.: "Arroz 1kg"). StockItemResponse do backend.
 *  `categoryName`/`categoryIcon` já vêm resolvidos pelo backend a partir do catálogo de categorias do
 *  próprio ponto — não é preciso cruzar com `CollectionPoint.categories` para renderizar. */
export interface StockItem {
  id: string
  name: string
  unit: string
  categoryId: string
  categoryName: string
  categoryIcon: string
  quantity: number
  /** Meta desejada (0 = sem meta). */
  goal: number
  /** Quantidade a partir da qual (ou abaixo) o item é considerado com estoque baixo. */
  alertThreshold: number
  /** Se o item é acompanhado pelo ponto (aparece na aba Estoque e conta nos indicadores). */
  isTracked: boolean
  /** Já vem calculado pelo backend — não recompute no front. */
  status: StockItemStatus
}

/** Corpo do CreateStockItemCommand. */
export interface CreateStockItemInput {
  name: string
  unit: string
  categoryId: string
  initialQuantity: number
  goal: number
  alertThreshold: number
  isTracked: boolean
}

/** Corpo do UpdateStockItemCommand (não altera a quantidade). */
export interface UpdateStockItemInput {
  name: string
  unit: string
  categoryId: string
  goal: number
  alertThreshold: number
  isTracked: boolean
}

/** Uma linha do UpdateStockQuantitiesCommand: item e nova quantidade disponível. */
export interface StockQuantityEntry {
  stockItemId: string
  quantity: number
}

/** Corpo do UpdateStockQuantitiesCommand (aba "Estoque"). */
export interface UpdateStockQuantitiesInput {
  items: StockQuantityEntry[]
}

/** Uma linha do UpdateStockItemSettingsCommand: meta, aviso e acompanhamento de um item. */
export interface StockItemSettingEntry {
  stockItemId: string
  goal: number
  alertThreshold: number
  isTracked: boolean
}

/** Corpo do UpdateStockItemSettingsCommand (aba "Metas e avisos"). */
export interface UpdateStockItemSettingsInput {
  items: StockItemSettingEntry[]
}

/** Corpo do UpdateWithdrawalRuleCommand: ciclo + máximo de cestas por beneficiário em cada ciclo. */
export interface UpdateWithdrawalRuleInput {
  cycleType: CycleType
  maxWithdrawalsPerCycle: number
}

/** Um item de uma receita de cesta básica: item de estoque do ponto e quantidade necessária por cesta. */
export interface BasketTemplateItem {
  stockItemId: string
  itemName: string
  quantity: number
}

/** Um item faltante para completar mais uma cesta a partir de uma receita. */
export interface BasketItemGap {
  itemId: string
  itemName: string
  requiredPerBasket: number
  currentQuantity: number
  neededForNextBasket: number
}

/** Quantas cestas completas o estoque atual permite montar a partir de uma receita, e o que falta. */
export interface BasketFeasibility {
  completeBaskets: number
  missing: BasketItemGap[]
}

/** Uma receita de cesta básica de um ponto. BasketTemplateResponse do backend. */
export interface BasketTemplate {
  id: string
  name: string
  isActive: boolean
  items: BasketTemplateItem[]
  feasibility: BasketFeasibility
}

/** Um item do corpo de Create/UpdateBasketTemplateCommand. */
export interface BasketTemplateItemInput {
  stockItemId: string
  quantity: number
}

/** Corpo do CreateBasketTemplateCommand. */
export interface CreateBasketTemplateInput {
  name: string
  items: BasketTemplateItemInput[]
}

/** Corpo do UpdateBasketTemplateCommand. */
export interface UpdateBasketTemplateInput {
  name: string
  items: BasketTemplateItemInput[]
  isActive: boolean
}

/** CollectionPointResponse do backend. */
export interface CollectionPoint {
  id: string
  name: string
  type: CollectionPointType
  /** Texto livre que apresenta a iniciativa/projeto (opcional). */
  description?: string | null
  /** Horário de funcionamento em texto livre (ex.: "Seg a Sex, 8h–17h"). */
  openingHours?: string | null
  /** Itens que o ponto precisa com urgência agora. */
  needs: string[]
  /** Catálogo de estoque granular do ponto. Só vem nas consultas de gestão (vazio na lista pública). */
  stockItems: StockItem[]
  /** Receitas de cesta básica do ponto. Só vem nas consultas de gestão (vazio na lista pública). */
  basketTemplates: BasketTemplate[]
  /** Regra de retirada do ponto: ciclo + máximo por família em cada ciclo. */
  withdrawalRule: Cycle
  address: Address
  contact: Contact
  isActive: boolean
  createdAtUtc: string
  /** Catálogo de categorias do ponto (sempre preenchido, em toda visibilidade) — é o que aparece
   *  como "o que este ponto aceita" no mapa público, e agrupa o estoque e o checklist de retirada. */
  categories: Category[]
  /** URL (temporária) da foto do ponto. Nulo nos pontos antigos/seed — a UI cai no ícone. */
  imageUrl?: string | null
  /** Média das notas (0 quando ainda não há avaliações). */
  averageRating: number
  /** Total de avaliações registradas pela comunidade. */
  reviewCount: number
  /** Nº de beneficiários distintos já atendidos no ponto (indicador de impacto). */
  beneficiariesServed: number
}

export type DashboardGranularity = 'Day' | 'Month'

/** Retiradas de um dia ou mês; `date` é o primeiro dia do intervalo (yyyy-MM-dd). */
export interface WithdrawalsSeriesPoint {
  date: string
  count: number
}

/** Item retirado numa visita, já resolvido (nome/unidade/categoria) pelo backend — mesmo formato
 *  usado no histórico de retiradas do beneficiário (`WithdrawalItemResponse` do backend). */
export interface WithdrawalItemRef {
  stockItemId: string
  itemName: string
  unit: string
  categoryId: string
  categoryName: string
  categoryIcon: string
  quantity: number
}

export interface RecentWithdrawal {
  withdrawalId: string
  beneficiaryName: string
  items: WithdrawalItemRef[]
  occurredAtUtc: string
}

/** Indicadores de resultado de um único ponto em um período (painel do gestor). */
export interface CollectionPointDashboard {
  pointId: string
  periodStart: string
  periodEnd: string
  withdrawalsInPeriod: number
  beneficiariesServedInPeriod: number
  totalWithdrawals: number
  registeredBeneficiaries: number
  granularity: DashboardGranularity
  withdrawalsSeries: WithdrawalsSeriesPoint[]
  recentWithdrawals: RecentWithdrawal[]
}

/** Corpo do CreateCollectionPointCommand. */
export interface CreateCollectionPointInput {
  name: string
  type: CollectionPointType
  description?: string | null
  openingHours?: string | null
  needs?: string[]
  street: string
  number: string
  complement?: string | null
  district: string
  city: string
  state: string
  zipCode: string
  responsibleName: string
  email: string
  phone: string
  /** Id do arquivo de imagem já enviado (POST /api/files). Obrigatório. */
  imageFileId: string
  /** Coordenadas exatas do ponto no mapa (opcionais). */
  latitude?: number | null
  longitude?: number | null
}

/** Corpo do UpdateCollectionPointCommand. A imagem é opcional: envie `imageFileId`
 * só quando quiser trocar a foto; ausente/nulo mantém a atual. */
export interface UpdateCollectionPointInput {
  name: string
  type: CollectionPointType
  description?: string | null
  openingHours?: string | null
  needs?: string[]
  street: string
  number: string
  complement?: string | null
  district: string
  city: string
  state: string
  zipCode: string
  responsibleName: string
  email: string
  phone: string
  /** Id da nova imagem (POST /api/files). Omitido mantém a imagem atual. */
  imageFileId?: string | null
  /** Coordenadas exatas do ponto no mapa (opcionais). */
  latitude?: number | null
  longitude?: number | null
}

/** Metadados de um arquivo enviado ao storage (subconjunto do StoredFileResponse). */
export interface UploadedImage {
  id: string
  url?: string | null
}

export const collectionPointTypeLabels: Record<CollectionPointType, string> = {
  Association: 'Associação',
  Church: 'Igreja',
  PublicEntity: 'Entidade Pública',
}

/** Monta a string de endereço usada para geocodificação e exibição. */
export function formatAddress(address: Address): string {
  const parts = [
    `${address.street}, ${address.number}`,
    address.district,
    `${address.city} - ${address.state}`,
    address.zipCode,
  ]
  return parts.filter(Boolean).join(', ')
}
