import type { Me } from './api/client'
import { ThemeChoice } from './ThemeChoice'
import { useLogout } from './useLogout'

// ui_polish_minispec 2.: the Profilom page (#/profil). Signing out ends every session of the user (D-3, ADR-0006).
export function ProfilePage({ me, onSignedOut }: { me: Me; onSignedOut: (notice?: string) => void }) {
  const { logout, busy, error } = useLogout(onSignedOut)
  return (
    <main className="form-page">
      <h1 tabIndex={-1}>Profilom</h1>
      <section className="panel panel-body">
        <p>E-mail-cím: {me.email}</p>
        <ThemeChoice />
        <p className="hint">A téma ebben a böngészőben marad meg.</p>
        <p className="hint">A kijelentkezés minden eszközödön kiléptet.</p>
        <div className="actions">
          <button type="button" className="secondary" onClick={() => void logout()} disabled={busy}>Kijelentkezés</button>
        </div>
        <p role="alert" className="form-error">{error}</p>
      </section>
    </main>
  )
}
