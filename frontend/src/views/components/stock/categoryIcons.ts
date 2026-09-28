import {
  Apple,
  Armchair,
  Baby,
  Bed,
  BookOpen,
  Brush,
  Car,
  Cross,
  Dumbbell,
  Footprints,
  Gamepad2,
  Gift,
  GraduationCap,
  Heart,
  Home,
  Lamp,
  Laptop,
  Music,
  Package,
  Palette,
  Pill,
  Puzzle,
  Refrigerator,
  Scissors,
  Shirt,
  ShoppingBag,
  Sparkles,
  SprayCan,
  Umbrella,
  Utensils,
  Wrench,
  type LucideIcon,
} from 'lucide-react'

/** Chave usada pelo backend quando a categoria original de um item de retirada foi removida. */
export const REMOVED_CATEGORY_ICON_KEY = 'help-circle'

/** Ícone genérico usado quando a chave guardada não é reconhecida (categoria removida ou dado antigo). */
export const FALLBACK_ICON: LucideIcon = Package

export interface IconPaletteEntry {
  key: string
  label: string
  Icon: LucideIcon
}

/** Paleta fixa de ícones oferecida ao gestor ao criar/editar uma categoria. O backend só guarda a
 *  chave (string opaca) — não valida contra esta lista, então uma chave desconhecida (categoria
 *  antiga, dado de outro ambiente) cai no ícone genérico via `iconFor`. */
export const ICON_PALETTE: IconPaletteEntry[] = [
  { key: 'apple', label: 'Alimentos', Icon: Apple },
  { key: 'utensils', label: 'Cozinha', Icon: Utensils },
  { key: 'shirt', label: 'Roupas', Icon: Shirt },
  { key: 'footprints', label: 'Calçados', Icon: Footprints },
  { key: 'spray-can', label: 'Higiene', Icon: SprayCan },
  { key: 'brush', label: 'Limpeza', Icon: Brush },
  { key: 'armchair', label: 'Móveis', Icon: Armchair },
  { key: 'refrigerator', label: 'Eletrodomésticos', Icon: Refrigerator },
  { key: 'puzzle', label: 'Brinquedos', Icon: Puzzle },
  { key: 'book-open', label: 'Livros', Icon: BookOpen },
  { key: 'pill', label: 'Medicamentos', Icon: Pill },
  { key: 'cross', label: 'Saúde', Icon: Cross },
  { key: 'baby', label: 'Bebês', Icon: Baby },
  { key: 'graduation-cap', label: 'Educação', Icon: GraduationCap },
  { key: 'gamepad', label: 'Jogos', Icon: Gamepad2 },
  { key: 'laptop', label: 'Eletrônicos', Icon: Laptop },
  { key: 'bed', label: 'Cama e banho', Icon: Bed },
  { key: 'lamp', label: 'Iluminação', Icon: Lamp },
  { key: 'wrench', label: 'Ferramentas', Icon: Wrench },
  { key: 'car', label: 'Automotivo', Icon: Car },
  { key: 'shopping-bag', label: 'Diversos', Icon: ShoppingBag },
  { key: 'palette', label: 'Artesanato', Icon: Palette },
  { key: 'music', label: 'Música', Icon: Music },
  { key: 'dumbbell', label: 'Esporte', Icon: Dumbbell },
  { key: 'scissors', label: 'Beleza', Icon: Scissors },
  { key: 'umbrella', label: 'Proteção', Icon: Umbrella },
  { key: 'heart', label: 'Doação', Icon: Heart },
  { key: 'gift', label: 'Presentes', Icon: Gift },
  { key: 'sparkles', label: 'Especiais', Icon: Sparkles },
  { key: 'home', label: 'Casa', Icon: Home },
  { key: 'package', label: 'Outros', Icon: Package },
]

/** Lookup direto chave → ícone (objeto simples, não `Map`) para uso em JSX — indexar um objeto
 *  estático é a forma que o linter de componentes reconhece como estável entre renders; ver
 *  `iconFor` para o mesmo lookup fora de um contexto de render. */
export const ICON_BY_KEY: Record<string, LucideIcon> = Object.fromEntries(
  ICON_PALETTE.map((entry) => [entry.key, entry.Icon]),
)

/** Resolve o ícone de uma categoria pela chave guardada. Cai no ícone genérico para chaves
 *  desconhecidas — inclusive `help-circle`, que o backend usa como marcador de categoria removida
 *  (ver `WithdrawalMappings.RemovedCategoryIcon`). Fora de JSX apenas — dentro de JSX prefira
 *  `ICON_BY_KEY[key] ?? FALLBACK_ICON` diretamente (ver nota acima). */
export function iconFor(key: string): LucideIcon {
  return ICON_BY_KEY[key] ?? FALLBACK_ICON
}
