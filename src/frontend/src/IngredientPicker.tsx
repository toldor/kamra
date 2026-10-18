import { useId, type ReactNode } from 'react'
import type { Ingredient } from './api/client'
import type { FieldProps } from './fieldErrors'

// The ingredient field of the pantry, shopping list and minimum level forms: free text with the list's
// names offered (the choice: findIngredient). Each picker has its own option list, so several can share a page.
export function IngredientPicker({ ingredients, value, onChange, field, error }: {
  ingredients: Ingredient[]
  value: string
  onChange: (value: string) => void
  field: FieldProps
  error: ReactNode
}) {
  const listId = useId()
  return (
    <>
      <label htmlFor={field.id}>Hozzávaló</label>
      <input {...field} list={listId} autoComplete="off" value={value} onChange={(e) => onChange(e.target.value)} />
      <datalist id={listId}>
        {ingredients.map((i) => <option key={i.id} value={i.name} />)}
      </datalist>
      {error}
    </>
  )
}
