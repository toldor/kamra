import { useState } from 'react'
import { api, ApiError, errorMessage, type Me } from './api/client'

// Walking skeleton: the pantry is always empty until US-1 adds pantry items. The ux_flows empty-state
// text continues with an invitation to the one-sentence entry, which arrives with US-2.
export function PantryPage({ me, onSignedOut }: {
  me: Me
  // notice: shown on the sign-in screen, e.g. when the session had already expired.
  onSignedOut: (notice?: string) => void
}) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function logout() {
    setBusy(true)
    setError(null)
    try {
      await api.logout()
      onSignedOut()
    } catch (caught) {
      // ux_flows "Lejárt munkamenet": back to sign-in with the explanation, not an error on this page.
      if (caught instanceof ApiError && caught.status === 401) {
        onSignedOut(caught.message)
        return
      }
      setError(errorMessage(caught))
      setBusy(false)
    }
  }

  return (
    <main>
      <header className="topbar">
        <span>{me.email}</span>
        <button type="button" onClick={logout} disabled={busy}>Kijelentkezés</button>
      </header>
      <h1>Kamra</h1>
      <p className="empty">Még üres a kamrád.</p>
      <div role="alert" className="form-error">{error}</div>
    </main>
  )
}
