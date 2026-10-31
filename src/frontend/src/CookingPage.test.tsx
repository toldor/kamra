import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import App from './App'
import { resetClientForTests } from './api/client'

// status 0 = the request never reached the server (network error).
// wait: the answer is held back until the promise settles.
type Reply = { status: number; body?: unknown; wait?: Promise<unknown> }

// A fake backend: each route answers from a queue (the last reply repeats), and every request is recorded.
function fakeBackend(routes: Record<string, Reply[]>) {
  const calls: { key: string; body: unknown }[] = []
  vi.stubGlobal('fetch', vi.fn(async (url: string, init: RequestInit = {}) => {
    const key = `${init.method ?? 'GET'} ${url}`
    calls.push({ key, body: init.body ? JSON.parse(init.body as string) : undefined })
    const queue = routes[key]
    if (!queue) throw new Error(`Unexpected request: ${key}`)
    const reply = queue.length > 1 ? queue.shift()! : queue[0]
    if (reply.wait) await reply.wait
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
  suggestedIngredients: [] as string[],
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

    fireEvent.click(within(await screen.findByRole('article', { name: 'Lecsó' })).getByRole('link', { name: 'Recept megtekintése' }))

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
    expect(tomatoes.textContent).toContain('Levonás (lejárat – mennyiség): okt. 12. – 200 g, okt. 16. – 100 g')
    expect(group('étolaj').textContent).toContain('Levonás (lejárat – mennyiség): okt. 10. – 30 ml (lejárt)')
    expect(group('vöröshagyma').textContent).toContain('Hiány: 150 g')
    expect(group('vöröshagyma').textContent).not.toContain('Levonás')
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

  it('marks an invalid used amount at its field, focuses it and sends nothing', async () => {
    const calls = backend()
    render(<App />)

    const input = within(await screen.findByRole('group', { name: 'paradicsom' })).getByLabelText('Felhasznált (g)')
    fireEvent.change(input, { target: { value: '-1' } })
    fireEvent.click(screen.getByRole('button', { name: 'Főzés megerősítése' }))

    await waitFor(() => expect(document.activeElement).toBe(input))
    expect(input.getAttribute('aria-invalid')).toBe('true')
    const message = document.getElementById(input.getAttribute('aria-describedby')!)
    expect(message?.textContent).toBe('A felhasznált mennyiség 0 és 100 000 között legyen.')
    expect(calls.filter((c) => c.key === confirm)).toHaveLength(0)
  })

  it('explains an item changed meanwhile in cooking terms', async () => {
    backend({
      [confirm]: [{
        status: 409,
        body: { code: 'PANTRY_ITEM_MODIFIED', title: 'Ezt a tételt közben máshol módosították. Nézd át a friss adatokat, és mentsd újra.' },
      }],
    })
    render(<App />)

    fireEvent.click(await screen.findByRole('button', { name: 'Főzés megerősítése' }))

    const alert = await screen.findByText('Közben megváltozott a készlet. Számold újra, és erősítsd meg újra.')
    expect(alert.getAttribute('role')).toBe('alert')
    expect(screen.getByRole('button', { name: 'Újraszámolás' })).toBeTruthy()
  })

  // M8 (T3): archived in another tab; recalculating would not help, so only the way back is offered.
  it('says so when the recipe was archived, on the preview and on the confirmation', async () => {
    const archived = { status: 409, body: { code: 'RECIPE_ARCHIVED', title: 'Ezt a receptet archiváltad, ezért már nem főzhető.' } }
    backend({ [confirm]: [archived] })
    render(<App />)

    fireEvent.click(await screen.findByRole('button', { name: 'Főzés megerősítése' }))

    expect((await screen.findByText('Ezt a receptet archiváltad, ezért már nem főzhető.')).getAttribute('role')).toBe('alert')
    expect(screen.queryByRole('button', { name: 'Újraszámolás' })).toBeNull()
    // Review C1: confirming again cannot help, so the form goes and the way back is offered.
    expect(screen.queryByRole('button', { name: 'Főzés megerősítése' })).toBeNull()
    expect(screen.getByRole('link', { name: 'Vissza a receptekhez' }).getAttribute('href')).toBe('#/mit-fozzek')

    cleanup()
    backend({ [preview]: [archived] })
    render(<App />)

    expect((await screen.findByText('Ezt a receptet archiváltad, ezért már nem főzhető.')).getAttribute('role')).toBe('alert')
    expect(screen.getByRole('link', { name: 'Vissza a receptekhez' }).getAttribute('href')).toBe('#/mit-fozzek')
  })

  it('says so when the recipe does not exist', async () => {
    backend({ [preview]: [{ status: 404, body: { code: 'RECIPE_NOT_FOUND', title: 'Ez a recept nem található.' } }] })
    render(<App />)

    expect((await screen.findByText('Ez a recept nem található.')).getAttribute('role')).toBe('alert')
  })
})

describe('cooking and the shopping list', () => {
  const addItem = 'POST /api/v1/shopping-list/items'
  const row = { id: 'l-1', ingredientId: 'i-paradicsom', ingredientName: 'paradicsom', amount: null, checked: false }
  const tomatoes = () => within(group('paradicsom')).getByLabelText('Felhasznált (g)') as HTMLInputElement

  it('puts the live shortage on the list before the confirmation (P13)', async () => {
    const calls = backend({ [addItem]: [{ status: 200, body: row }] })
    render(<App />)

    fireEvent.change(within(await screen.findByRole('group', { name: 'paradicsom' })).getByLabelText('Felhasznált (g)'), { target: { value: '250' } })
    fireEvent.click(screen.getByRole('button', { name: 'Hiány a bevásárlólistára' }))

    expect(await screen.findByText('Felvettem a listára: paradicsom 50 g, vöröshagyma 150 g.')).toBeTruthy()
    expect(calls.filter((c) => c.key === addItem).map((c) => c.body)).toEqual([
      { ingredientId: 'i-paradicsom', amount: 50, unit: 'g' },
      { ingredientId: 'i-vöröshagyma', amount: 150, unit: 'g' },
    ])
    expect(screen.queryByRole('button', { name: 'Hiány a bevásárlólistára' })).toBeNull()
    expect(screen.getByRole('button', { name: 'Főzés megerősítése' })).toBeTruthy()

    // K10: no shortage for a moment, then one again - the button does not come back, so nothing is added twice.
    fireEvent.change(tomatoes(), { target: { value: '300' } })
    fireEvent.change(within(group('vöröshagyma')).getByLabelText('Felhasznált (g)'), { target: { value: '150' } })
    fireEvent.change(tomatoes(), { target: { value: '250' } })
    expect(screen.queryByRole('button', { name: 'Hiány a bevásárlólistára' })).toBeNull()
    expect(screen.getByText('Felvettem a listára: paradicsom 50 g, vöröshagyma 150 g.')).toBeTruthy()
  })

  it('names the new shopping suggestions in the summary, and only when there are some (M5)', async () => {
    backend({ [confirm]: [{ status: 201, body: { ...cooked, suggestedIngredients: ['paprika'] } }] })
    render(<App />)

    fireEvent.click(await screen.findByRole('button', { name: 'Főzés megerősítése' }))

    expect(await screen.findByText('Felvettem a bevásárlójavaslatok közé: paprika.')).toBeTruthy()
    expect(screen.getByRole('link', { name: 'Megnézem' }).getAttribute('href')).toBe('#/bevasarlolista')
    cleanup()

    sessionStorage.clear()
    backend()
    render(<App />)
    fireEvent.click(await screen.findByRole('button', { name: 'Főzés megerősítése' }))

    expect(await screen.findByText('Elkészült: Lecsó, 4 adag.')).toBeTruthy()
    expect(screen.queryByText(/bevásárlójavaslatok közé/)).toBeNull()
    expect(screen.queryByRole('link', { name: 'Megnézem' })).toBeNull()
  })

  it('marks the line whose stock dropped meanwhile with the fresh amount (R1)', async () => {
    const available = [
      { ingredientId: 'i-étolaj', ingredientName: 'étolaj', amount: 1000, unit: 'ml' },
      { ingredientId: 'i-paprika', ingredientName: 'paprika', amount: 500, unit: 'g' },
      { ingredientId: 'i-paradicsom', ingredientName: 'paradicsom', amount: 150, unit: 'g' },
      { ingredientId: 'i-vöröshagyma', ingredientName: 'vöröshagyma', amount: 0, unit: 'g' },
    ]
    backend({
      [confirm]: [{ status: 409, body: { code: 'INSUFFICIENT_STOCK', title: 'Közben kevesebb lett a készlet.', available } }],
    })
    render(<App />)

    fireEvent.click(await screen.findByRole('button', { name: 'Főzés megerősítése' }))

    await waitFor(() => expect(document.activeElement).toBe(tomatoes()))
    expect(tomatoes().getAttribute('aria-invalid')).toBe('true')
    expect(document.getElementById(tomatoes().getAttribute('aria-describedby')!)?.textContent).toBe('Már csak 150 g van, 300 g helyett.')
    expect(group('paradicsom').textContent).toContain('Van: 150 g')
    expect(within(group('paprika')).getByLabelText('Felhasznált (g)').getAttribute('aria-invalid')).toBeNull()
  })

  it('brings the default amounts for new servings even while a recalculation is under way (R1)', async () => {
    backend({
      [preview]: [{ status: 200, body: lecso() }, { status: 200, body: lecso(), wait: new Promise(() => {}) }],
      [`${preview}?servings=6`]: [{ status: 200, body: lecso(6, 450) }],
      [confirm]: [{ status: 409, body: { code: 'INSUFFICIENT_STOCK', title: 'Közben kevesebb lett a készlet.' } }],
    })
    render(<App />)

    fireEvent.change(within(await screen.findByRole('group', { name: 'paradicsom' })).getByLabelText('Felhasznált (g)'), { target: { value: '250' } })
    fireEvent.click(screen.getByRole('button', { name: 'Főzés megerősítése' }))
    fireEvent.click(await screen.findByRole('button', { name: 'Újraszámolás' }))
    fireEvent.change(screen.getByLabelText('Adagszám'), { target: { value: '6' } })

    await waitFor(() => expect(group('paradicsom').textContent).toContain('Kell: 450 g'))
    expect(tomatoes().value).toBe('450')
  })

  it('keeps the entered amounts when recalculating, and warns where they are too much (R1)', async () => {
    const fewerTomatoes = {
      ...lecso(),
      lines: lecso().lines.map((l) => (l.ingredientName === 'paradicsom' ? { ...l, available: 200, defaultUsed: 200, shortage: 100 } : l)),
    }
    const calls = backend({
      [preview]: [{ status: 200, body: lecso() }, { status: 200, body: fewerTomatoes }],
      [confirm]: [{
        status: 409,
        body: { code: 'INSUFFICIENT_STOCK', title: 'Közben kevesebb lett a készlet.', available: [{ ingredientId: 'i-paradicsom', ingredientName: 'paradicsom', amount: 200, unit: 'g' }] },
      }],
    })
    render(<App />)

    fireEvent.change(within(await screen.findByRole('group', { name: 'paradicsom' })).getByLabelText('Felhasznált (g)'), { target: { value: '250' } })
    fireEvent.change(within(group('paprika')).getByLabelText('Felhasznált (g)'), { target: { value: '400' } })
    fireEvent.click(screen.getByRole('button', { name: 'Főzés megerősítése' }))
    fireEvent.click(await screen.findByRole('button', { name: 'Újraszámolás' }))

    await waitFor(() => expect(calls.filter((c) => c.key === preview)).toHaveLength(2))
    await waitFor(() => expect(group('paradicsom').textContent).toContain('Van: 200 g'))
    expect(tomatoes().value).toBe('250')
    expect((within(group('paprika')).getByLabelText('Felhasznált (g)') as HTMLInputElement).value).toBe('400')
    expect(group('paradicsom').textContent).toContain('Több, mint ami van (200 g).')
    expect(group('paprika').textContent).not.toContain('Több, mint ami van')
  })
})
