import { useEffect, useId, useRef, useState, type ReactNode } from 'react'
import { api, failWith, type Category, type CategoryInfo, type Dimension, type Ingredient } from './api/client'
import type { FieldProps } from './fieldErrors'
import { categoryName, findIngredient } from './format'

// N11: what the picker needs to offer "Új hozzávaló: név" (the pantry and the recipe forms only).
export type IngredientCreation = {
  categories: CategoryInfo[]
  // After the 201: the parent reloads its ingredients, so the typed name matches the new one.
  onCreated: (ingredient: Ingredient) => void
  onSignedOut: (notice: string) => void
}

const dimensions: { value: Dimension; label: string }[] = [
  { value: 'mass', label: 'Tömeg (g, dkg, kg)' },
  { value: 'volume', label: 'Térfogat (ml, dl, l)' },
  { value: 'count', label: 'Darab (db)' },
]

// The same tidy form as the backend's (Ingredient.Tidy).
const tidy = (name: string) => name.trim().split(/\s+/).join(' ')

// The ingredient field of the pantry, recipe, shopping list and minimum level forms: free text with the list's
// names offered (the choice: findIngredient). Each picker has its own option list, so several can share a page.
export function IngredientPicker({ ingredients, value, onChange, field, error, create }: {
  ingredients: Ingredient[]
  value: string
  onChange: (value: string) => void
  field: FieldProps
  error: ReactNode
  create?: IngredientCreation
}) {
  const listId = useId()
  const [creating, setCreating] = useState(false)
  const [notice, setNotice] = useState<string | null>(null)
  const name = tidy(value)
  const offer = create && name !== '' && !findIngredient(ingredients, value)
  // Review S3: the button or the small form that had the focus is gone; the focus goes back to the field.
  const focusField = () => document.getElementById(field.id)?.focus()

  return (
    <>
      <label htmlFor={field.id}>Hozzávaló</label>
      <input {...field} list={listId} autoComplete="off" value={value} onChange={(e) => onChange(e.target.value)} />
      <datalist id={listId}>
        {ingredients.map((i) => <option key={i.id} value={i.name} />)}
      </datalist>
      {error}
      {create && <p role="status" className="notice">{notice}</p>}
      {offer && !creating && (
        <button type="button" className="secondary" onClick={() => setCreating(true)}>Új hozzávaló: {name}</button>
      )}
      {offer && creating && (
        <NewIngredient
          key={name}
          name={name}
          create={create}
          onCancel={() => {
            setCreating(false)
            focusField()
          }}
          onCreated={(ingredient) => {
            setCreating(false)
            setNotice(`Létrehoztam: ${ingredient.name}. Csak a te háztartásodban látszik.`)
            create.onCreated(ingredient)
            focusField()
          }}
        />
      )}
    </>
  )
}

// A fieldset, not a form: the picker already sits inside the parent's form.
function NewIngredient({ name, create, onCancel, onCreated }: {
  name: string
  create: IngredientCreation
  onCancel: () => void
  onCreated: (ingredient: Ingredient) => void
}) {
  const id = useId()
  const [dimension, setDimension] = useState<Dimension | ''>('')
  const [category, setCategory] = useState<Category | ''>('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [dimensionError, setDimensionError] = useState(false)
  const measure = useRef<HTMLSelectElement>(null)

  // Review S3: the button that opened this form is gone, so the focus starts here.
  useEffect(() => measure.current?.focus(), [])

  async function submit() {
    setError(null)
    // Review S1: the missing choice is tied to its field and focused (ux_flows a11y).
    setDimensionError(!dimension)
    if (!dimension) {
      measure.current?.focus()
      return
    }

    setBusy(true)
    try {
      onCreated(await api.createIngredient({ name, dimension, defaultCategory: category || 'other' }))
    } catch (caught) {
      failWith(create.onSignedOut, setError)(caught)
      setBusy(false)
    }
  }

  return (
    <fieldset className="new-ingredient">
      <legend>Új hozzávaló: {name}</legend>
      <label htmlFor={`${id}-dimension`}>Mivel méred?</label>
      <select
        ref={measure}
        id={`${id}-dimension`}
        aria-invalid={dimensionError || undefined}
        aria-describedby={dimensionError ? `${id}-dimension-error` : undefined}
        value={dimension}
        onChange={(e) => setDimension(e.target.value as Dimension)}
      >
        <option value="">Válassz…</option>
        {dimensions.map((d) => <option key={d.value} value={d.value}>{d.label}</option>)}
      </select>
      {dimensionError && <p id={`${id}-dimension-error`} className="field-error">Válaszd ki, mivel méred: tömeg, térfogat vagy darab.</p>}
      <label htmlFor={`${id}-category`}>Kategória</label>
      <select id={`${id}-category`} value={category} onChange={(e) => setCategory(e.target.value as Category)}>
        <option value="">Egyéb</option>
        {create.categories.filter((c) => c.category !== 'other').map((c) => (
          <option key={c.category} value={c.category}>{categoryName(c.category)}</option>
        ))}
      </select>
      <p role="alert" className="form-error">{error}</p>
      <div className="actions">
        <button type="button" disabled={busy} onClick={() => void submit()}>Létrehozom</button>
        <button type="button" className="secondary" disabled={busy} onClick={onCancel}>Mégse</button>
      </div>
    </fieldset>
  )
}
