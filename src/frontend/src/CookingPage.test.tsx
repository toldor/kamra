import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import App from './App'
import { resetClientForTests } from './api/client'

// status 0 = the request never reached the server (network error).
type Reply = { status: number; body?: unknown }

// A fake backend: each route answers from a queue (the last reply repeats), and every request is recorded.
function fakeBackend(routes: Record<string, Reply[]>) {
  const calls: { key: string; body: unknown }[] = []
  vi.stubGlobal('fetch', vi.fn(async (url: string, init: RequestInit = {}) => {
    const key = `${init.method ?? 'GET'} ${url}`
    calls.push({ key, body: init.body ? JSON.parse(init.body as string) : undefined })
    const queue = routes[key]
    if (!queue) throw new Error(`Unexpected request: ${key}`)
    const reply = queue.length > 1 ? queue.shift()! : queue[0]
    if (reply.status === 0) throw new TypeError('Failed to fetch')
    return new Response(reply.body === undefined ? null : JSON.stringify(reply.body), { status: reply.status })
  }))
  return calls
}

const line = (ingredientName: string, unit: string, required: number, available: number, allocations: object[] = []) => ({
  ingredientId: `i-${ingredientName}`, ingredientName, unit, required, available,
  defaultUsed: Math.min(required, available), shortage: Math.max(0, required - available), allocations,
})

// The S5 example: Lecsó for 4, tomatoes from two items, the oil from an expired one, no onion.
const lecso = (servings = 4, tomatoes = 300) => ({
  recipeId: 'r-lecso',
  name: 'Lecsó',
  servings,
  recipeServings: 4,
  instructions: 'Pirítsd meg a hagymát. Add hozzá a paprikát és a paradicsomot.',
  staples: ['só'],
  lines: [
    line('étolaj', 'ml', 30, 1000, [{ expiryDate: '2026-10-10', amount: 30, expired: true }]),
    line('paprika', 'g', 500, 500, [{ expiryDate: '2026-10-13', amount: 500, expired: false }]),
    line('paradicsom', 'g', tomatoes, 600, [
      { expiryDate: '2026-10-12', amount: 200, expired: false },
      { expiryDate: '2026-10-16', amount: tomatoes - 200, expired: false },
    ]),
    line('vöröshagyma', 'g', 150, 0),
  ],
})

const cooked = {
  cookingId: 'c-1',
  recipeId: 'r-lecso',
  recipeName: 'Lecsó',
  servings: 4,
  cookedAt: '2026-10-11T16:00:00Z',
  lines: [
    { ingredientName: 'étolaj', unit: 'ml', required: 30, used: 30, shortage: 0 },
    { ingredientName: 'paprika', unit: 'g', required: 500, used: 500, shortage: 0 },
    { ingredientName: 'paradicsom', unit: 'g', required: 300, used: 250, shortage: 50 },
    { ingredientName: 'vöröshagyma', unit: 'g', required: 150, used: 0, shortage: 150 },
  ],
  depletedIngredients: ['paprika'],
}

const preview = 'GET /api/v1/recipes/r-lecso/cooking-preview'
const confirm = 'POST /api/v1/cookings'

function backend(extra: Record<string, Reply[]> = {}) {
  return fakeBackend({
    'GET /api/v1/auth/me': [{ status: 200, body: { email: 'tomi@example.com', householdId: 'h-1' } }],
    'GET /api/v1/auth/antiforgery': [{ status: 200, body: { requestToken: 'token-1' } }],
    [preview]: [{ status: 200, body: lecso() }],
    [confirm]: [{ status: 201, body: cooked }],
    ...extra,
  })
}

const group = (name: string) => screen.getByRole('group', { name })

beforeEach(() => {
  resetClientForTests()
  sessionStorage.clear()
  window.location.hash = '#/fozes/r-lecso'
})
afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
  window.location.hash = ''
})

describe('cooking', () => {
  it('opens the cooking screen from a recipe card', async () => {
    window.location.hash = '#/mit-fozzek'
    backend({
      'GET /api/v1/recommendations': [{
        status: 200,
        body: {
          pantryIsEmpty: false,
          almostMakeable: [],
          makeable: [{ recipeId: 'r-lecso', name: 'Lecsó', servings: 4, soonExpiringIngredients: [], expiredIngredients: [], shortages: [] }],
        },
      }],
    })
    render(<App />)

    fireEvent.click(within(await screen.findByRole('article', { name: 'Lecsó' })).getByRole('link', { name: 'Megfőztem' }))

    expect(await screen.findByRole('heading', { name: 'Lecsó főzése' })).toBeTruthy()
  })

  it('shows what each ingredient needs, what there is and which items the deduction comes from', async () => {
    backend()
    render(<App />)

    expect(await screen.findByRole('heading', { name: 'Lecsó főzése' })).toBeTruthy()
    expect(screen.getByText('A recept 4 adagos.')).toBeTruthy()
    const tomatoes = group('paradicsom')
    expect(tomatoes.textContent).toContain('Kell: 300 g · Van: 600 g')
    expect((within(tomatoes).getByLabelText('Felhasznált (g)') as HTMLInputElement).value).toBe('300')
    expect(tomatoes.textContent).toContain('Levonás: okt. 12. – 200 g, okt. 16. – 100 g')
    expect(group('étolaj').textContent).toContain('Levonás: okt. 10. – 30 ml (lejárt)')
    expect(group('vöröshagyma').textContent).toContain('Hiány: 150 g')
    expect(screen.getByText('Alaphozzávalók, ezek nem vonódnak le: só.')).toBeTruthy()
    expect(screen.getByRole('heading', { name: 'Elkészítés' })).toBeTruthy()
    expect(screen.getByText(/Pirítsd meg a hagymát/)).toBeTruthy()
  })

  it('updates the shortage as the used amount changes', async () => {
    backend()
    render(<App />)

    fireEvent.change(within(await screen.findByRole('group', { name: 'paradicsom' })).getByLabelText('Felhasznált (g)'), { target: { value: '100' } })

    expect(group('paradicsom').textContent).toContain('Hiány: 200 g')
  })

  it('loads the preview again for another number of servings', async () => {
    const calls = backend({
      [`${preview}?servings=6`]: [{ status: 200, body: lecso(6, 450) }],
    })
    render(<App />)

    fireEvent.change(await screen.findByLabelText('Adagszám'), { target: { value: '6' } })

    await waitFor(() => expect(group('paradicsom').textContent).toContain('Kell: 450 g'))
    expect(calls.map((c) => c.key)).toContain(`${preview}?servings=6`)
  })

  it('confirms the cooking with the used amounts and shows the summary', async () => {
    const calls = backend()
    render(<App />)

    fireEvent.change(within(await screen.findByRole('group', { name: 'paradicsom' })).getByLabelText('Felhasznált (g)'), { target: { value: '250' } })
    fireEvent.click(screen.getByRole('button', { name: 'Főzés megerősítése' }))

    expect(await screen.findByText('Elkészült: Lecsó, 4 adag.')).toBeTruthy()
    expect(screen.getByText('Levonva: étolaj 30 ml, paprika 500 g, paradicsom 250 g.')).toBeTruthy()
    expect(screen.getByText('Elfogyott: paprika.')).toBeTruthy()
    expect(screen.getByRole('link', { name: 'Vissza a készlethez' }).getAttribute('href')).toBe('#/')
    const body = calls.find((c) => c.key === confirm)!.body as { requestId: string; recipeId: string; servings: number; used: object[] }
    expect(body.recipeId).toBe('r-lecso')
    expect(body.servings).toBe(4)
    expect(body.used).toEqual([
      { ingredientId: 'i-étolaj', amount: 30 },
      { ingredientId: 'i-paprika', amount: 500 },
      { ingredientId: 'i-paradicsom', amount: 250 },
      { ingredientId: 'i-vöröshagyma', amount: 0 },
    ])
    expect(sessionStorage.getItem('kamra.cooking.r-lecso')).toBeNull()
  })

  it('sends the same request id after a reload and after a failed attempt', async () => {
    sessionStorage.setItem('kamra.cooking.r-lecso', 'req-1')
    const calls = backend({ [confirm]: [{ status: 0 }, { status: 201, body: cooked }] })
    render(<App />)

    fireEvent.click(await screen.findByRole('button', { name: 'Főzés megerősítése' }))
    expect((await screen.findByText(/Nem sikerült kapcsolódni a szerverhez/)).getAttribute('role')).toBe('alert')
    fireEvent.click(screen.getByRole('button', { name: 'Főzés megerősítése' }))

    expect(await screen.findByText('Elkészült: Lecsó, 4 adag.')).toBeTruthy()
    const ids = calls.filter((c) => c.key === confirm).map((c) => (c.body as { requestId: string }).requestId)
    expect(ids).toEqual(['req-1', 'req-1'])
  })

  it('sends one request even when the button is clicked twice', async () => {
    const calls = backend()
    render(<App />)

    const button = await screen.findByRole('button', { name: 'Főzés megerősítése' })
    fireEvent.click(button)
    fireEvent.click(button)

    expect(await screen.findByText('Elkészült: Lecsó, 4 adag.')).toBeTruthy()
    expect(calls.filter((c) => c.key === confirm)).toHaveLength(1)
  })

  it('explains a stock conflict and recalculates on request', async () => {
    const message = 'Közben kevesebb lett a készlet, mint amennyit felhasználnál. Nézd át a friss mennyiségeket, és erősítsd meg újra.'
    const calls = backend({ [confirm]: [{ status: 409, body: { code: 'INSUFFICIENT_STOCK', title: message } }] })
    render(<App />)

    fireEvent.click(await screen.findByRole('button', { name: 'Főzés megerősítése' }))
    // The layout's own (empty) alert region is always there, so the message is looked up by its text.
    expect((await screen.findByText(message)).getAttribute('role')).toBe('alert')
    fireEvent.click(screen.getByRole('button', { name: 'Újraszámolás' }))

    await waitFor(() => expect(calls.filter((c) => c.key === preview)).toHaveLength(2))
  })

  it('says so when the recipe does not exist', async () => {
    backend({ [preview]: [{ status: 404, body: { code: 'RECIPE_NOT_FOUND', title: 'Ez a recept nem található.' } }] })
    render(<App />)

    expect((await screen.findByText('Ez a recept nem található.')).getAttribute('role')).toBe('alert')
  })
})
