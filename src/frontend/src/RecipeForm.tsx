import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { api, ApiError, failWith, type CategoryInfo, type Dimension, type Ingredient, type Unit } from './api/client'
import { useFieldErrors } from './fieldErrors'
import { allUnits, findIngredient, formatAmount, unitsOf } from './format'
import { IngredientPicker } from './IngredientPicker'
import { newDraft, newRow, readDraft, storeDraft, type Draft, type Row } from './recipeDraft'
import { routes } from './useHashRoute'

type Catalog = { status: 'loading' } | { status: 'error'; message: string } | { status: 'ready'; categories: CategoryInfo[]; ingredients: Ingredient[] }

const rowField = (index: number, field: string) => `ingredients[${index}].${field}`
const measuredIn: Record<Dimension, string> = { mass: 'tömegben', volume: 'térfogatban', count: 'darabban' }

// Review U1 (Q8: A): a unit of the chat's idea that does not fit the matched ingredient is not converted (AGENTS.md 5.:
// no guessing); its row says so, with the field error of the form.
function unitMismatches(rows: Row[], ingredients: Ingredient[]): Record<string, string[]> {
  return Object.fromEntries(rows.flatMap((row, i) => {
    const ingredient = findIngredient(ingredients, row.ingredient)
    return ingredient && !unitsOf[ingredient.dimension].includes(row.unit)
      ? [[rowField(i, 'unit'), [`A chat ötletében ${formatAmount(Number(row.amount), row.unit)} szerepelt, de a(z) ${ingredient.name} ` +
          `${measuredIn[ingredient.dimension]} mérhető. Válaszd ki a mértékegységet, és írd át a mennyiséget.`]]]
      : []
  }))
}

// N10 (M9): a new own recipe. The rows' backend errors ("ingredients[1].amount") land under their field.
export function RecipeForm({ onSaved, onSignedOut }: { onSaved: (notice: string) => void; onSignedOut: (notice: string) => void }) {
  const [restored] = useState(readDraft)
  const [draft, setDraft] = useState<Draft>(() => restored ?? newDraft())
  const [catalog, setCatalog] = useState<Catalog>({ status: 'loading' })
  const [tries, setTries] = useState(0)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  // Review S5: the live region stays empty until the form is shown, so the restored-draft text is announced.
  const [restoredNotice, setRestoredNotice] = useState<string | null>(null)
  const rowCount = draft.rows.length
  const order = useMemo(
    () => ['name', 'servings', ...Array.from({ length: rowCount }, (_, i) => ['ingredientId', 'amount', 'unit'].map((f) => rowField(i, f))).flat(),
      'instructions'],
    [rowCount])
  const fields = useFieldErrors('recipe', order)
  const setFieldErrors = fields.setErrors

  useEffect(() => {
    let active = true
    const fail = failWith(onSignedOut, (message) => setCatalog({ status: 'error', message }))
    Promise.all([api.categories(), api.ingredients()])
      .then(([categories, ingredients]) => {
        if (!active) return
        setCatalog({ status: 'ready', categories, ingredients })
        if (restored?.aiSuggested) setFieldErrors(unitMismatches(restored.rows, ingredients))
        if (restored) {
          setRestoredNotice(restored.aiSuggested
            ? 'Átvettem a chat receptötletét. Mentés előtt nézd át, és javítsd, amit kell.'
            : 'Visszatöltöttem a félbehagyott receptet.')
        }
      })
      .catch((caught: unknown) => active && fail(caught))
    return () => {
      active = false
    }
  }, [tries, onSignedOut, restored, setFieldErrors])

  const ingredients = catalog.status === 'ready' ? catalog.ingredients : []

  function change(next: Draft) {
    setDraft(next)
    storeDraft(next)
  }

  const changeRow = (index: number, patch: Partial<Row>) =>
    change({ ...draft, rows: draft.rows.map((r, i) => (i === index ? { ...r, ...patch } : r)) })

  function unitsFor(row: Row) {
    const ingredient = findIngredient(ingredients, row.ingredient)
    const units = ingredient ? unitsOf[ingredient.dimension] : allUnits
    return { units, unit: units.includes(row.unit) ? row.unit : units[0] }
  }

  async function submit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    const chosen = draft.rows.map((r) => findIngredient(ingredients, r.ingredient))
    const missing = Object.fromEntries(chosen.flatMap((c, i) => (c ? [] : [[rowField(i, 'ingredientId'), ['Válassz hozzávalót a listából.']]])))
    if (Object.keys(missing).length > 0) {
      fields.setErrors(missing)
      return
    }

    setBusy(true)
    fields.setErrors({})
    try {
      const saved = await api.createRecipe({
        name: draft.name,
        servings: draft.servings.trim() === '' ? null : Number(draft.servings),
        instructions: draft.instructions,
        ingredients: draft.rows.map((r, i) => ({
          ingredientId: chosen[i]!.id,
          amount: r.amount.trim() === '' ? null : Number(r.amount),
          unit: unitsFor(r).unit,
        })),
        // receptotlet_minispec 6.: only a draft from the chat's idea is marked; a hand-written one sends nothing.
        aiSuggested: draft.aiSuggested || undefined,
      })
      storeDraft(null)
      onSaved(`Elmentettem a receptet: ${saved.name}.`)
    } catch (caught) {
      failWith(onSignedOut, setError)(caught)
      if (caught instanceof ApiError && caught.status !== 401) {
        // Review S2: a recipe-level error (no or too many ingredients) goes under the first row's field, so it
        // is tied to a field and gets the focus.
        const { ingredients: recipeLevel, ...rest } = caught.errors
        const first = rowField(0, 'ingredientId')
        fields.setErrors(recipeLevel ? { ...rest, [first]: [...recipeLevel, ...(rest[first] ?? [])] } : rest)
      }
      setBusy(false)
    }
  }

  return (
    <main className="form-page">
      <h1 tabIndex={-1}>Új recept</h1>
      <p role="status" className="notice">{restoredNotice}</p>
      {catalog.status === 'loading' && <p>Betöltés…</p>}
      {catalog.status === 'error' && (
        <div>
          <p role="alert">{catalog.message}</p>
          <button
            type="button"
            onClick={() => {
              setCatalog({ status: 'loading' })
              setTries((n) => n + 1)
            }}
          >
            Újra
          </button>
        </div>
      )}
      {catalog.status === 'ready' && (
        <form aria-label="Új recept" onSubmit={submit} noValidate>
          <label htmlFor={fields.props('name').id}>Név</label>
          <input {...fields.props('name')} value={draft.name} onChange={(e) => change({ ...draft, name: e.target.value })} />
          {fields.error('name')}

          <label htmlFor={fields.props('servings').id}>Adagszám</label>
          <input
            {...fields.props('servings')}
            type="number"
            inputMode="numeric"
            min="1"
            max="100"
            value={draft.servings}
            onChange={(e) => change({ ...draft, servings: e.target.value })}
          />
          {fields.error('servings')}

          <h2>Hozzávalók</h2>
          <p className="hint">A mennyiségeket az adagszámra add meg. A víz, a só és a bors szerepelhet, de az ajánlás nem számol velük.</p>
          {draft.rows.map((row, i) => {
            const { units, unit } = unitsFor(row)
            return (
              <fieldset key={row.key} className="recipe-row">
                <legend>{i + 1}. hozzávaló</legend>
                <IngredientPicker
                  ingredients={ingredients}
                  value={row.ingredient}
                  onChange={(value) => changeRow(i, { ingredient: value })}
                  field={fields.props(rowField(i, 'ingredientId'))}
                  error={fields.error(rowField(i, 'ingredientId'))}
                  create={{
                    categories: catalog.categories,
                    onCreated: (ingredient) =>
                      setCatalog((current) => (current.status === 'ready' ? { ...current, ingredients: [...current.ingredients, ingredient] } : current)),
                    onSignedOut,
                  }}
                />
                <div className="row">
                  <div className="field">
                    <label htmlFor={fields.props(rowField(i, 'amount')).id}>Mennyiség</label>
                    <input
                      {...fields.props(rowField(i, 'amount'))}
                      type="number"
                      inputMode="decimal"
                      min="0"
                      step="any"
                      value={row.amount}
                      onChange={(e) => changeRow(i, { amount: e.target.value })}
                    />
                  </div>
                  <div className="field">
                    <label htmlFor={fields.props(rowField(i, 'unit')).id}>Mértékegység</label>
                    <select {...fields.props(rowField(i, 'unit'))} value={unit} onChange={(e) => changeRow(i, { unit: e.target.value as Unit })}>
                      {units.map((u) => <option key={u} value={u}>{u}</option>)}
                    </select>
                  </div>
                </div>
                {fields.error(rowField(i, 'amount'))}
                {fields.error(rowField(i, 'unit'))}
                {rowCount > 1 && (
                  <button
                    type="button"
                    className="secondary"
                    aria-label={`Sor törlése: ${i + 1}. hozzávaló`}
                    onClick={() => {
                      // Review S2: row errors follow the index, so they would land under another row.
                      fields.setErrors({})
                      change({ ...draft, rows: draft.rows.filter((_, j) => j !== i) })
                    }}
                  >
                    Sor törlése
                  </button>
                )}
              </fieldset>
            )
          })}
          <button type="button" className="secondary" onClick={() => change({ ...draft, rows: [...draft.rows, newRow()] })}>
            Újabb hozzávaló
          </button>

          <label htmlFor={fields.props('instructions').id}>Elkészítés (nem kötelező)</label>
          <textarea
            {...fields.props('instructions')}
            rows={6}
            value={draft.instructions}
            onChange={(e) => change({ ...draft, instructions: e.target.value })}
          />
          {fields.error('instructions')}

          <p role="alert" className="form-error">{error}</p>
          <div className="actions">
            <button type="submit" disabled={busy}>Mentés</button>
            <button
              type="button"
              className="secondary"
              onClick={() => {
                storeDraft(null)
                window.location.hash = routes.recipes
              }}
            >
              Mégse
            </button>
          </div>
        </form>
      )}
    </main>
  )
}
