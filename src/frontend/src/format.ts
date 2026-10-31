import type { Amount, Category, Dimension, Ingredient, Shortage, Unit } from './api/client'

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

// A moment (e.g. when a suggestion was made) as its Budapest calendar day, like the backend's "today".
const dayFormat = new Intl.DateTimeFormat('hu-HU', { month: 'short', day: 'numeric', timeZone: 'Europe/Budapest' })

export const formatDay = (isoDateTime: string) => dayFormat.format(new Date(isoDateTime))

// A shopping list line: "tejföl 50 g", or just the name without an amount.
export const formatListLine = (name: string, amount: Amount | null) =>
  amount ? `${name} ${formatAmount(amount.value, amount.unit)}` : name

// US-3: "tejföl: 50 g hiányzik (150 g van, 200 g kell)", in the recipe's unit.
export const formatShortage = ({ ingredientName, missing, available, required, unit }: Shortage) =>
  `${ingredientName}: ${formatAmount(missing, unit)} hiányzik (${formatAmount(available, unit)} van, ${formatAmount(required, unit)} kell)`

// Like the backend's Ingredient.Normalize: case and extra spaces do not matter.
const normalize = (name: string) => name.trim().replace(/\s+/g, ' ').toLowerCase()

// ADR-0003: only an ingredient from the list can be chosen; undefined while the text matches none.
export const findIngredient = (ingredients: Ingredient[], name: string) =>
  ingredients.find((i) => normalize(i.name) === normalize(name))

// The Receptek and Bevásárlólista search: the query anywhere in one of the texts, by the same rule as
// the backend's pantry search (case and extra spaces do not matter, accents do).
export const matchesSearch = (query: string, ...texts: string[]) => texts.some((t) => normalize(t).includes(normalize(query)))
