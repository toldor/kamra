import { useEffect, useState } from 'react'

// Q9: hash routes (#/, #/mit-fozzek, #/bevasarlolista, #/fozes/{receptId}) need no server fallback and no router dependency.
// The route lives in the URL, so after signing in again the user is back on the same page (ux_flows,
// Lejárt munkamenet).
export const routes = { pantry: '#/', recommendations: '#/mit-fozzek', shopping: '#/bevasarlolista' } as const

const cookingPrefix = '#/fozes/'

export type Route = (typeof routes)[keyof typeof routes] | `${typeof cookingPrefix}${string}`

export const cookingRoute = (recipeId: string): Route => `${cookingPrefix}${recipeId}`

// The recipe of a cooking route, or null.
export const cookingRecipeId = (route: Route): string | null =>
  route.startsWith(cookingPrefix) ? route.slice(cookingPrefix.length) : null

// An unknown or empty address shows the pantry.
const current = (): Route => {
  const hash = window.location.hash
  if ((Object.values(routes) as string[]).includes(hash)) return hash as Route
  return hash.length > cookingPrefix.length && hash.startsWith(cookingPrefix) ? (hash as Route) : routes.pantry
}

export function useHashRoute(): Route {
  const [route, setRoute] = useState(current)

  useEffect(() => {
    const update = () => setRoute(current())
    window.addEventListener('hashchange', update)
    return () => window.removeEventListener('hashchange', update)
  }, [])

  return route
}
