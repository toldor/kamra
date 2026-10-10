import { useCallback, useEffect, useState } from 'react'
import { api, errorMessage, type Me } from './api/client'
import { AuthForm } from './AuthForm'
import { PantryPage } from './PantryPage'

// Three screens and no URLs yet, so plain state instead of a router (added with US-1).
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

export default function App() {
  const [state, setState] = useState<State>({ screen: 'loading' })
  // Stable, so the pantry page does not reload its data whenever App renders.
  const signOut = useCallback((notice?: string) => setState({ screen: 'login', notice }), [])

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
      return <PantryPage me={state.me} onSignedOut={signOut} />
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
