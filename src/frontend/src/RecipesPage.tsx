import { useEffect, useId, useRef, useState } from 'react'
import { api, failWith, type Recipe } from './api/client'
import { matchesSearch } from './format'
import { cookingRoute, routes } from './useHashRoute'

type Load = { status: 'loading' } | { status: 'error'; message: string } | { status: 'ready'; recipes: Recipe[] }

// US-3, N9: the household's recipes (system and own, without archived ones), each with a way to cook it and
// to archive it after the browser's own confirmation. notice: the message of the form that opened the page.
export function RecipesPage({ notice: openingNotice, onSignedOut }: { notice?: string | null; onSignedOut: (notice: string) => void }) {
  const [load, setLoad] = useState<Load>({ status: 'loading' })
  const [tries, setTries] = useState(0)
  const [notice, setNotice] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const noticeRef = useRef<HTMLParagraphElement>(null)

  useEffect(() => {
    let active = true
    const fail = failWith(onSignedOut, (message) => setLoad({ status: 'error', message }))
    api.recipes()
      .then((recipes) => {
        if (!active) return
        setLoad({ status: 'ready', recipes })
        // Review S5: the live region was empty until now, so the saved-recipe text is announced.
        if (openingNotice) setNotice(openingNotice)
      })
      .catch((caught: unknown) => active && fail(caught))
    return () => {
      active = false
    }
  }, [tries, onSignedOut, openingNotice])

  async function archive(recipe: Recipe) {
    const question = `Archiválod ezt a receptet: ${recipe.name}? Nem ajánlom többé, és nem főzhető. A korábbi főzéseid megmaradnak. Ezt nem lehet visszavonni.`
    if (!window.confirm(question)) return
    setBusy(true)
    setError(null)
    try {
      await api.archiveRecipe(recipe.id)
      setLoad({ status: 'ready', recipes: await api.recipes() })
      setNotice(`Archiváltam: ${recipe.name}.`)
      // The card and its button are gone; the focus goes to the notice (ux_flows a11y).
      noticeRef.current?.focus()
    } catch (caught) {
      failWith(onSignedOut, setError)(caught)
    } finally {
      setBusy(false)
    }
  }

  return (
    <main>
      <div className="page-head">
        <h1 tabIndex={-1}>Receptek</h1>
        <a href={routes.newRecipe} className="button-link">Új recept</a>
      </div>
      <p ref={noticeRef} role="status" className="notice" tabIndex={-1}>{notice}</p>
      <p role="alert" className="form-error">{error}</p>
      {load.status === 'loading' && <p>Betöltés…</p>}
      {load.status === 'error' && (
        <div>
          <p role="alert">{load.message}</p>
          <button
            type="button"
            onClick={() => {
              setLoad({ status: 'loading' })
              setTries((n) => n + 1)
            }}
          >
            Újra
          </button>
        </div>
      )}
      {load.status === 'ready' && load.recipes.length === 0 && (
        <p className="empty">Nincs recepted. Vegyél fel egyet az „Új recept” gombbal.</p>
      )}
      {load.status === 'ready' && load.recipes.length > 0 && (
        <RecipeList recipes={load.recipes} busy={busy} onArchive={(r) => void archive(r)} />
      )}
    </main>
  )
}

// v0.3.1: the search filters the loaded recipes in the browser, by name and ingredients.
function RecipeList({ recipes, busy, onArchive }: { recipes: Recipe[]; busy: boolean; onArchive: (recipe: Recipe) => void }) {
  const [search, setSearch] = useState('')
  const shown = recipes.filter((r) => matchesSearch(search, r.name, ...r.ingredients))
  return (
    <div className="split">
      <form role="search" className="filters panel side" onSubmit={(e) => e.preventDefault()}>
        <label htmlFor="recipe-search">Keresés</label>
        <input id="recipe-search" type="search" aria-describedby="recipe-search-hint" value={search} onChange={(e) => setSearch(e.target.value)} />
        <p id="recipe-search-hint" className="hint">A recept nevében és a hozzávalóiban keresek.</p>
      </form>
      <div className="content">
        {shown.length === 0
          ? <p className="empty">Nincs a keresésnek megfelelő recept.</p>
          : <div className="panel">{shown.map((r) => <RecipeCard key={r.id} recipe={r} busy={busy} onArchive={() => onArchive(r)} />)}</div>}
      </div>
    </div>
  )
}

function RecipeCard({ recipe, busy, onArchive }: { recipe: Recipe; busy: boolean; onArchive: () => void }) {
  const headingId = useId()
  return (
    <article aria-labelledby={headingId} className="recipe">
      <div className="recipe-head">
        <h2 id={headingId}>{recipe.name}</h2>
        <span className="leader" aria-hidden="true" />
        <span className="serv">{recipe.servings} adag</span>
      </div>
      {recipe.own && <p><span className="badge neutral">Saját recept</span></p>}
      <p className="hint">Hozzávalók: {recipe.ingredients.join(', ')}</p>
      <div className="actions">
        <a href={cookingRoute(recipe.id)} className="button-link">Megnyitás</a>
        <button type="button" className="secondary" aria-label={`Archiválás: ${recipe.name}`} disabled={busy} onClick={onArchive}>
          Archiválás
        </button>
      </div>
    </article>
  )
}
