import { useEffect, useRef, useState } from 'react'
import { api, failWith, type Unit } from './api/client'
import { formatListLine } from './format'
import { routes } from './useHashRoute'

export type ShortageLine = { ingredientId: string; ingredientName: string; amount: number; unit: Unit }

// US-3 / US-4: one click puts every shortage on the shopping list, one request per line (K1). Adding adds
// up (P12a), so after a failure only the lines not yet saved are sent again. Once done, the button gives
// way to what was added for as long as the screen is open, even if the shortage changes meanwhile; pressing
// it again after a reload adds the amounts again (K9, K10, Ismert korlát).
export function ShortageToList({ label, lines, showLink, onSignedOut }: {
  label: string
  lines: ShortageLine[]
  showLink: boolean
  onSignedOut: (notice: string) => void
}) {
  const [saved, setSaved] = useState<ReadonlySet<string>>(new Set())
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [added, setAdded] = useState<string | null>(null)
  const status = useRef<HTMLParagraphElement>(null)

  // The pressed button gives way to the result, so the focus goes there (ux_flows a11y).
  useEffect(() => {
    if (added) status.current?.focus()
  }, [added])

  async function add() {
    setBusy(true)
    setError(null)
    const done = new Set(saved)
    try {
      for (const line of lines.filter((l) => !done.has(l.ingredientId))) {
        await api.addShoppingListItem({ ingredientId: line.ingredientId, amount: line.amount, unit: line.unit })
        done.add(line.ingredientId)
      }
      const names = lines.map((l) => formatListLine(l.ingredientName, { value: l.amount, unit: l.unit }))
      setAdded(`Felvettem a listára: ${names.join(', ')}.`)
    } catch (caught) {
      failWith(onSignedOut, setError)(caught)
    } finally {
      setSaved(done)
      setBusy(false)
    }
  }

  // The live region is always there, so a screen reader announces the result when its text appears.
  return (
    <div className="actions">
      <p ref={status} role="status" className="notice" tabIndex={-1}>{added}</p>
      {added && showLink && <a href={routes.shopping} className="button-link">Megnézem</a>}
      {!added && error && <p role="alert" className="form-error">{error}</p>}
      {!added && lines.length > 0 && (
        <button type="button" className="secondary" onClick={add} disabled={busy}>{busy ? 'Kérlek, várj…' : label}</button>
      )}
    </div>
  )
}
