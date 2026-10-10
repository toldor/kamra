import { useEffect, useState } from 'react'
import { api, ApiError, errorMessage, failWith, noFilter, type Category, type CategoryInfo, type Ingredient, type Me, type PantryItem } from './api/client'
import { categoryName, formatAmount, formatDate } from './format'
import { PantryItemEditor } from './PantryItemEditor'
import { PantryItemForm } from './PantryItemForm'

type List = { status: 'loading' } | { status: 'error'; message: string } | { status: 'ready'; items: PantryItem[] }
type Catalog =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; categories: CategoryInfo[]; ingredients: Ingredient[] }

// US-1: the pantry with its filters, the new item form and the item editor. Expired and soon-expiring
// items come first, because the backend orders by expiry date.
export function PantryPage({ me, onSignedOut }: { me: Me; onSignedOut: (notice?: string) => void }) {
  const [filter, setFilter] = useState(noFilter)
  const [searchInput, setSearchInput] = useState('')
  const [list, setList] = useState<List>({ status: 'loading' })
  const [reloads, setReloads] = useState(0)
  const [catalog, setCatalog] = useState<Catalog>({ status: 'loading' })
  const [catalogTries, setCatalogTries] = useState(0)
  const [editing, setEditing] = useState<PantryItem | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [logoutError, setLogoutError] = useState<string | null>(null)

  const reload = () => setReloads((n) => n + 1)

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
      .then((items) => active && setList({ status: 'ready', items }))
      .catch((caught: unknown) => active && fail(caught))
    return () => {
      active = false
    }
  }, [filter, reloads, onSignedOut])

  async function logout() {
    setBusy(true)
    setLogoutError(null)
    try {
      await api.logout()
      onSignedOut()
    } catch (caught) {
      // 401: the session had already ended - the user wanted to be signed out, and is.
      if (caught instanceof ApiError && caught.status === 401) {
        onSignedOut()
        return
      }
      setLogoutError(errorMessage(caught))
      setBusy(false)
    }
  }

  const filtered = filter.search.trim() !== '' || filter.category !== '' || filter.expiringSoon
  const categories = catalog.status === 'ready' ? catalog.categories : []

  return (
    <main>
      <header className="topbar">
        <span>{me.email}</span>
        <button type="button" onClick={logout} disabled={busy}>Kijelentkezés</button>
      </header>
      <div role="alert" className="form-error">{logoutError}</div>
      <h1>Kamra</h1>
      <p role="status" className="notice">{notice}</p>

      {catalog.status === 'error' && (
        <div>
          <p role="alert">{catalog.message}</p>
          <button type="button" onClick={() => setCatalogTries((n) => n + 1)}>Újra</button>
        </div>
      )}
      {catalog.status === 'ready' && (
        <PantryItemForm
          categories={catalog.categories}
          ingredients={catalog.ingredients}
          onAdded={() => {
            setNotice(null)
            reload()
          }}
          onSignedOut={onSignedOut}
        />
      )}

      <section aria-labelledby="pantry-heading">
        <h2 id="pantry-heading">Készlet</h2>
        <form role="search" className="filters" onSubmit={(e) => e.preventDefault()}>
          <label htmlFor="filter-search">Keresés</label>
          <input
            id="filter-search"
            type="search"
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
          />
          <label htmlFor="filter-category">Kategória</label>
          <select
            id="filter-category"
            value={filter.category}
            onChange={(e) => setFilter({ ...filter, category: e.target.value as Category | '' })}
          >
            <option value="">Mind</option>
            {categories.map((c) => <option key={c.category} value={c.category}>{categoryName(c.category)}</option>)}
          </select>
          <label className="check">
            <input
              type="checkbox"
              checked={filter.expiringSoon}
              onChange={(e) => setFilter({ ...filter, expiringSoon: e.target.checked })}
            />
            Csak a hamarosan lejárók
          </label>
        </form>

        {list.status === 'loading' && <p role="status">Betöltés…</p>}
        {list.status === 'error' && (
          <div>
            <p role="alert">{list.message}</p>
            <button type="button" onClick={reload}>Újra</button>
          </div>
        )}
        {list.status === 'ready' && list.items.length === 0 && (filtered
          ? <p className="empty">Nincs a szűrésnek megfelelő tétel.</p>
          : (
            <>
              <p className="empty">Még üres a kamrád.</p>
              <p>Rögzítsd az első tételt az »Új tétel« űrlappal.</p>
            </>
          ))}
        {list.status === 'ready' && list.items.length > 0 && (
          <ul className="items">
            {list.items.map((item) => (
              <li key={item.id} aria-labelledby={`item-${item.id}`} className="item">
                <span id={`item-${item.id}`} className="item-name">{item.ingredientName}</span>
                <span>{formatAmount(item.amount, item.unit)}</span>
                <span>
                  {formatDate(item.expiryDate)}
                  {item.expiryEstimated && ' (becsült)'}
                </span>
                {item.expired && <span className="badge expired">Lejárt</span>}
                {item.soonExpiring && <span className="badge soon">Hamarosan lejár</span>}
                <button
                  type="button"
                  className="secondary"
                  aria-label={`Módosítás: ${item.ingredientName}`}
                  onClick={() => setEditing(item)}
                >
                  Módosítás
                </button>
              </li>
            ))}
          </ul>
        )}
      </section>

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
