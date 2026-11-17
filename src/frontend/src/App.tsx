import { useCallback, useEffect, useState, type Dispatch, type SetStateAction } from 'react'
import { api, errorMessage, type Me } from './api/client'
import { AuthForm } from './AuthForm'
import { ChatPage } from './ChatPage'
import { newConversation, type Conversation } from './conversation'
import { CookingPage } from './CookingPage'
import { Layout } from './Layout'
import { PantryPage } from './PantryPage'
import { ProfilePage } from './ProfilePage'
import { RecipeForm } from './RecipeForm'
import { RecipesPage } from './RecipesPage'
import { RecommendationsPage } from './RecommendationsPage'
import { ShoppingListPage } from './ShoppingListPage'
import { cookingRecipeId, notFoundRoute, pageTitle, routes, useHashRoute, type Route } from './useHashRoute'

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

// ui_polish_minispec 7.: an unknown address.
function NotFoundPage() {
  return (
    <main className="narrow">
      <h1 tabIndex={-1}>Nincs ilyen oldal</h1>
      <p>A megnyitott cím nem létezik a Kamrában.</p>
      <p><a href={routes.pantry}>Ugrás a Készlethez</a></p>
    </main>
  )
}

function Page({ me, route, onSignedOut, recipeNotice, onRecipeSaved, conversation, onConversationChange }: {
  me: Me
  route: Route
  onSignedOut: (notice?: string) => void
  // The Kérdezz conversation outlives the page (l3_l5_minispec L5).
  conversation: Conversation
  onConversationChange: Dispatch<SetStateAction<Conversation>>
  // The new recipe form's message, shown once on the Receptek page it opens.
  recipeNotice: string | null
  onRecipeSaved: (notice: string) => void
}) {
  const recipeId = cookingRecipeId(route)
  // key: another recipe is a new cooking screen with its own state.
  if (recipeId) return <CookingPage key={recipeId} recipeId={recipeId} onSignedOut={onSignedOut} />
  if (route === routes.recommendations) return <RecommendationsPage onSignedOut={onSignedOut} />
  if (route === routes.shopping) return <ShoppingListPage onSignedOut={onSignedOut} />
  if (route === routes.chat) return <ChatPage conversation={conversation} onChange={onConversationChange} onSignedOut={onSignedOut} />
  if (route === routes.recipes) return <RecipesPage notice={recipeNotice} onSignedOut={onSignedOut} />
  if (route === routes.newRecipe) return <RecipeForm onSaved={onRecipeSaved} onSignedOut={onSignedOut} />
  if (route === routes.profile) return <ProfilePage me={me} onSignedOut={onSignedOut} />
  if (route === notFoundRoute) return <NotFoundPage />
  return <PantryPage onSignedOut={onSignedOut} />
}

export default function App() {
  const [state, setState] = useState<State>({ screen: 'loading' })
  const route = useHashRoute()
  // In memory only; signing out (or an expired session) starts a new one, so the next user never sees it.
  const [conversation, setConversation] = useState(newConversation)
  // Stable, so the pantry page does not reload its data whenever App renders.
  const signOut = useCallback((notice?: string) => {
    setConversation(newConversation())
    setState({ screen: 'login', notice })
  }, [])
  const [recipeNotice, setRecipeNotice] = useState<string | null>(null)
  // Shown once: leaving the Receptek page drops it.
  const [noticeRoute, setNoticeRoute] = useState(route)
  if (route !== noticeRoute) {
    setNoticeRoute(route)
    if (route !== routes.recipes) setRecipeNotice(null)
  }

  // ui_polish_minispec 6.: the browser tab names the page.
  useEffect(() => {
    document.title = state.screen === 'pantry' ? pageTitle(route)
      : state.screen === 'login' ? 'Bejelentkezés – Kamra'
        : state.screen === 'register' ? 'Regisztráció – Kamra' : 'Kamra'
  }, [state.screen, route])

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
      return <main className="narrow" aria-busy="true"><p>Betöltés…</p></main>
    case 'error':
      return (
        <main className="narrow">
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
            me={state.me}
            route={route}
            onSignedOut={signOut}
            conversation={conversation}
            onConversationChange={setConversation}
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
