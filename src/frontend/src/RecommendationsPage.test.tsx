import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
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
    // status 0 = the request never reached the server (network error).
    if (reply.status === 0) throw new TypeError('Failed to fetch')
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

  it('moves the focus to the new page heading, and treats an unknown address as the pantry', async () => {
    window.location.hash = '#/ismeretlen'
    backend([recommendations({})])
    render(<App />)

    expect(await screen.findByRole('heading', { name: 'Kamra' })).toBeTruthy()
    expect(screen.getByRole('link', { name: 'Készlet' }).getAttribute('aria-current')).toBe('page')

    fireEvent.click(screen.getByRole('link', { name: 'Mit főzzek?' }))

    const heading = await screen.findByRole('heading', { name: 'Mit főzzek?' })
    await waitFor(() => expect(document.activeElement).toBe(heading))
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

describe('shortage to the shopping list', () => {
  const addItem = 'POST /api/v1/shopping-list/items'
  const rantotta = recipe('Rántotta', {
    servings: 2,
    shortages: [
      { ingredientId: 'i-tojás', ingredientName: 'tojás', missing: 3, available: 0, required: 3, unit: 'db' },
      { ingredientId: 'i-tejföl', ingredientName: 'tejföl', missing: 50, available: 150, required: 200, unit: 'g' },
    ],
  })
  const row = { id: 'l-1', ingredientId: 'i-tojás', ingredientName: 'tojás', amount: { value: 3, unit: 'db' }, checked: false }
  const sent = (calls: { url: string; init: RequestInit }[]) =>
    calls.filter((c) => `${c.init.method} ${c.url}` === addItem).map((c) => JSON.parse(c.init.body as string))

  it('puts every missing amount on the list with one click (P11)', async () => {
    const calls = backend([recommendations({ almostMakeable: [rantotta] })], {
      'GET /api/v1/auth/antiforgery': [{ status: 200, body: { requestToken: 'token-1' } }],
      [addItem]: [{ status: 200, body: row }],
    })
    render(<App />)

    const card = await screen.findByRole('article', { name: 'Rántotta' })
    // The live region is in place before it speaks, so a screen reader announces the result.
    const status = within(card).getByRole('status')
    expect(status.textContent).toBe('')
    fireEvent.click(within(card).getByRole('button', { name: 'Bevásárlólistára' }))

    await waitFor(() => expect(status.textContent).toBe('Felvettem a listára: tojás 3 db, tejföl 50 g.'))
    // The pressed button is gone, so the focus moves to the result.
    expect(document.activeElement).toBe(status)
    expect(within(card).getByRole('link', { name: 'Megnézem' }).getAttribute('href')).toBe('#/bevasarlolista')
    expect(within(card).queryByRole('button', { name: 'Bevásárlólistára' })).toBeNull()
    expect(sent(calls)).toEqual([
      { ingredientId: 'i-tojás', amount: 3, unit: 'db' },
      { ingredientId: 'i-tejföl', amount: 50, unit: 'g' },
    ])
  })

  it('sends only what is still missing after a failed attempt (K1)', async () => {
    const calls = backend([recommendations({ almostMakeable: [rantotta] })], {
      'GET /api/v1/auth/antiforgery': [{ status: 200, body: { requestToken: 'token-1' } }],
      [addItem]: [{ status: 200, body: row }, { status: 0 }, { status: 200, body: row }],
    })
    render(<App />)

    const card = await screen.findByRole('article', { name: 'Rántotta' })
    fireEvent.click(within(card).getByRole('button', { name: 'Bevásárlólistára' }))
    expect((await within(card).findByText(/Nem sikerült kapcsolódni a szerverhez/)).getAttribute('role')).toBe('alert')
    fireEvent.click(within(card).getByRole('button', { name: 'Bevásárlólistára' }))

    expect(await within(card).findByText('Felvettem a listára: tojás 3 db, tejföl 50 g.')).toBeTruthy()
    expect(sent(calls)).toEqual([
      { ingredientId: 'i-tojás', amount: 3, unit: 'db' },
      { ingredientId: 'i-tejföl', amount: 50, unit: 'g' },
      { ingredientId: 'i-tejföl', amount: 50, unit: 'g' },
    ])
  })

  it('offers the button only for almost makeable recipes', async () => {
    backend([recommendations({ makeable: [recipe('Lecsó')], almostMakeable: [rantotta] })])
    render(<App />)

    expect(within(await screen.findByRole('article', { name: 'Lecsó' })).queryByRole('button', { name: 'Bevásárlólistára' })).toBeNull()
  })
})
