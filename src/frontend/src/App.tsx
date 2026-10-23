import { useCallback, useEffect, useState } from 'react'
import { api, errorMessage, type Me } from './api/client'
import { AuthForm } from './AuthForm'
import { CookingPage } from './CookingPage'
import { Layout } from './Layout'
import { PantryPage } from './PantryPage'
import { RecipeForm } from './RecipeForm'
import { RecipesPage } from './RecipesPage'
import { RecommendationsPage } from './RecommendationsPage'
import { ShoppingListPage } from './ShoppingListPage'
import { cookingRecipeId, routes, useHashRoute, type Route } from './useHashRoute'

// Signed out: the auth screens; signed in: the pages chosen by the hash route (useHashRoute).
type State =
  | { screen: 'loading' }
  | { screen: 'error'; message: string }
  | { screen: 'login' | 'register'; notice?: string }
  | { screen: 'pantry'; me: Me }

async function loadSession(): Promise<State> {
  try {
    const me = await api.me()
    return me ? { screen: 'pantry', me } : { screen: 'login' }
  } catch (caught) {
    return { screen: 'error', message: errorMessage(caught) }
  }
}

function Page({ route, onSignedOut, recipeNotice, onRecipeSaved }: {
  route: Route
  onSignedOut: (notice: string) => void
  // The new recipe form's message, shown once on the Receptek page it opens.
  recipeNotice: string | null
  onRecipeSaved: (notice: string) => void
}) {
  const recipeId = cookingRecipeId(route)
  // key: another recipe is a new cooking screen with its own state.
  if (recipeId) return <CookingPage key={recipeId} recipeId={recipeId} onSignedOut={onSignedOut} />
  if (route === routes.recommendations) return <RecommendationsPage onSignedOut={onSignedOut} />
  if (route === routes.shopping) return <ShoppingListPage onSignedOut={onSignedOut} />
  if (route === routes.recipes) return <RecipesPage notice={recipeNotice} onSignedOut={onSignedOut} />
  if (route === routes.newRecipe) return <RecipeForm onSaved={onRecipeSaved} onSignedOut={onSignedOut} />
  return <PantryPage onSignedOut={onSignedOut} />
}

export default function App() {
  const [state, setState] = useState<State>({ screen: 'loading' })
  const route = useHashRoute()
  // Stable, so the pantry page does not reload its data whenever App renders.
  const signOut = useCallback((notice?: string) => setState({ screen: 'login', notice }), [])
  const [recipeNotice, setRecipeNotice] = useState<string | null>(null)
  // Shown once: leaving the Receptek page drops it.
  const [noticeRoute, setNoticeRoute] = useState(route)
  if (route !== noticeRoute) {
    setNoticeRoute(route)
    if (route !== routes.recipes) setRecipeNotice(null)
  }

  useEffect(() => {
    let active = true
    void loadSession().then((next) => {
      if (active) setState(next)
    })
    return () => {
      active = false
    }
  }, [])

  switch (state.screen) {
    case 'loading':
      return <main aria-busy="true"><p>Betöltés…</p></main>
    case 'error':
      return (
        <main>
          <h1>Kamra</h1>
          <p role="alert">{state.message}</p>
          <button
            type="button"
            onClick={() => {
              setState({ screen: 'loading' })
              void loadSession().then(setState)
            }}
          >
            Újra
          </button>
        </main>
      )
    case 'pantry':
      return (
        <Layout me={state.me} route={route} onSignedOut={signOut}>
          <Page
            route={route}
            onSignedOut={signOut}
            recipeNotice={recipeNotice}
            onRecipeSaved={(notice) => {
              setRecipeNotice(notice)
              window.location.hash = routes.recipes
            }}
          />
        </Layout>
      )
    default:
      return (
        <AuthForm
          key={state.screen}
          mode={state.screen}
          notice={state.notice}
          onSignedIn={(me) => setState({ screen: 'pantry', me })}
          onSwitchMode={() => setState({ screen: state.screen === 'login' ? 'register' : 'login' })}
        />
      )
  }
}
