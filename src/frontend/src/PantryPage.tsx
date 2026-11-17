import { useEffect, useRef, useState } from 'react'
import { flushSync } from 'react-dom'
import { api, failWith, noFilter, type Category, type CategoryInfo, type Ingredient, type ItemProposal, type PantryItem } from './api/client'
import { categoryName, formatAmount, formatDate } from './format'
import { Icon } from './Icon'
import { LoadingRows } from './Loading'
import { PantryItemEditor } from './PantryItemEditor'
import { PantryItemForm } from './PantryItemForm'
import { ProposalList, QuickEntryForm } from './QuickEntry'
import { routes } from './useHashRoute'

// No search, category or soon-expiring filter: the list is the whole pantry.
const isWhole = (filter: typeof noFilter) => filter.search.trim() === '' && filter.category === '' && !filter.expiringSoon

// ui_polish_minispec 15. (A1): the counts of the whole pantry; "lejárt" and "hamarosan lejár" switch their filter.
function PantrySummary({ totals, expiredOnly, soonOnly, onExpired, onSoon }: {
  totals: PantryItem[]
  expiredOnly: boolean
  soonOnly: boolean
  onExpired: () => void
  onSoon: () => void
}) {
  const expired = totals.filter((i) => i.expired).length
  const soon = totals.filter((i) => i.soonExpiring).length
  return (
    <div role="group" aria-label="Összesítés" className="summary">
      <button type="button" className="chip expired" aria-pressed={expiredOnly} disabled={expired === 0 && !expiredOnly} onClick={onExpired}>
        {expired} lejárt
      </button>
      <button type="button" className="chip soon" aria-pressed={soonOnly} disabled={soon === 0 && !soonOnly} onClick={onSoon}>
        {soon} hamarosan lejár
      </button>
      <span className="chip">{totals.length} tétel</span>
    </div>
  )
}

// ui_polish_minispec 8.: the stripe of the most urgent state; the badges and "(becsült)" carry the meaning.
const expiryStripe = (item: PantryItem) => (item.expired ? 'expired' : item.soonExpiring ? 'soon' : item.expiryEstimated ? 'estimated' : '')

type List = { status: 'loading' } | { status: 'error'; message: string } | { status: 'ready'; items: PantryItem[] }
type Catalog =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; categories: CategoryInfo[]; ingredients: Ingredient[] }

// US-1: the pantry with its filters, the new item form and the item editor. Expired and soon-expiring
// items come first, because the backend orders by expiry date.
export function PantryPage({ onSignedOut }: { onSignedOut: (notice: string) => void }) {
  const [filter, setFilter] = useState(noFilter)
  const [byCategory, setByCategory] = useState(false)
  const [searchInput, setSearchInput] = useState('')
  const [list, setList] = useState<List>({ status: 'loading' })
  const [reloads, setReloads] = useState(0)
  const [catalog, setCatalog] = useState<Catalog>({ status: 'loading' })
  const [catalogTries, setCatalogTries] = useState(0)
  const [editing, setEditing] = useState<PantryItem | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  // US-2: the quick entry shows only when the backend says it is on; any failure keeps it hidden (the manual
  // form always works). Each sentence gets a new proposal list (batch), so its heading takes the focus again.
  const [quickEntry, setQuickEntry] = useState(false)
  const [proposals, setProposals] = useState<ItemProposal[]>([])
  const [batch, setBatch] = useState(0)
  // ui_polish_minispec 15.: the counts of the whole pantry, and the expired filter in the browser.
  const [totals, setTotals] = useState<PantryItem[] | null>(null)
  const [expiredOnly, setExpiredOnly] = useState(false)
  // ui_polish_minispec 17.: the folding cards; "Új tétel" opens when there is no sentence entry.
  const [quickOpen, setQuickOpen] = useState(true)
  const [newItemOpen, setNewItemOpen] = useState<boolean | null>(null)
  const side = useRef<HTMLDivElement>(null)

  const reload = () => setReloads((n) => n + 1)

  useEffect(() => {
    let active = true
    api.itemProposalAvailability()
      .then((availability) => active && setQuickEntry(availability.enabled))
      .catch(() => active && setQuickEntry(false))
    return () => {
      active = false
    }
  }, [])

  useEffect(() => {
    let active = true
    const fail = failWith(onSignedOut, (message) => setCatalog({ status: 'error', message }))
    Promise.all([api.categories(), api.ingredients()])
      .then(([categories, ingredients]) => active && setCatalog({ status: 'ready', categories, ingredients }))
      .catch((caught: unknown) => active && fail(caught))
    return () => {
      active = false
    }
  }, [catalogTries, onSignedOut])

  // Typing in the search box waits a moment, so not every keystroke is a request.
  useEffect(() => {
    const timer = setTimeout(
      () => setFilter((current) => (current.search === searchInput ? current : { ...current, search: searchInput })), 250)
    return () => clearTimeout(timer)
  }, [searchInput])

  useEffect(() => {
    let active = true
    const fail = failWith(onSignedOut, (message) => setList({ status: 'error', message }))
    api.pantryItems(filter)
      .then((items) => {
        if (!active) return
        setList({ status: 'ready', items })
        if (isWhole(filter)) setTotals(items)
      })
      .catch((caught: unknown) => active && fail(caught))
    return () => {
      active = false
    }
  }, [filter, reloads, onSignedOut])

  // Q17 (a): while a filter is on, the summary still counts the whole pantry - one more request, only then.
  const whole = isWhole(filter)
  useEffect(() => {
    if (whole) return
    let active = true
    api.pantryItems(noFilter)
      .then((items) => active && setTotals(items))
      .catch(() => {})
    return () => {
      active = false
    }
  }, [whole, reloads])

  // Q21 (b): the side column stays in view only when it fits on the screen.
  useEffect(() => {
    const element = side.current
    if (!element) return
    const fit = () => element.classList.toggle('fits', element.offsetHeight + 96 <= window.innerHeight)
    fit()
    window.addEventListener('resize', fit)
    const observer = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(fit)
    observer?.observe(element)
    return () => {
      window.removeEventListener('resize', fit)
      observer?.disconnect()
    }
  }, [])

  const filtered = filter.search.trim() !== '' || filter.category !== '' || filter.expiringSoon || expiredOnly
  const shown = list.status === 'ready' ? (expiredOnly ? list.items.filter((i) => i.expired) : list.items) : []
  const categories = catalog.status === 'ready' ? catalog.categories : []


  // ui_polish_minispec 14.: by category - the groups in the order of the categories (any other category after them),
  // each item in its expiry order from the server.
  const groups = (items: PantryItem[]): [Category, PantryItem[]][] =>
    [...new Set([...categories.map((c) => c.category), ...items.map((i) => i.category)])]
      .map((category): [Category, PantryItem[]] => [category, items.filter((i) => i.category === category)])
      .filter(([, inGroup]) => inGroup.length > 0)

  const itemRow = (item: PantryItem) => (
    <li key={item.id} aria-labelledby={`item-${item.id}`} className={`item cols ${expiryStripe(item)}`}>
      <span className="line">
        <span id={`item-${item.id}`} className="name">{item.ingredientName}</span>
        <span className="leader" aria-hidden="true" />
        <span className="qty">{formatAmount(item.amount, item.unit)}</span>
      </span>
      <span className="meta">
        <span>
          {formatDate(item.expiryDate)}
          {item.expiryEstimated && ' (becsült)'}
        </span>
        <span>
          {item.expired && <span className="badge expired">Lejárt</span>}
          {item.soonExpiring && <span className="badge soon">Hamarosan lejár</span>}
        </span>
      </span>
      <button
        type="button"
        className="secondary"
        aria-label={`Módosítás: ${item.ingredientName}`}
        onClick={() => setEditing(item)}
      >
        Módosítás
      </button>
    </li>
  )

  return (
    <main>
      <h1 tabIndex={-1}>Készlet</h1>
      <p role="status" className="notice">{notice}</p>

      {/* C3: the sentence panel, the filters, the new item form and the hint beside the list; stacked on a phone (C2). */}
      <div className="split">
        <div ref={side} className="side">
          {/* us2_minispec K13: at the top of the side column. */}
          {quickEntry && (
            <QuickEntryForm
              open={quickOpen}
              onToggle={setQuickOpen}
              onProposed={(items) => {
                setNotice(null)
                setProposals(items)
                setBatch((n) => n + 1)
              }}
              onManual={() => {
                // The field inside a closed card cannot take the focus, so the card opens first.
                flushSync(() => setNewItemOpen(true))
                document.getElementById('new-ingredientId')?.focus()
              }}
              onSignedOut={onSignedOut}
            />
          )}
          {catalog.status === 'loading' && <p role="status">Betöltés…</p>}
          {catalog.status === 'error' && (
            <div>
              <p role="alert">{catalog.message}</p>
              <button
                type="button"
                onClick={() => {
                  // A retry shows the loading state at once, so the old error does not linger.
                  setCatalog({ status: 'loading' })
                  setCatalogTries((n) => n + 1)
                }}
              >
                Újra
              </button>
            </div>
          )}
          {catalog.status === 'ready' && (
            <PantryItemForm
              open={newItemOpen ?? !quickEntry}
              onToggle={setNewItemOpen}
              categories={catalog.categories}
              ingredients={catalog.ingredients}
              onAdded={(item) => {
                // L1 (MT-03): the live region says what went in (the quick entry's text for one item).
                setNotice(`Felvettem a kamrádba: ${item.ingredientName} ${formatAmount(item.amount, item.unit)}.`)
                reload()
              }}
              onIngredientCreated={(ingredient) =>
                setCatalog((current) => (current.status === 'ready' ? { ...current, ingredients: [...current.ingredients, ingredient] } : current))}
              onSignedOut={onSignedOut}
            />
          )}

          <p className="hint">
            Minimumszintet a <a href={routes.shopping}>Bevásárlólista</a> oldalon állíthatsz be: ha a készlet az alá csökken, felveszem a bevásárlójavaslatok közé.
          </p>
        </div>

        <section aria-labelledby="pantry-heading" className="content">
          {catalog.status === 'ready' && proposals.length > 0 && (
            <ProposalList
              key={batch}
              proposals={proposals}
              ingredients={catalog.ingredients}
              categories={catalog.categories}
              onIngredientCreated={(ingredient) =>
                setCatalog((current) => (current.status === 'ready' ? { ...current, ingredients: [...current.ingredients, ingredient] } : current))}
              onApproved={reload}
              onNotice={setNotice}
              onSignedOut={onSignedOut}
            />
          )}
          {totals !== null && totals.length > 0 && (
            <PantrySummary
              totals={totals}
              expiredOnly={expiredOnly}
              soonOnly={filter.expiringSoon}
              onExpired={() => {
                setExpiredOnly((on) => !on)
                setFilter((current) => (current.expiringSoon ? { ...current, expiringSoon: false } : current))
              }}
              onSoon={() => {
                setExpiredOnly(false)
                setFilter((current) => ({ ...current, expiringSoon: !current.expiringSoon }))
              }}
            />
          )}
          <div className="panel">
            <h2 id="pantry-heading">Tételek</h2>
              <form role="search" className="filters filterbar" onSubmit={(e) => e.preventDefault()}>
                <div className="field search">
                  <label htmlFor="filter-search">Keresés</label>
                  <input
                    id="filter-search"
                    type="search"
                    value={searchInput}
                    onChange={(e) => setSearchInput(e.target.value)}
                  />
                </div>
                <div className="field">
                  <label htmlFor="filter-category">Kategória</label>
                  <select
                    id="filter-category"
                    value={filter.category}
                    onChange={(e) => setFilter({ ...filter, category: e.target.value as Category | '' })}
                  >
                    <option value="">Mind</option>
                    {categories.map((c) => <option key={c.category} value={c.category}>{categoryName(c.category)}</option>)}
                  </select>
                </div>
                {/* ui_polish_minispec 14.: in the browser only; not kept over a reload. */}
                <fieldset className="sort">
                  <legend>Rendezés</legend>
                  <label className="check">
                    <input type="radio" name="pantry-sort" checked={!byCategory} onChange={() => setByCategory(false)} />
                    Lejárat szerint
                  </label>
                  <label className="check">
                    <input type="radio" name="pantry-sort" checked={byCategory} onChange={() => setByCategory(true)} />
                    Kategóriák szerint
                  </label>
                </fieldset>
              </form>
            <div className="panel-body">
              {list.status === 'loading' && <LoadingRows />}
              {list.status === 'error' && (
                <div>
                  <p role="alert">{list.message}</p>
                  <button
                    type="button"
                    onClick={() => {
                      setList({ status: 'loading' })
                      reload()
                    }}
                  >
                    Újra
                  </button>
                </div>
              )}
              {list.status === 'ready' && shown.length === 0 && (filtered
                ? <p className="empty">Nincs a szűrésnek megfelelő tétel.</p>
                : (
                  <div className="empty-state">
                    <Icon name="pantry" size={48} />
                    <p className="empty">Még üres a kamrád.</p>
                    <p>
                      {quickEntry
                        ? 'Írd le egy mondatban, mit vettél – például: »vettem 2 liter tejet, 10 tojást és fél kiló paradicsomot, a tej pénteken jár le«.'
                        : 'Rögzítsd az első tételt az »Új tétel« űrlappal.'}
                    </p>
                  </div>
                ))}
            </div>
            {list.status === 'ready' && shown.length > 0 && (byCategory
              ? groups(shown).map(([category, items]) => (
                <details key={category} className="category" open>
                  <summary>{categoryName(category)} ({items.length})</summary>
                  <ul className="items">{items.map(itemRow)}</ul>
                </details>
              ))
              : <ul className="items">{shown.map(itemRow)}</ul>)}
          </div>
        </section>
      </div>

      {editing && (
        <PantryItemEditor
          key={editing.id}
          item={editing}
          categories={categories}
          onDone={() => {
            setEditing(null)
            reload()
          }}
          onCancel={() => setEditing(null)}
          onStale={reload}
          onGone={(message) => {
            setEditing(null)
            setNotice(message)
            reload()
          }}
          onSignedOut={onSignedOut}
        />
      )}
    </main>
  )
}
