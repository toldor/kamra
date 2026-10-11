import { useEffect, useRef, useState, type ReactNode } from 'react'
import { api, ApiError, errorMessage, type Me } from './api/client'
import { routes, type Route } from './useHashRoute'

// The signed-in frame: who is signed in, logging out, and the navigation between the pages.
export function Layout({ me, route, onSignedOut, children }: {
  me: Me
  route: Route
  onSignedOut: (notice?: string) => void
  children: ReactNode
}) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const firstRoute = useRef(route)

  // After navigating, the focus goes to the new page's heading, so a screen reader announces it
  // (ux_flows a11y). Not on the first page load, where the browser keeps its own focus.
  useEffect(() => {
    if (route !== firstRoute.current) {
      document.querySelector<HTMLElement>('main h1')?.focus()
      firstRoute.current = route
    }
  }, [route])

  async function logout() {
    setBusy(true)
    setError(null)
    try {
      await api.logout()
      onSignedOut()
    } catch (caught) {
      // 401: the session had already ended - the user wanted to be signed out, and is.
      if (caught instanceof ApiError && caught.status === 401) {
        onSignedOut()
        return
      }
      setError(errorMessage(caught))
      setBusy(false)
    }
  }

  const link = (href: string, label: string) => (
    <a href={href} aria-current={route === href ? 'page' : undefined}>{label}</a>
  )

  return (
    <div className="shell">
      <header className="topbar">
        <span>{me.email}</span>
        <button type="button" onClick={logout} disabled={busy}>Kijelentkezés</button>
      </header>
      <div role="alert" className="form-error">{error}</div>
      <nav aria-label="Oldalak" className="nav">
        {link(routes.pantry, 'Készlet')}
        {link(routes.recommendations, 'Mit főzzek?')}
      </nav>
      {children}
    </div>
  )
}
