import { useEffect, useState } from 'react'

// Q9: hash routes (#/, #/mit-fozzek) need no server fallback and no router dependency. The route lives
// in the URL, so after signing in again the user is back on the same page (ux_flows, Lejárt munkamenet).
export const routes = { pantry: '#/', recommendations: '#/mit-fozzek' } as const

export type Route = (typeof routes)[keyof typeof routes]

// An unknown or empty address shows the pantry, so exactly one navigation link is always current.
const current = (): Route => (Object.values(routes) as string[]).includes(window.location.hash)
  ? (window.location.hash as Route)
  : routes.pantry

export function useHashRoute(): Route {
  const [route, setRoute] = useState(current)

  useEffect(() => {
    const update = () => setRoute(current())
    window.addEventListener('hashchange', update)
    return () => window.removeEventListener('hashchange', update)
  }, [])

  return route
}
