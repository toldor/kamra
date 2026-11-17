import { useEffect, useState } from 'react'

// Q9: hash routes (#/, #/mit-fozzek, #/receptek, #/receptek/uj, #/bevasarlolista, #/kerdezz, #/profil, #/fozes/{receptId}) need no server fallback and no router dependency.
// The route lives in the URL, so after signing in again the user is back on the same page (ux_flows,
// Lejárt munkamenet).
export const routes = {
  pantry: '#/',
  recommendations: '#/mit-fozzek',
  recipes: '#/receptek',
  newRecipe: '#/receptek/uj',
  shopping: '#/bevasarlolista',
  chat: '#/kerdezz',
  profile: '#/profil',
} as const

// ui_polish_minispec 7.: an unknown, non-empty address.
export const notFoundRoute = 'not-found'

const cookingPrefix = '#/fozes/'

export type Route = (typeof routes)[keyof typeof routes] | `${typeof cookingPrefix}${string}` | typeof notFoundRoute

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

// ui_polish_minispec 6.: the browser tab's title of a signed-in page.
export const pageTitle = (route: Route): string => (route ? 'Kamra' : 'Kamra')

export function useHashRoute(): Route {
  const [route, setRoute] = useState(current)

  useEffect(() => {
    const update = () => setRoute(current())
    window.addEventListener('hashchange', update)
    return () => window.removeEventListener('hashchange', update)
  }, [])

  return route
}
