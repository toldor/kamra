import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import App from './App'
import { resetClientForTests } from './api/client'

type Reply = { status: number; body?: unknown }

// A fake backend: each route answers from a queue (the last reply repeats), and every request is recorded.
function fakeBackend(routes: Record<string, Reply[]>) {
  const calls: { url: string; init: RequestInit }[] = []
  vi.stubGlobal('fetch', vi.fn(async (url: string, init: RequestInit = {}) => {
    calls.push({ url, init })
    const key = `${init.method ?? 'GET'} ${url}`
    const queue = routes[key]
    if (!queue) throw new Error(`Unexpected request: ${key}`)
    const reply = queue.length > 1 ? queue.shift()! : queue[0]
    return new Response(reply.body === undefined ? null : JSON.stringify(reply.body), { status: reply.status })
  }))
  return calls
}

const signedIn: Reply = { status: 200, body: { email: 'tomi@example.com', householdId: 'h-1' } }
const notSignedIn: Reply = { status: 401, body: { code: 'UNAUTHENTICATED', title: 'Jelentkezz be a folytatáshoz.' } }
const emptyList: Reply = { status: 200, body: [] }
const recipe = (name: string, extra: object = {}) => ({
  recipeId: `r-${name}`, name, servings: 4, soonExpiringIngredients: [], expiredIngredients: [], shortages: [], ...extra,
})
const recommendations = (body: object): Reply => ({ status: 200, body: { pantryIsEmpty: false, makeable: [], almostMakeable: [], ...body } })

function backend(recommendationReplies: Reply[], extra: Record<string, Reply[]> = {}) {
  return fakeBackend({
    'GET /api/v1/auth/me': [signedIn],
    'GET /api/v1/pantry-items': [emptyList],
    'GET /api/v1/categories': [emptyList],
    'GET /api/v1/ingredients': [emptyList],
    'GET /api/v1/recommendations': recommendationReplies,
    ...extra,
  })
}

beforeEach(() => {
  resetClientForTests()
  window.location.hash = '#/mit-fozzek'
})
afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
  window.location.hash = ''
})

describe('navigation', () => {
  it('moves between the pantry and the recipe suggestions and marks the current page', async () => {
    window.location.hash = ''
    backend([recommendations({})])
    render(<App />)

    expect(await screen.findByRole('heading', { name: 'Kamra' })).toBeTruthy()
    fireEvent.click(screen.getByRole('link', { name: 'Mit főzzek?' }))

    expect(await screen.findByRole('heading', { name: 'Mit főzzek?' })).toBeTruthy()
    expect(screen.getByRole('link', { name: 'Mit főzzek?' }).getAttribute('aria-current')).toBe('page')
    expect(screen.getByRole('link', { name: 'Készlet' }).getAttribute('aria-current')).toBeNull()
  })

  it('returns to the same page after signing in again', async () => {
    fakeBackend({
      'GET /api/v1/auth/me': [notSignedIn, signedIn],
      'GET /api/v1/auth/antiforgery': [{ status: 200, body: { requestToken: 'token-1' } }],
      'POST /api/v1/auth/login': [{ status: 204 }],
      'GET /api/v1/recommendations': [recommendations({})],
    })
    render(<App />)

    fireEvent.change(await screen.findByLabelText('E-mail-cím'), { target: { value: 'tomi@example.com' } })
    fireEvent.change(screen.getByLabelText('Jelszó'), { target: { value: 'correct horse battery staple' } })
    fireEvent.click(screen.getByRole('button', { name: 'Bejelentkezés' }))

    expect(await screen.findByRole('heading', { name: 'Mit főzzek?' })).toBeTruthy()
  })
})

describe('recipe suggestions', () => {
  it('shows makeable and almost makeable recipes with their reasons and shortages', async () => {
    backend([recommendations({
      makeable: [
        recipe('Lecsó', { soonExpiringIngredients: ['paradicsom', 'paprika'] }),
        recipe('Paprikás krumpli'),
      ],
      almostMakeable: [recipe('Rántotta', {
        servings: 2,
        shortages: [
          { ingredientName: 'tojás', missing: 3, available: 0, required: 3, unit: 'db' },
          { ingredientName: 'tejföl', missing: 50, available: 150, required: 200, unit: 'g' },
        ],
      })],
    })])
    render(<App />)

    const lecso = await screen.findByRole('article', { name: 'Lecsó' })
    expect(lecso.textContent).toContain('4 adag')
    expect(lecso.textContent).toContain('2 hamarosan lejáró hozzávalót használ fel: paradicsom, paprika')
    expect(screen.getByRole('article', { name: 'Paprikás krumpli' }).textContent).toContain('Minden hozzávaló megvan.')
    const rantotta = screen.getByRole('article', { name: 'Rántotta' })
    expect(rantotta.textContent).toContain('tojás: 3 db hiányzik (0 db van, 3 db kell)')
    expect(rantotta.textContent).toContain('tejföl: 50 g hiányzik (150 g van, 200 g kell)')
    expect(screen.getByRole('heading', { name: 'Elkészíthető' })).toBeTruthy()
    expect(screen.getByRole('heading', { name: 'Majdnem elkészíthető' })).toBeTruthy()
  })

  it('marks a recipe that uses an expired item', async () => {
    backend([recommendations({ makeable: [recipe('Rántotta', { expiredIngredients: ['tojás'] })] })])
    render(<App />)

    expect((await screen.findByRole('article', { name: 'Rántotta' })).textContent).toContain('Lejárt tételt is felhasznál: tojás.')
  })

  it('tells why there is nothing to suggest when the pantry is empty', async () => {
    backend([{ status: 200, body: { pantryIsEmpty: true, makeable: [], almostMakeable: [] } }])
    render(<App />)

    expect(await screen.findByText('Még nincs mit ajánlanom, mert üres a kamrád. Rögzítsd, mit vettél, és megmutatom, mit főzhetsz belőle.')).toBeTruthy()
    expect(screen.getByRole('link', { name: 'Készlet rögzítése' }).getAttribute('href')).toBe('#/')
  })

  it('says so when only almost makeable recipes exist', async () => {
    backend([recommendations({ almostMakeable: [recipe('Rántotta', { shortages: [{ ingredientName: 'vaj', missing: 20, available: 0, required: 20, unit: 'g' }] })] })])
    render(<App />)

    expect(await screen.findByText('Most egyik receptedhez sincs meg minden hozzávaló.')).toBeTruthy()
    expect(screen.getByRole('article', { name: 'Rántotta' })).toBeTruthy()
  })

  it('says so when nothing matches the pantry', async () => {
    backend([recommendations({})])
    render(<App />)

    expect(await screen.findByText('A kamrád alapján most nem találtam receptet. Rögzíts további hozzávalókat, és újra megnézem.')).toBeTruthy()
    expect(screen.getByRole('link', { name: 'Készlet rögzítése' })).toBeTruthy()
  })

  it('shows an error and loads again on retry', async () => {
    backend([
      { status: 500, body: { code: 'INTERNAL_ERROR', title: 'Váratlan hiba történt. Próbáld újra később.' } },
      recommendations({ makeable: [recipe('Lecsó')] }),
    ])
    render(<App />)

    expect(await screen.findByText('Váratlan hiba történt. Próbáld újra később.')).toBeTruthy()
    fireEvent.click(screen.getByRole('button', { name: 'Újra' }))

    await waitFor(() => expect(screen.getByRole('article', { name: 'Lecsó' })).toBeTruthy())
  })
})
