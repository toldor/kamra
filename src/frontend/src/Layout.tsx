import { useEffect, useRef, useState, type ReactNode } from 'react'
import { api, type Me } from './api/client'
import { Icon, type IconName } from './Icon'
import { ProfileMenu } from './ProfileMenu'
import { routes, type Route } from './useHashRoute'

// The signed-in frame: the navigation between the pages and the Profilom panel (ui_polish_minispec S1). The band
// stays on top; on a phone the menu is a bar at the bottom (index.css), with the same links and names.
export function Layout({ me, route, onSignedOut, children }: {
  me: Me
  route: Route
  onSignedOut: (notice?: string) => void
  children: ReactNode
}) {
  const previousRoute = useRef(route)
  // US-6 (us6_minispec K2, K8): the "Kérdezz" item only when the switch is on; if this cannot be read, it stays hidden.
  const [chatEnabled, setChatEnabled] = useState(false)

  useEffect(() => {
    let active = true
    api.chatAvailability()
      .then((availability) => {
        if (active) setChatEnabled(availability.enabled)
      })
      .catch(() => {})
    return () => {
      active = false
    }
  }, [])

  // After navigating, the focus goes to the new page's heading, so a screen reader announces it
  // (ux_flows a11y). Not on the first page load, where the browser keeps its own focus.
  useEffect(() => {
    if (route !== previousRoute.current) {
      document.querySelector<HTMLElement>('main h1')?.focus()
      previousRoute.current = route
    }
  }, [route])

  const link = (href: string, icon: IconName, label: ReactNode) => (
    <a href={href} aria-current={route === href ? 'page' : undefined}>
      <Icon name={icon} />
      <span>{label}</span>
    </a>
  )

  return (
    <>
      {/* ADR-0013: the olive band. ui_polish_minispec Ü4: the bar on a phone shows the full names; <wbr> lets
          "Bevásárlólista" break without changing its name. */}
      <header className="band">
        <div className="band-inner">
          <span className="brand">Kamra</span>
          <nav aria-label="Oldalak" className="nav">
            {link(routes.pantry, 'pantry', 'Készlet')}
            {link(routes.recommendations, 'cook', 'Mit főzzek?')}
            {link(routes.recipes, 'recipes', 'Receptek')}
            {link(routes.shopping, 'list', <>Bevásárló<wbr />lista</>)}
            {chatEnabled && link(routes.chat, 'chat', 'Kérdezz')}
          </nav>
          <ProfileMenu me={me} route={route} onSignedOut={onSignedOut} />
        </div>
      </header>
      {children}
    </>
  )
}
