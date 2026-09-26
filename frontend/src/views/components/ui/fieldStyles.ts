export const inputBase =
  'w-full rounded-xl border bg-white px-3.5 text-sm text-stone-900 transition-all duration-150 placeholder:text-stone-400 focus:outline-none disabled:bg-stone-50 disabled:text-stone-400'

export function stateRing(invalid?: boolean): string {
  return invalid
    ? 'border-red-300 focus:border-red-500 focus:ring-4 focus:ring-red-500/10'
    : 'border-stone-200 hover:border-stone-300 focus:border-brand-500 focus:ring-4 focus:ring-brand-500/10'
}
