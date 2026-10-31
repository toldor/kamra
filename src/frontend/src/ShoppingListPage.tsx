import { useEffect, useId, useRef, useState, type FormEvent, type ReactNode } from 'react'
import {
  api, ApiError, failWith, type AmountRequest, type Ingredient, type ShoppingListItem, type ShoppingSuggestion, type Unit,
} from './api/client'
import { useFieldErrors } from './fieldErrors'
import { findIngredient, formatAmount, formatDay, formatListLine, matchesSearch, unitsOf } from './format'
import { IngredientPicker } from './IngredientPicker'

type Data = { suggestions: ShoppingSuggestion[]; items: ShoppingListItem[]; ingredients: Ingredient[] }

async function loadAll(): Promise<Data> {
  const [suggestions, items, ingredients] = await Promise.all([api.shoppingSuggestions(), api.shoppingList(), api.ingredients()])
  return { suggestions, items, ingredients }
}
type Load = { status: 'loading' } | { status: 'error'; message: string } | { status: 'ready'; data: Data }

// Field errors of a rejected write, or null when it succeeded.
type Write = (action: () => Promise<unknown>, success: string) => Promise<Record<string, string[]> | null>

// K5: the row is gone or was decided elsewhere; the page reloads, and the message says so.
const reloadOn = ['SHOPPING_LIST_ITEM_NOT_FOUND', 'SHOPPING_SUGGESTION_NOT_FOUND', 'SHOPPING_SUGGESTION_STATE_CONFLICT']

const allUnits = Object.values(unitsOf).flat()
const amountFields = ['amount', 'unit'] as const

// An empty amount field: no amount at all.
const amountOf = (amount: string, unit: Unit): { amount: number; unit: Unit } | null =>
  amount.trim() === '' ? null : { amount: Number(amount), unit }

function AmountFields({ label, amount, onAmount, units, unit, onUnit, fields, hint }: {
  label: string
  amount: string
  onAmount: (value: string) => void
  units: Unit[]
  unit: Unit
  onUnit: (value: Unit) => void
  fields: { props: ReturnType<typeof useFieldErrors>['props']; error: (field: string) => ReactNode }
  hint?: string
}) {
  const hintId = useId()
  const amountField = fields.props('amount', hint ? hintId : undefined)
  const unitField = fields.props('unit')
  return (
    <>
      <div className="row">
        <div className="field">
          <label htmlFor={amountField.id}>{label}</label>
          <input {...amountField} type="number" inputMode="decimal" min="0" step="any" value={amount} onChange={(e) => onAmount(e.target.value)} />
        </div>
        <div className="field">
          <label htmlFor={unitField.id}>Mértékegység</label>
          <select {...unitField} value={unit} onChange={(e) => onUnit(e.target.value as Unit)}>
            {units.map((u) => <option key={u} value={u}>{u}</option>)}
          </select>
        </div>
      </div>
      {fields.error('amount')}
      {fields.error('unit')}
      {hint && <p id={hintId} className="hint">{hint}</p>}
    </>
  )
}

// M12: why the suggestion is here, from live data.
function reasonOf(s: ShoppingSuggestion): string {
  if (s.trigger === 'depleted') return `Elfogyott (${formatDay(s.createdAt)}${s.recipeName ? `, főzés: ${s.recipeName}` : ''}).`
  const current = s.currentAmount ? formatAmount(s.currentAmount.value, s.currentAmount.unit) : ''
  return s.minimumAmount
    ? `A minimumszint (${formatAmount(s.minimumAmount.value, s.minimumAmount.unit)}) alá csökkent: ${current} van.`
    : `A minimumszint alá csökkent: ${current} van.`
}

// M12, M13: the reason, how much to buy (empty for a depleted item, the missing part for a minimum) and
// what each answer does.
function SuggestionRow({ suggestion: s, busy, write }: { suggestion: ShoppingSuggestion; busy: boolean; write: Write }) {
  const [amount, setAmount] = useState(s.suggestedAmount ? String(s.suggestedAmount.value) : '')
  const [unit, setUnit] = useState<Unit>(s.suggestedAmount?.unit ?? unitsOf[s.dimension][0])
  const fields = useFieldErrors(`suggestion-${s.id}`, amountFields)
  const headingId = useId()
  const explainId = useId()
  const depleted = s.trigger === 'depleted'

  async function accept() {
    const chosen = amountOf(amount, unit)
    const errors = await write(() => api.acceptSuggestion(s.id, chosen), `Felvettem a listára: ${formatListLine(s.ingredientName, chosen && { value: chosen.amount, unit })}.`)
    fields.setErrors(errors ?? {})
  }

  async function reject() {
    const success = depleted
      ? `Rendben. Amíg újra nem veszel belőle, nem ajánlom újra: ${s.ingredientName}.`
      : `Rendben. Amíg a készlet a minimumszint fölé nem kerül, nem ajánlom újra: ${s.ingredientName}.`
    await write(() => api.rejectSuggestion(s.id), success)
  }

  return (
    <article aria-labelledby={headingId} className="recipe">
      <h3 id={headingId}>{s.ingredientName}</h3>
      <p>{reasonOf(s)}</p>
      <AmountFields
        label="Mennyit vegyél?"
        amount={amount}
        onAmount={setAmount}
        units={unitsOf[s.dimension]}
        unit={unit}
        onUnit={setUnit}
        fields={fields}
        hint={s.suggestedAmount ? 'Ennyi hiányzik a minimumszintig.' : 'Ha üresen hagyod, mennyiség nélkül kerül a listára.'}
      />
      <p id={explainId} className="hint">
        {depleted
          ? 'Hozzáadom: felkerül a bevásárlólistára. Nem kell: nem ajánlom újra, amíg újra nem veszel belőle.'
          : 'Hozzáadom: felkerül a bevásárlólistára. Nem kell: nem ajánlom újra, amíg a készlet a minimumszint fölé nem kerül.'}
      </p>
      <div className="actions">
        <button type="button" aria-describedby={explainId} disabled={busy} onClick={accept}>Hozzáadom</button>
        <button type="button" className="secondary" aria-describedby={explainId} disabled={busy} onClick={reject}>Nem kell</button>
      </div>
    </article>
  )
}

// A form with an ingredient, an amount and a unit: the manual list entry and the minimum level.
function IngredientAmountForm({ prefix, heading, amountLabel, amountRequired, button, hint, ingredients, busy, onSubmit }: {
  prefix: string
  heading: string
  amountLabel: string
  // The message when the amount is required but empty; without it the amount may be left out.
  amountRequired?: string
  button: string
  hint: string
  ingredients: Ingredient[]
  busy: boolean
  // amount null: the field was left empty.
  onSubmit: (ingredient: Ingredient, amount: AmountRequest) => Promise<Record<string, string[]> | null>
}) {
  const [name, setName] = useState('')
  const [amount, setAmount] = useState('')
  const [unit, setUnit] = useState<Unit>('g')
  const fields = useFieldErrors(prefix, ['ingredientId', ...amountFields])
  const headingId = useId()
  const ingredient = findIngredient(ingredients, name)
  const units = ingredient ? unitsOf[ingredient.dimension] : allUnits
  const chosenUnit = units.includes(unit) ? unit : units[0]

  async function submit(event: FormEvent) {
    event.preventDefault()
    if (!ingredient) {
      fields.setErrors({ ingredientId: ['Válassz hozzávalót a listából.'] })
      return
    }
    const chosen = amountOf(amount, chosenUnit)
    if (!chosen && amountRequired) {
      fields.setErrors({ amount: [amountRequired] })
      return
    }
    fields.setErrors({})
    const errors = await onSubmit(ingredient, chosen ?? { amount: null, unit: chosenUnit })
    if (errors) {
      fields.setErrors(errors)
      return
    }
    setName('')
    setAmount('')
  }

  return (
    <form aria-labelledby={headingId} onSubmit={submit} noValidate>
      <h3 id={headingId}>{heading}</h3>
      <IngredientPicker ingredients={ingredients} value={name} onChange={setName} field={fields.props('ingredientId')} error={fields.error('ingredientId')} />
      <AmountFields label={amountLabel} amount={amount} onAmount={setAmount} units={units} unit={chosenUnit} onUnit={setUnit} fields={fields} />
      <p className="hint">{hint}</p>
      <button type="submit" disabled={busy}>{button}</button>
    </form>
  )
}

// US-5 (ux_flows 3. flow): the suggestions to decide on, the list itself and the minimum levels.
export function ShoppingListPage({ onSignedOut }: { onSignedOut: (notice: string) => void }) {
  const [load, setLoad] = useState<Load>({ status: 'loading' })
  const [reloads, setReloads] = useState(0)
  const [busy, setBusy] = useState(false)
  const [notice, setNotice] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const noticeRef = useRef<HTMLParagraphElement>(null)
  const errorRef = useRef<HTMLDivElement>(null)
  // Where the focus was when a write started (a disabled button loses it in the browser).
  const returnFocus = useRef<Element | null>(null)

  // K3: the three lists are always loaded together, so a suggestion, the list and the minimums agree.
  useEffect(() => {
    let active = true
    const fail = failWith(onSignedOut, (message) => setLoad({ status: 'error', message }))
    loadAll()
      .then((data) => active && setLoad({ status: 'ready', data }))
      .catch((caught: unknown) => active && fail(caught))
    return () => {
      active = false
    }
  }, [reloads, onSignedOut])

  // After a write the focus goes back where it was; if that element is gone (an accepted suggestion, a
  // deleted row), to the message about what happened (ux_flows a11y).
  useEffect(() => {
    const target = returnFocus.current
    if (busy || !target) return
    returnFocus.current = null
    if (target instanceof HTMLElement && target !== document.body && target.isConnected) target.focus()
    else if (notice) noticeRef.current?.focus()
    else if (error) errorRef.current?.focus()
  }, [busy, notice, error])

  // C2: the buttons stay disabled until the fresh lists have arrived, so an old row cannot be pressed again
  // and an older answer cannot overwrite a newer state.
  const write: Write = async (action, success) => {
    returnFocus.current = document.activeElement
    setBusy(true)
    setNotice(null)
    setError(null)
    try {
      await action()
    } catch (caught) {
      if (caught instanceof ApiError && reloadOn.includes(caught.code)) await refresh()
      failWith(onSignedOut, setError)(caught)
      setBusy(false)
      return caught instanceof ApiError ? caught.errors : {}
    }
    await refresh()
    setNotice(success)
    setBusy(false)
    return null
  }

  async function refresh() {
    try {
      setLoad({ status: 'ready', data: await loadAll() })
    } catch (caught) {
      failWith(onSignedOut, (message) => setLoad({ status: 'error', message }))(caught)
    }
  }

  // In the shop the tick shows at once; a failed save takes it back.
  async function check(id: string, checked: boolean) {
    const tick = (value: boolean) => setLoad((current) => current.status !== 'ready' ? current : {
      ...current, data: { ...current.data, items: current.data.items.map((i) => (i.id === id ? { ...i, checked: value } : i)) },
    })
    tick(checked)
    if (await write(() => api.checkShoppingListItem(id, checked), '')) tick(!checked)
  }

  return (
    <main>
      <h1 tabIndex={-1}>Bevásárlólista</h1>
      <p ref={noticeRef} role="status" className="notice" tabIndex={-1}>{notice}</p>
      <div ref={errorRef} role="alert" className="form-error" tabIndex={-1}>{error}</div>
      {load.status === 'loading' && <p role="status">Betöltés…</p>}
      {load.status === 'error' && (
        <div>
          <p role="alert">{load.message}</p>
          <button
            type="button"
            onClick={() => {
              setLoad({ status: 'loading' })
              setReloads((n) => n + 1)
            }}
          >
            Újra
          </button>
        </div>
      )}
      {load.status === 'ready' && <Content data={load.data} busy={busy} write={write} onCheck={check} />}
    </main>
  )
}

function Content({ data: { suggestions, items, ingredients }, busy, write, onCheck }: {
  data: Data
  busy: boolean
  write: Write
  onCheck: (id: string, checked: boolean) => void
}) {
  const minimums = ingredients.filter((i) => i.minimum)
  // v0.3.1: the search filters the loaded list in the browser.
  const [search, setSearch] = useState('')
  const shown = items.filter((i) => matchesSearch(search, i.ingredientName))

  return (
    // C3: the suggestions and the list in the content column, the minimum levels beside them.
    <div className="split">
      <div className="content">
      {suggestions.length > 0 && (
        <section aria-labelledby="suggestions-heading" className="panel">
          <h2 id="suggestions-heading">Javaslatok ({suggestions.length})</h2>
          {/* key: a new suggestion for the same ingredient starts with its own amount. */}
          {suggestions.map((s) => <SuggestionRow key={s.id} suggestion={s} busy={busy} write={write} />)}
        </section>
      )}

      <section aria-labelledby="list-heading" className="panel">
        <h2 id="list-heading">A listád</h2>
        <div className="panel-body">
        <p className="hint">Amíg egy hozzávaló pipa nélkül a listán van, nem javaslom újra.</p>
        {items.length > 0 && (
          <form role="search" className="filters" onSubmit={(e) => e.preventDefault()}>
            <label htmlFor="list-search">Keresés</label>
            <input id="list-search" type="search" value={search} onChange={(e) => setSearch(e.target.value)} />
          </form>
        )}
        {items.length === 0 && (
          <p className="empty">
            {suggestions.length > 0
              ? 'A listád üres. Fogadj el egy javaslatot, vagy vegyél fel tételt alább.'
              : 'A listád üres. Vegyél fel tételt alább.'}
          </p>
        )}
        {items.length > 0 && shown.length === 0 && <p className="empty">Nincs a keresésnek megfelelő tétel a listádon.</p>}
        </div>
        {shown.length > 0 && (
          <ul className="items">
            {shown.map((item) => (
              <li key={item.id} className={item.checked ? 'item text-row done' : 'item text-row'}>
                <label className="check">
                  <input
                    type="checkbox"
                    checked={item.checked}
                    disabled={busy}
                    onChange={(e) => onCheck(item.id, e.target.checked)}
                  />
                  {item.amount ? `${item.ingredientName} – ${formatAmount(item.amount.value, item.amount.unit)}` : item.ingredientName}
                </label>
                <span className="leader" aria-hidden="true" />
                <button
                  type="button"
                  className="secondary"
                  aria-label={`Törlés: ${item.ingredientName}`}
                  disabled={busy}
                  onClick={() => void write(() => api.deleteShoppingListItem(item.id), `Töröltem a listáról: ${item.ingredientName}.`)}
                >
                  Törlés
                </button>
              </li>
            ))}
          </ul>
        )}
        <IngredientAmountForm
          prefix="list"
          heading="Új tétel a listára"
          amountLabel="Mennyiség (nem kötelező)"
          button="Felveszem"
          hint="Ha a hozzávaló már a listán van, a mennyiség hozzáadódik; ha kipipáltad, az új mennyiséggel kerül vissza."
          ingredients={ingredients}
          busy={busy}
          onSubmit={(ingredient, amount) => write(
            () => api.addShoppingListItem(amount.amount === null ? { ingredientId: ingredient.id } : { ingredientId: ingredient.id, ...amount }),
            `Felvettem a listára: ${formatListLine(ingredient.name, amount.amount === null ? null : { value: amount.amount, unit: amount.unit })}.`)}
        />
      </section>
      </div>

      <section aria-labelledby="minimum-heading" className="panel side">
        <h2 id="minimum-heading">Minimumszintek</h2>
        <div className="panel-body">
          <p className="hint">Ha egy hozzávalóból a minimumszint alá csökken a készlet, felveszem a bevásárlójavaslatok közé.</p>
          {minimums.length === 0 && <p>Még nem adtál meg minimumszintet.</p>}
        </div>
        {minimums.length > 0 && (
          <ul className="items">
            {minimums.map((i) => (
              <li key={i.id} className="item text-row">
                <span>{i.name}: {formatAmount(i.minimum!.value, i.minimum!.unit)}</span>
                <span className="leader" aria-hidden="true" />
                <button
                  type="button"
                  className="secondary"
                  aria-label={`Minimumszint törlése: ${i.name}`}
                  disabled={busy}
                  onClick={() => void write(() => api.setMinimum(i.id, null), `Töröltem a minimumszintet: ${i.name}.`)}
                >
                  Törlés
                </button>
              </li>
            ))}
          </ul>
        )}
        <IngredientAmountForm
          prefix="minimum"
          heading="Új minimumszint"
          amountLabel="Mennyiség"
          amountRequired="Add meg, mennyi legyen a minimumszint."
          button="Beállítom"
          hint="Ha a hozzávalónak már van minimumszintje, az új érték lecseréli."
          ingredients={ingredients}
          busy={busy}
          onSubmit={(ingredient, amount) => write(
            () => api.setMinimum(ingredient.id, amount),
            `Beállítottam a minimumszintet: ${ingredient.name}, ${formatAmount(amount.amount ?? 0, amount.unit)}.`)}
        />
      </section>
    </div>
  )
}
