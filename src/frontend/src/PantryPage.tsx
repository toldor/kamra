import { useState } from 'react'
import { api, ApiError, type Me } from './api/client'
import { FALLBACK_MESSAGE } from './api/messages'

// Walking skeleton: the pantry is always empty until US-1 adds pantry items.
export function PantryPage({ me, onSignedOut }: { me: Me; onSignedOut: () => void }) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function logout() {
    setBusy(true)
    setError(null)
    try {
      await api.logout()
      onSignedOut()
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : FALLBACK_MESSAGE)
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
      <div role="alert" aria-live="assertive" className="form-error">{error}</div>
    </main>
  )
}
