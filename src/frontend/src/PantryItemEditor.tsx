import { useEffect, useRef, useState, type FormEvent } from 'react'
import { api, ApiError, failWith, noFilter, type Category, type CategoryInfo, type PantryItem, type Unit } from './api/client'
import { messageFor } from './api/messages'
import { useFieldErrors } from './fieldErrors'
import { categoryName, formatAmount, formatDate, toBase, unitsOf } from './format'

const reasons = [
  { value: 'consumed', label: 'Elfogyott' },
  { value: 'discarded', label: 'Kidobtam' },
  { value: 'corrected', label: 'Hibás rögzítés' },
]

const fieldOrder = ['amount', 'unit', 'category', 'expiryDate', 'reason'] as const

const dimensionOf = (unit: Unit) => (Object.keys(unitsOf) as (keyof typeof unitsOf)[]).find((d) => unitsOf[d].includes(unit))!

// Base quantities have at most 3 decimals (ADR-0002); rounding avoids 1.15 dl reading as 114.999... ml.
const roundToThousandths = (value: number) => Math.round(value * 1000) / 1000

// What changed between the version the user edited and the fresh one (US-1: "a változás kiemelésével").
function describeChanges(before: PantryItem, after: PantryItem): string | null {
  const changes = [
    before.quantity !== after.quantity || before.unit !== after.unit
      ? `mennyiség: ${formatAmount(before.amount, before.unit)} → ${formatAmount(after.amount, after.unit)}` : null,
    before.category !== after.category ? `kategória: ${categoryName(before.category)} → ${categoryName(after.category)}` : null,
    before.expiryDate !== after.expiryDate ? `lejárat: ${formatDate(before.expiryDate)} → ${formatDate(after.expiryDate)}` : null,
  ].filter((change) => change !== null)
  return changes.length ? `Közben változott – ${changes.join('; ')}` : null
}

// US-1: edit, decrease (with a reason) or delete (decrease to 0 with a reason) one pantry item. The
// version the user saw goes with the request; a conflict shows the fresh values instead of saving
// (ADR-0008), and an item deleted elsewhere closes the dialog.
export function PantryItemEditor({ item, categories, onDone, onCancel, onStale, onGone, onSignedOut }: {
  item: PantryItem
  categories: CategoryInfo[]
  onDone: () => void
  onCancel: () => void
  onStale: () => void
  onGone: (message: string) => void
  onSignedOut: (notice: string) => void
}) {
  const dialog = useRef<HTMLDialogElement>(null)
  const [base, setBase] = useState(item)
  const [amount, setAmount] = useState(String(item.amount))
  const [unit, setUnit] = useState<Unit>(item.unit)
  const [category, setCategory] = useState<Category>(item.category)
  const [expiryDate, setExpiryDate] = useState(item.expiryDate)
  const [expiryTouched, setExpiryTouched] = useState(false)
  const [deleting, setDeleting] = useState(false)
  const [reason, setReason] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [changes, setChanges] = useState<string | null>(null)
  const fields = useFieldErrors('edit', fieldOrder)

  // The dialog leaves the top layer when it is removed from the page, so no close() on unmount.
  useEffect(() => dialog.current?.showModal(), [])

  // The delete button is replaced by the confirming one, so the focus moves to the reason first.
  const { focus } = fields
  useEffect(() => {
    if (deleting) focus('reason')
  }, [deleting, focus])

  // An empty amount is invalid input for the backend to report, never a decrease to zero.
  const decreasing = deleting || (amount.trim() !== '' && roundToThousandths(toBase(Number(amount), unit)) < base.quantity)
  const estimateShown = base.expiryEstimated && !expiryTouched

  function startFrom(fresh: PantryItem) {
    setBase(fresh)
    setAmount(String(fresh.amount))
    setUnit(fresh.unit)
    setCategory(fresh.category)
    setExpiryDate(fresh.expiryDate)
    setExpiryTouched(false)
    setDeleting(false)
    setReason('')
  }

  async function submit(event: FormEvent) {
    event.preventDefault()
    setBusy(true)
    setError(null)
    setChanges(null)
    fields.setErrors({})
    try {
      await api.updatePantryItem(base.id, {
        amount: deleting ? 0 : amount.trim() === '' ? null : Number(amount),
        unit,
        category,
        // An untouched estimate stays an estimate (a category change re-estimates it); a cleared date too.
        expiryDate: estimateShown ? null : expiryDate || null,
        reason: decreasing ? reason || null : null,
        version: base.version,
      })
      onDone()
    } catch (caught) {
      await handleError(caught)
    } finally {
      setBusy(false)
    }
  }

  async function handleError(caught: unknown) {
    const fail = failWith(onSignedOut, setError)
    if (!(caught instanceof ApiError) || caught.status === 401) {
      fail(caught)
    } else if (caught.code === 'PANTRY_ITEM_NOT_FOUND') {
      onGone(caught.message)
    } else if (caught.code === 'PANTRY_ITEM_MODIFIED') {
      setError(caught.message)
      onStale()
      try {
        const fresh = (await api.pantryItems(noFilter)).find((i) => i.id === base.id)
        if (!fresh) {
          onGone(messageFor('PANTRY_ITEM_NOT_FOUND', undefined))
          return
        }
        setChanges(describeChanges(base, fresh))
        startFrom(fresh)
      } catch (reloadFailure) {
        fail(reloadFailure)
      }
    } else {
      setError(caught.message)
      fields.setErrors(caught.errors)
    }
  }

  return (
    <dialog ref={dialog} aria-labelledby="edit-heading" onClose={onCancel}>
      <form onSubmit={submit} noValidate>
        <h2 id="edit-heading">{base.ingredientName} módosítása</h2>

        {!deleting && (
          <>
            <div className="row">
              <div className="field">
                <label htmlFor="edit-amount">Mennyiség</label>
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
                <label htmlFor="edit-unit">Mértékegység</label>
                <select {...fields.props('unit')} value={unit} onChange={(e) => setUnit(e.target.value as Unit)}>
                  {unitsOf[dimensionOf(base.unit)].map((u) => <option key={u} value={u}>{u}</option>)}
                </select>
              </div>
            </div>
            {fields.error('amount')}
            {fields.error('unit')}

            <label htmlFor="edit-category">Kategória</label>
            <select {...fields.props('category')} value={category} onChange={(e) => setCategory(e.target.value as Category)}>
              {categories.map((c) => <option key={c.category} value={c.category}>{categoryName(c.category)}</option>)}
            </select>
            {fields.error('category')}

            <label htmlFor="edit-expiryDate">Lejárat</label>
            <input
              {...fields.props('expiryDate', estimateShown ? 'edit-expiry-hint' : undefined)}
              type="date"
              value={expiryDate}
              onChange={(e) => {
                setExpiryDate(e.target.value)
                setExpiryTouched(true)
              }}
            />
            {fields.error('expiryDate') || (estimateShown && (
              <p id="edit-expiry-hint" className="hint">Becsült dátum: ha nem módosítod, kategóriaváltáskor újraszámoljuk.</p>
            ))}
          </>
        )}

        {decreasing && (
          <>
            <label htmlFor="edit-reason">Miért csökken?</label>
            <select {...fields.props('reason')} value={reason} onChange={(e) => setReason(e.target.value)}>
              <option value="">Válassz okot</option>
              {reasons.map((r) => <option key={r.value} value={r.value}>{r.label}</option>)}
            </select>
            {fields.error('reason')}
          </>
        )}

        <div role="alert" className="form-error">
          {error && <p>{error}</p>}
          {changes && <p className="changes">{changes}</p>}
        </div>

        <div className="actions">
          {deleting ? (
            <button type="submit" className="danger" disabled={busy}>Törlés</button>
          ) : (
            <>
              <button type="submit" disabled={busy}>Mentés</button>
              <button type="button" className="danger" onClick={() => setDeleting(true)}>Törlés</button>
            </>
          )}
          <button type="button" className="secondary" onClick={onCancel}>Mégse</button>
        </div>
      </form>
    </dialog>
  )
}
