import { useEffect, useId, useRef, useState } from 'react'
import { api, failWith, type Recipe } from './api/client'
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
      <h1 tabIndex={-1}>Receptek</h1>
      <p><a href={routes.newRecipe} className="button-link">Új recept</a></p>
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
      {load.status === 'ready' && load.recipes.map((r) => <RecipeCard key={r.id} recipe={r} busy={busy} onArchive={() => void archive(r)} />)}
    </main>
  )
}

function RecipeCard({ recipe, busy, onArchive }: { recipe: Recipe; busy: boolean; onArchive: () => void }) {
  const headingId = useId()
  return (
    <article aria-labelledby={headingId} className="recipe">
      <h2 id={headingId}>{recipe.name}</h2>
      <p>{recipe.servings} adag</p>
      {recipe.own && <p>Saját recept</p>}
      <p>Hozzávalók: {recipe.ingredients.join(', ')}</p>
      <div className="actions">
        <a href={cookingRoute(recipe.id)} className="button-link">Megfőztem</a>
        <button type="button" className="secondary" aria-label={`Archiválás: ${recipe.name}`} disabled={busy} onClick={onArchive}>
          Archiválás
        </button>
      </div>
    </article>
  )
}
