import { useEffect, useId, useState } from 'react'
import { api, failWith, type Recommendation, type RecommendationsResponse } from './api/client'
import { formatShortage } from './format'
import { cookingRoute, routes } from './useHashRoute'

type Load = { status: 'loading' } | { status: 'error'; message: string } | { status: 'ready'; data: RecommendationsResponse }

function RecipeCard({ recipe }: { recipe: Recommendation }) {
  const soon = recipe.soonExpiringIngredients
  // The heading names the card; useId keeps the reference valid whatever the data's ids contain.
  const headingId = useId()
  return (
    <article aria-labelledby={headingId} className="recipe">
      <h3 id={headingId}>{recipe.name}</h3>
      <p className="hint">{recipe.servings} adag</p>
      {recipe.shortages.length > 0 ? (
        <ul>{recipe.shortages.map((s) => <li key={s.ingredientName}>{formatShortage(s)}</li>)}</ul>
      ) : (
        <p>{soon.length > 0 ? `${soon.length} hamarosan lejáró hozzávalót használ fel: ${soon.join(', ')}` : 'Minden hozzávaló megvan.'}</p>
      )}
      {recipe.expiredIngredients.length > 0 && (
        <p className="warning">Lejárt tételt is felhasznál: {recipe.expiredIngredients.join(', ')}.</p>
      )}
      <a href={cookingRoute(recipe.recipeId)} className="button-link">Megfőztem</a>
    </article>
  )
}

const recordPantry = <a href={routes.pantry} className="button-link">Készlet rögzítése</a>

// US-3 "Mit főzzek?": makeable recipes first, ranked by the backend (soon-expiring items used,
// earliest expiry, name), then the almost makeable ones with what is missing.
export function RecommendationsPage({ onSignedOut }: { onSignedOut: (notice: string) => void }) {
  const [load, setLoad] = useState<Load>({ status: 'loading' })
  const [tries, setTries] = useState(0)

  useEffect(() => {
    let active = true
    const fail = failWith(onSignedOut, (message) => setLoad({ status: 'error', message }))
    api.recommendations()
      .then((data) => active && setLoad({ status: 'ready', data }))
      .catch((caught: unknown) => active && fail(caught))
    return () => {
      active = false
    }
  }, [tries, onSignedOut])

  return (
    <main>
      <h1 tabIndex={-1}>Mit főzzek?</h1>
      {load.status === 'loading' && <p role="status">Betöltés…</p>}
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
      {load.status === 'ready' && <Suggestions data={load.data} />}
    </main>
  )
}

function Suggestions({ data }: { data: RecommendationsResponse }) {
  // ux_flows, Üres állapotok.
  if (data.pantryIsEmpty) {
    return (
      <>
        <p className="empty">Még nincs mit ajánlanom, mert üres a kamrád. Rögzítsd, mit vettél, és megmutatom, mit főzhetsz belőle.</p>
        {recordPantry}
      </>
    )
  }

  if (data.makeable.length === 0 && data.almostMakeable.length === 0) {
    // D4: the ux_flows text offers an own recipe, which arrives on okt. 23.
    return (
      <>
        <p className="empty">A kamrád alapján most nem találtam receptet. Rögzíts további hozzávalókat, és újra megnézem.</p>
        {recordPantry}
      </>
    )
  }

  return (
    <>
      {data.makeable.length > 0 ? (
        <section aria-labelledby="makeable-heading">
          <h2 id="makeable-heading">Elkészíthető</h2>
          {data.makeable.map((r) => <RecipeCard key={r.recipeId} recipe={r} />)}
        </section>
      ) : (
        <p className="empty">Most egyik receptedhez sincs meg minden hozzávaló.</p>
      )}
      {data.almostMakeable.length > 0 && (
        <section aria-labelledby="almost-heading">
          <h2 id="almost-heading">Majdnem elkészíthető</h2>
          {data.almostMakeable.map((r) => <RecipeCard key={r.recipeId} recipe={r} />)}
        </section>
      )}
    </>
  )
}
