import type { Category, Dimension, Unit } from './api/client'

// Display helpers for the pantry; the names follow data_model.md (Kategórialista).
const categoryNames: Record<Category, string> = {
  dairy: 'Tejtermék',
  cheese: 'Sajt',
  eggs: 'Tojás',
  freshMeatFish: 'Friss hús, baromfi, hal',
  processedMeat: 'Felvágott, húskészítmény',
  vegetables: 'Zöldség',
  fruit: 'Gyümölcs',
  bakery: 'Pékáru',
  dryGoods: 'Száraz élelmiszer',
  cannedAndSauces: 'Konzerv, befőtt, szósz',
  frozen: 'Fagyasztott',
  beverages: 'Ital',
  preparedFood: 'Készétel, maradék',
  other: 'Egyéb',
}

export const categoryName = (category: Category) => categoryNames[category]

// ADR-0002: the units of each dimension and their factor to the base unit (g, ml, db).
export const unitsOf: Record<Dimension, Unit[]> = { mass: ['g', 'dkg', 'kg'], volume: ['ml', 'dl', 'l'], count: ['db'] }

const factors: Record<Unit, number> = { g: 1, dkg: 10, kg: 1000, ml: 1, dl: 100, l: 1000, db: 1 }

export const toBase = (amount: number, unit: Unit) => amount * factors[unit]

const numberFormat = new Intl.NumberFormat('hu-HU', { maximumFractionDigits: 3 })

export const formatAmount = (amount: number, unit: Unit) => `${numberFormat.format(amount)} ${unit}`

// Dates arrive as yyyy-mm-dd calendar days; formatting them in UTC keeps the day unchanged.
const dateFormat = new Intl.DateTimeFormat('hu-HU', { month: 'short', day: 'numeric', timeZone: 'UTC' })

export const formatDate = (isoDate: string) => dateFormat.format(new Date(`${isoDate}T00:00:00Z`))
