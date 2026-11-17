import type { RecipeIdea, Unit } from './api/client'

// The draft of the Új recept form (M14): it survives a reload or signing in again in this tab, and it is forgotten
// after saving or Mégse. aiSuggested: filled from a recipe idea of the chat (receptotlet_minispec 4.-6.).
export type Row = { key: string; ingredient: string; amount: string; unit: Unit }
export type Draft = { name: string; servings: string; instructions: string; rows: Row[]; aiSuggested?: boolean }

const draftKey = 'kamra:recipe-draft'
export const newRow = (): Row => ({ key: crypto.randomUUID(), ingredient: '', amount: '', unit: 'g' })
export const newDraft = (): Draft => ({ name: '', servings: '4', instructions: '', rows: [newRow()] })

// Storage may be unavailable (private window, blocked site data); the form then works without a draft.
export function readDraft(): Draft | null {
  try {
    const stored = sessionStorage.getItem(draftKey)
    return stored ? (JSON.parse(stored) as Draft) : null
  } catch {
    return null
  }
}

export function storeDraft(draft: Draft | null) {
  try {
    if (draft) sessionStorage.setItem(draftKey, JSON.stringify(draft))
    else sessionStorage.removeItem(draftKey)
  } catch {
    // Without storage there is no draft.
  }
}

// receptotlet_minispec 4.-6.: the chat's recipe idea becomes the draft of the form, marked as AI-suggested.
export function storeRecipeIdea(idea: RecipeIdea) {
  storeDraft({
    name: idea.name,
    servings: String(idea.servings),
    instructions: idea.instructions,
    rows: idea.ingredients.map((i) => ({ ...newRow(), ingredient: i.name, amount: String(i.amount), unit: i.unit })),
    aiSuggested: true,
  })
}
