import { useState, type FormEvent } from 'react'
import { api, ApiError, failWith, type Category, type CategoryInfo, type Ingredient, type Unit } from './api/client'
import { useFieldErrors } from './fieldErrors'
import { categoryName, unitsOf } from './format'

const allUnits = Object.values(unitsOf).flat()
const fieldOrder = ['ingredientId', 'amount', 'unit', 'category', 'expiryDate'] as const

// US-1: a new pantry item. The ingredient is chosen from the list (ADR-0003); the units follow its
// dimension (ADR-0002); a missing expiry date is estimated by the backend from the category.
export function PantryItemForm({ categories, ingredients, onAdded, onSignedOut }: {
  categories: CategoryInfo[]
  ingredients: Ingredient[]
  onAdded: () => void
  onSignedOut: (notice: string) => void
}) {
  const [ingredientName, setIngredientName] = useState('')
  const [amount, setAmount] = useState('')
  const [unit, setUnit] = useState<Unit>('g')
  const [category, setCategory] = useState<Category | ''>('')
  const [expiryDate, setExpiryDate] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const fields = useFieldErrors('new', fieldOrder)

  const ingredient = ingredients.find((i) => i.name.toLowerCase() === ingredientName.trim().toLowerCase())
  const units = ingredient ? unitsOf[ingredient.dimension] : allUnits
  const chosenUnit = units.includes(unit) ? unit : units[0]
  const chosenCategory = category || ingredient?.defaultCategory || ''

  async function submit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    if (!ingredient) {
      fields.setErrors({ ingredientId: ['Válassz hozzávalót a listából.'] })
      return
    }

    setBusy(true)
    fields.setErrors({})
    try {
      await api.addPantryItem({
        ingredientId: ingredient.id,
        amount: amount.trim() === '' ? null : Number(amount),
        unit: chosenUnit,
        category: chosenCategory || null,
        expiryDate: expiryDate || null,
      })
      setIngredientName('')
      setAmount('')
      setUnit('g')
      setCategory('')
      setExpiryDate('')
      onAdded()
    } catch (caught) {
      failWith(onSignedOut, setError)(caught)
      if (caught instanceof ApiError && caught.status !== 401) fields.setErrors(caught.errors)
    } finally {
      setBusy(false)
    }
  }

  return (
    <form aria-labelledby="new-item-heading" onSubmit={submit} noValidate>
      <h2 id="new-item-heading">Új tétel</h2>

      <label htmlFor="new-ingredientId">Hozzávaló</label>
      <input
        {...fields.props('ingredientId')}
        list="ingredient-options"
        autoComplete="off"
        value={ingredientName}
        onChange={(e) => setIngredientName(e.target.value)}
      />
      <datalist id="ingredient-options">
        {ingredients.map((i) => <option key={i.id} value={i.name} />)}
      </datalist>
      {fields.error('ingredientId')}

      <div className="row">
        <div className="field">
          <label htmlFor="new-amount">Mennyiség</label>
          <input
            {...fields.props('amount')}
            type="number"
            inputMode="decimal"
            min="0"
            step="any"
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
          />
        </div>
        <div className="field">
          <label htmlFor="new-unit">Mértékegység</label>
          <select {...fields.props('unit')} value={chosenUnit} onChange={(e) => setUnit(e.target.value as Unit)}>
            {units.map((u) => <option key={u} value={u}>{u}</option>)}
          </select>
        </div>
      </div>
      {fields.error('amount')}
      {fields.error('unit')}

      <label htmlFor="new-category">Kategória</label>
      <select {...fields.props('category')} value={chosenCategory} onChange={(e) => setCategory(e.target.value as Category)}>
        {!chosenCategory && <option value="">Válassz kategóriát</option>}
        {categories.map((c) => <option key={c.category} value={c.category}>{categoryName(c.category)}</option>)}
      </select>
      {fields.error('category')}

      <label htmlFor="new-expiryDate">Lejárat (nem kötelező)</label>
      <input
        {...fields.props('expiryDate', 'new-expiry-hint')}
        type="date"
        value={expiryDate}
        onChange={(e) => setExpiryDate(e.target.value)}
      />
      {fields.error('expiryDate') || (
        <p id="new-expiry-hint" className="hint">Ha üresen hagyod, a kategória alapján becsüljük meg.</p>
      )}

      <div role="alert" className="form-error">{error}</div>
      <button type="submit" disabled={busy}>{busy ? 'Kérlek, várj…' : 'Hozzáadás'}</button>
    </form>
  )
}
