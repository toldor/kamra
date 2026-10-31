import { useEffect, useId, useMemo, useRef, useState, type ReactNode } from 'react'
import { api, ApiError, failWith, type AvailableAmount, type Cooking, type CookingPreview, type CookingPreviewLine } from './api/client'
import { useFieldErrors, type FieldProps } from './fieldErrors'
import { formatAmount, formatDate } from './format'
import { ShortageToList } from './ShortageToList'
import { routes } from './useHashRoute'

type Load = { status: 'loading' } | { status: 'error'; message: string } | { status: 'ready'; preview: CookingPreview }

const storageKey = (recipeId: string) => `kamra.cooking.${recipeId}`

const roundToThousandths = (value: number) => Math.round(value * 1000) / 1000

// ADR-0008: one request id per confirmation, kept in sessionStorage until the cooking succeeds, so a
// reload or a retry after a network error sends the same id and the backend deducts only once.
function storedRequestId(recipeId: string): string {
  try {
    const stored = sessionStorage.getItem(storageKey(recipeId))
    if (stored) return stored
    const created = crypto.randomUUID()
    sessionStorage.setItem(storageKey(recipeId), created)
    return created
  } catch {
    // Storage blocked (private mode): the id still lives as long as the screen.
    return crypto.randomUUID()
  }
}

function forgetRequestId(recipeId: string) {
  try {
    sessionStorage.removeItem(storageKey(recipeId))
  } catch {
    // Nothing stored.
  }
}

const validServings = (text: string) => /^\d+$/.test(text) && Number(text) >= 1 && Number(text) <= 100

// D-d: the same range as the backend; the decimals are checked there, in base units (D-a).
const validUsed = (text: string) => text.trim() !== '' && Number(text) >= 0 && Number(text) <= 100_000

const usedMessage = 'A felhasznált mennyiség 0 és 100 000 között legyen.'

// D-b: on this screen a changed item means the stock changed, not that an edit must be saved again.
const stockChangedMessage = 'Közben megváltozott a készlet. Számold újra, és erősítsd meg újra.'

// The live shortage of a line: what the recipe needs beyond the entered used amount.
function shortageOf(line: CookingPreviewLine, value: string): number {
  const used = value.trim() === '' ? 0 : Number(value)
  return Number.isFinite(used) ? roundToThousandths(Math.max(0, line.required - Math.max(0, used))) : 0
}

function Line({ line, value, onChange, field, error }: {
  line: CookingPreviewLine
  value: string
  onChange: (value: string) => void
  field: FieldProps
  error: ReactNode
}) {
  const used = value.trim() === '' ? 0 : Number(value)
  const shortage = shortageOf(line, value)
  // F5: the split of the default amount; an edited amount is split by the backend the same way.
  // Nothing in stock: no allocations and no line.
  const allocations = line.allocations.map((a) => `${formatDate(a.expiryDate)} – ${formatAmount(a.amount, line.unit)}${a.expired ? ' (lejárt)' : ''}`).join(', ')
  const deduction = value === String(line.defaultUsed)
    ? allocations && `Levonás (lejárat – mennyiség): ${allocations}`
    : used > 0 ? 'Levonás: a legkorábban lejáró tételtől kezdve.' : ''

  return (
    <fieldset className="cooking-line">
      <legend>{line.ingredientName}</legend>
      <p className="hint">Kell: {formatAmount(line.required, line.unit)} · Van: {formatAmount(line.available, line.unit)}</p>
      <label htmlFor={field.id}>Felhasznált ({line.unit})</label>
      <input {...field} type="number" inputMode="decimal" min="0" step="any" value={value} onChange={(e) => onChange(e.target.value)} />
      {error}
      {/* R1: an entered amount the stock no longer covers; a field error already says it. */}
      {!error && used > line.available && <p className="warning">Több, mint ami van ({formatAmount(line.available, line.unit)}).</p>}
      {shortage > 0 && <p className="warning">Hiány: {formatAmount(shortage, line.unit)}</p>}
      {deduction && <p className="hint">{deduction}</p>}
    </fieldset>
  )
}

function Summary({ cooking }: { cooking: Cooking }) {
  const heading = useRef<HTMLHeadingElement>(null)
  useEffect(() => heading.current?.focus(), [])
  const deducted = cooking.lines.filter((l) => l.used > 0)

  return (
    <section aria-labelledby="cooking-done">
      <h2 id="cooking-done" ref={heading} tabIndex={-1}>Elkészült: {cooking.recipeName}, {cooking.servings} adag.</h2>
      {deducted.length > 0 && <p>Levonva: {deducted.map((l) => `${l.ingredientName} ${formatAmount(l.used, l.unit)}`).join(', ')}.</p>}
      {cooking.depletedIngredients.length > 0 && <p>Elfogyott: {cooking.depletedIngredients.join(', ')}.</p>}
      {/* M5, M15: only what really became a suggestion is named. */}
      {cooking.suggestedIngredients.length > 0 && <p>Felvettem a bevásárlójavaslatok közé: {cooking.suggestedIngredients.join(', ')}.</p>}
      <p className="actions">
        {cooking.suggestedIngredients.length > 0 && <a href={routes.shopping} className="button-link">Megnézem</a>}
        <a href={routes.pantry} className="button-link">Vissza a készlethez</a>
        <a href={routes.recommendations} className="button-link">Mit főzzek?</a>
      </p>
    </section>
  )
}

// US-4 2. / P13: the live shortages go on the shopping list before the confirmation.
function CookingShortages({ preview, used, onSignedOut }: {
  preview: CookingPreview
  used: Record<string, string>
  onSignedOut: (notice: string) => void
}) {
  const lines = preview.lines
    .map((l) => ({ ingredientId: l.ingredientId, ingredientName: l.ingredientName, amount: shortageOf(l, used[l.ingredientId] ?? ''), unit: l.unit }))
    .filter((l) => l.amount > 0)
  // Always mounted: once added, the button stays gone for this screen (K10).
  return <ShortageToList label="Hiány a bevásárlólistára" lines={lines} showLink={false} onSignedOut={onSignedOut} />
}

// US-4 confirmation screen: servings, what each ingredient needs and has, the editable used amount, the
// live shortage and the items the deduction comes from; then the summary.
export function CookingPage({ recipeId, onSignedOut }: { recipeId: string; onSignedOut: (notice: string) => void }) {
  const [requestId] = useState(() => storedRequestId(recipeId))
  // undefined: the recipe's own number of servings.
  const [servings, setServings] = useState<number>()
  const [servingsText, setServingsText] = useState('')
  const [load, setLoad] = useState<Load>({ status: 'loading' })
  const [tries, setTries] = useState(0)
  const [used, setUsed] = useState<Record<string, string>>({})
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<{ message: string; recalculate: boolean } | null>(null)
  const [cooked, setCooked] = useState<Cooking | null>(null)
  // R1: "Újraszámolás" reloads the preview but keeps what the user entered.
  const keepUsed = useRef(false)
  const hintId = useId()
  const preview = load.status === 'ready' ? load.preview : null
  // The first invalid line gets the focus; the order follows the screen.
  const lineOrder = useMemo(() => preview?.lines.map((l) => l.ingredientId) ?? [], [preview])
  const fields = useFieldErrors('used', lineOrder)

  useEffect(() => {
    let active = true
    const fail = failWith(onSignedOut, (message) => setLoad({ status: 'error', message }))
    api.cookingPreview(recipeId, servings)
      .then((preview) => {
        if (!active) return
        setLoad({ status: 'ready', preview })
        if (servings === undefined) setServingsText(String(preview.servings))
        if (!keepUsed.current) setUsed(Object.fromEntries(preview.lines.map((l) => [l.ingredientId, String(l.defaultUsed)])))
        keepUsed.current = false
      })
      .catch((caught: unknown) => active && fail(caught))
    return () => {
      active = false
    }
  }, [recipeId, servings, tries, onSignedOut])

  function changeServings(text: string) {
    // New servings mean new required amounts, so the defaults come back (K11).
    keepUsed.current = false
    setServingsText(text)
    if (validServings(text)) setServings(Number(text))
  }

  // R1 (US-4 4.): the fresh amounts of an INSUFFICIENT_STOCK answer replace the old ones, and every line
  // whose entered amount is now too much is marked at its field.
  function showAvailable(preview: CookingPreview, available: AvailableAmount[]) {
    const fresh = new Map(available.map((a) => [a.ingredientId, a.amount]))
    const lines = preview.lines.map((l) => ({ ...l, available: fresh.get(l.ingredientId) ?? l.available }))
    setLoad({ status: 'ready', preview: { ...preview, lines } })
    const tooMuch = lines.filter((l) => fresh.has(l.ingredientId) && Number(used[l.ingredientId]) > l.available)
    fields.setErrors(Object.fromEntries(tooMuch.map((l) => [l.ingredientId,
      [`Már csak ${formatAmount(l.available, l.unit)} van, ${formatAmount(Number(used[l.ingredientId]), l.unit)} helyett.`]])))
  }

  async function confirm(preview: CookingPreview) {
    setError(null)
    const invalid = preview.lines.filter((l) => !validUsed(used[l.ingredientId] ?? ''))
    fields.setErrors(Object.fromEntries(invalid.map((l) => [l.ingredientId, [usedMessage]])))
    if (invalid.length > 0) return

    setBusy(true)
    try {
      const cooking = await api.confirmCooking({
        requestId,
        recipeId,
        servings: preview.servings,
        used: preview.lines.map((l) => ({
          ingredientId: l.ingredientId,
          amount: used[l.ingredientId]?.trim() ? Number(used[l.ingredientId]) : null,
        })),
      })
      forgetRequestId(recipeId)
      setCooked(cooking)
    } catch (caught) {
      // A conflicting earlier cooking with this id: the next screen starts with a new one.
      if (caught instanceof ApiError && caught.code === 'IDEMPOTENCY_CONFLICT') forgetRequestId(recipeId)
      const code = caught instanceof ApiError ? caught.code : ''
      if (code === 'RECIPE_ARCHIVED') {
        // Review C1: confirming again cannot help, so the page offers the way back, like a failed preview.
        failWith(onSignedOut, (message) => setLoad({ status: 'error', message }))(caught)
        setBusy(false)
        return
      }
      const recalculate = code === 'INSUFFICIENT_STOCK' || code === 'PANTRY_ITEM_MODIFIED'
      const fieldErrors = caught instanceof ApiError ? Object.values(caught.errors).flat() : []
      const show = (message: string) =>
        setError({ message: code === 'PANTRY_ITEM_MODIFIED' ? stockChangedMessage : fieldErrors.length > 0 ? fieldErrors.join(' ') : message, recalculate })
      failWith(onSignedOut, show)(caught)
      if (caught instanceof ApiError && caught.available.length > 0) showAvailable(preview, caught.available)
      setBusy(false)
    }
  }

  return (
    <main className="form-page">
      <h1 tabIndex={-1}>{preview ? `${preview.name} főzése` : 'Főzés'}</h1>
      {load.status === 'loading' && <p role="status">Betöltés…</p>}
      {load.status === 'error' && (
        <>
          <p role="alert">{load.message}</p>
          <a href={routes.recommendations} className="button-link">Vissza a receptekhez</a>
        </>
      )}
      {cooked && <Summary cooking={cooked} />}
      {preview && !cooked && (
        <form
          aria-label="Főzés megerősítése"
          noValidate
          onSubmit={(e) => {
            e.preventDefault()
            void confirm(preview)
          }}
        >
          <label htmlFor="cooking-servings">Adagszám</label>
          <input
            id="cooking-servings"
            type="number"
            inputMode="numeric"
            min="1"
            max="100"
            value={servingsText}
            aria-invalid={validServings(servingsText) ? undefined : true}
            aria-describedby={validServings(servingsText) ? hintId : `${hintId}-error`}
            onChange={(e) => changeServings(e.target.value)}
          />
          <p id={hintId} className="hint">A recept {preview.recipeServings} adagos.</p>
          {!validServings(servingsText) && <p id={`${hintId}-error`} className="field-error">Az adagszám 1 és 100 között legyen.</p>}
          {preview.lines.map((line) => (
            <Line
              key={line.ingredientId}
              line={line}
              value={used[line.ingredientId] ?? ''}
              onChange={(value) => setUsed((current) => ({ ...current, [line.ingredientId]: value }))}
              field={fields.props(line.ingredientId)}
              error={fields.error(line.ingredientId)}
            />
          ))}
          {preview.staples.length > 0 && <p className="hint">Alaphozzávalók, ezek nem vonódnak le: {preview.staples.join(', ')}.</p>}
          <CookingShortages preview={preview} used={used} onSignedOut={onSignedOut} />
          <h2>Elkészítés</h2>
          <p className="instructions">{preview.instructions}</p>
          {error && (
            <div>
              <p role="alert" className="form-error">{error.message}</p>
              {error.recalculate && (
                <button
                  type="button"
                  className="link"
                  onClick={() => {
                    setError(null)
                    fields.setErrors({})
                    keepUsed.current = true
                    setTries((n) => n + 1)
                  }}
                >
                  Újraszámolás
                </button>
              )}
            </div>
          )}
          <button type="submit" disabled={busy || servingsText !== String(preview.servings)}>
            {busy ? 'Kérlek, várj…' : 'Főzés megerősítése'}
          </button>
        </form>
      )}
    </main>
  )
}
