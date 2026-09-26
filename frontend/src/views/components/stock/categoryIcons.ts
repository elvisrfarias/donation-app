import {
  Apple,
  Armchair,
  BookOpen,
  Brush,
  Footprints,
  Package,
  Pill,
  Puzzle,
  Refrigerator,
  Shirt,
  SprayCan,
  type LucideIcon,
} from 'lucide-react'
import type { ItemCategory } from '@/models/types'

export const categoryIcons: Record<ItemCategory, LucideIcon> = {
  Food: Apple,
  Clothing: Shirt,
  Footwear: Footprints,
  Hygiene: SprayCan,
  Cleaning: Brush,
  Furniture: Armchair,
  Appliances: Refrigerator,
  Toys: Puzzle,
  Books: BookOpen,
  Medicine: Pill,
  Other: Package,
}
