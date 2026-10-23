import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import App from './App'
import { resetClientForTests } from './api/client'

type Reply = { status: number; body?: unknown; wait?: Promise<void> }

// A fake backend: each route answers from a queue (the last reply repeats), and every request is recorded.
// status 0 = the request never reached the server; wait holds the answer back.
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

const signedIn: Reply = { status: 200, body: { email: 'tomi@example.com', householdId: 'h-1' } }
const token: Reply = { status: 200, body: { requestToken: 'token-1' } }
const categories: Reply = { status: 200, body: [{ category: 'dairy', shelfLifeDays: 7 }, { category: 'beverages', shelfLifeDays: 30 }] }
const tej = { id: 'i-tej', name: 'tej', dimension: 'volume', defaultCategory: 'dairy', minimum: null }
const kakao = { id: 'i-kakao', name: 'kakaópor', dimension: 'mass', defaultCategory: 'dryGoods', minimum: null }
const zabtej = { id: 'i-zabtej', name: 'zabtej', dimension: 'volume', defaultCategory: 'beverages', minimum: null }
const ingredients = (...rows: object[]): Reply => ({ status: 200, body: rows })
const lecso = { id: 'r-lecso', name: 'Lecsó', servings: 4, own: false, ingredients: ['étolaj', 'paprika', 'paradicsom', 'só', 'vöröshagyma'] }
const own = { id: 'r-kakao', name: 'Zabtejes kakaó', servings: 1, own: true, ingredients: ['zabtej'] }
const recipes = (...rows: object[]): Reply => ({ status: 200, body: rows })

const recipesUrl = 'GET /api/v1/recipes'
const createUrl = 'POST /api/v1/recipes'
const draftKey = 'kamra:recipe-draft'

function backend(extra: Record<string, Reply[]> = {}) {
  return fakeBackend({
    'GET /api/v1/auth/me': [signedIn],
    'GET /api/v1/auth/antiforgery': [token],
    'GET /api/v1/categories': [categories],
    'GET /api/v1/ingredients': [ingredients(tej, kakao)],
    [recipesUrl]: [recipes(lecso, own)],
    ...extra,
  })
}

const card = (name: string) => within(screen.getByRole('article', { name }))
const row = (n: number) => within(screen.getByRole('group', { name: `${n}. hozzávaló` }))

beforeEach(() => {
  resetClientForTests()
  sessionStorage.clear()
  window.location.hash = '#/receptek'
})
afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
  window.location.hash = ''
})

describe('the Receptek page (N9)', () => {
  it('is in the menu and lists the recipes with servings, own mark, ingredients and a cooking link', async () => {
    backend()
    render(<App />)

    expect(await screen.findByRole('heading', { name: 'Receptek' })).toBeTruthy()
    expect(screen.getByRole('link', { name: 'Receptek' }).getAttribute('aria-current')).toBe('page')
    expect(screen.getByRole('link', { name: 'Új recept' }).getAttribute('href')).toBe('#/receptek/uj')
    expect(card('Lecsó').getByText('4 adag')).toBeTruthy()
    expect(card('Lecsó').queryByText('Saját recept')).toBeNull()
    expect(card('Lecsó').getByText('Hozzávalók: étolaj, paprika, paradicsom, só, vöröshagyma')).toBeTruthy()
    expect(card('Lecsó').getByRole('link', { name: 'Megfőztem' }).getAttribute('href')).toBe('#/fozes/r-lecso')
    expect(card('Zabtejes kakaó').getByText('Saját recept')).toBeTruthy()
    expect(card('Zabtejes kakaó').getByText('1 adag')).toBeTruthy()
  })

  it('shows the loading, error and empty states, and loads again on retry', async () => {
    let release!: () => void
    backend({
      [recipesUrl]: [
        { status: 500, body: { code: 'INTERNAL_ERROR', title: 'Váratlan hiba történt. Próbáld újra később.' }, wait: new Promise((r) => { release = r }) },
        recipes(),
      ],
    })
    render(<App />)

    expect(await screen.findByText('Betöltés…')).toBeTruthy()
    release()
    expect((await screen.findByText('Váratlan hiba történt. Próbáld újra később.')).getAttribute('role')).toBe('alert')
    fireEvent.click(screen.getByRole('button', { name: 'Újra' }))
    expect(await screen.findByText('Nincs recepted. Vegyél fel egyet az „Új recept” gombbal.')).toBeTruthy()
  })

  it('archives only after the confirmation, then tells so and reloads (M10)', async () => {
    const calls = backend({
      [recipesUrl]: [recipes(lecso, own), recipes(own)],
      'POST /api/v1/recipes/r-lecso/archive': [{ status: 204 }],
    })
    const confirm = vi.spyOn(window, 'confirm').mockReturnValueOnce(false).mockReturnValueOnce(true)
    render(<App />)

    fireEvent.click(await screen.findByRole('button', { name: 'Archiválás: Lecsó' }))
    expect(confirm).toHaveBeenLastCalledWith(
      'Archiválod ezt a receptet: Lecsó? Nem ajánlom többé, és nem főzhető. A korábbi főzéseid megmaradnak. Ezt nem lehet visszavonni.')
    expect(calls.some((c) => c.key.endsWith('/archive'))).toBe(false)

    fireEvent.click(screen.getByRole('button', { name: 'Archiválás: Lecsó' }))
    const notice = await screen.findByText('Archiváltam: Lecsó.')
    expect(notice.getAttribute('role')).toBe('status')
    await waitFor(() => expect(screen.queryByRole('article', { name: 'Lecsó' })).toBeNull())
    await waitFor(() => expect(document.activeElement).toBe(notice))
    expect(calls.filter((c) => c.key === 'POST /api/v1/recipes/r-lecso/archive')).toHaveLength(1)
  })
})

describe('the Új recept form (N10)', () => {
  beforeEach(() => {
    window.location.hash = '#/receptek/uj'
  })

  async function fillKakao() {
    fireEvent.change(await screen.findByLabelText('Név'), { target: { value: 'Kakaó' } })
    fireEvent.change(screen.getByLabelText('Adagszám'), { target: { value: '2' } })
    fireEvent.change(row(1).getByLabelText('Hozzávaló'), { target: { value: 'tej' } })
    fireEvent.change(row(1).getByLabelText('Mennyiség'), { target: { value: '5' } })
    fireEvent.change(row(1).getByLabelText('Mértékegység'), { target: { value: 'dl' } })
    fireEvent.click(screen.getByRole('button', { name: 'Újabb hozzávaló' }))
    fireEvent.change(row(2).getByLabelText('Hozzávaló'), { target: { value: 'kakaópor' } })
    fireEvent.change(row(2).getByLabelText('Mennyiség'), { target: { value: '2' } })
    fireEvent.change(row(2).getByLabelText('Mértékegység'), { target: { value: 'dkg' } })
  }

  it('sends the rows, then opens the Receptek page with a notice and forgets the draft', async () => {
    const calls = backend({ [createUrl]: [{ status: 201, body: { id: 'r-new', name: 'Kakaó', servings: 2, own: true, ingredients: ['kakaópor', 'tej'] } }] })
    render(<App />)

    expect(await screen.findByRole('heading', { name: 'Új recept' })).toBeTruthy()
    expect((screen.getByLabelText('Adagszám') as HTMLInputElement).value).toBe('4')
    await fillKakao()
    fireEvent.click(screen.getByRole('button', { name: 'Újabb hozzávaló' }))
    fireEvent.click(screen.getByRole('button', { name: 'Sor törlése: 3. hozzávaló' }))
    expect(screen.queryByRole('group', { name: '3. hozzávaló' })).toBeNull()
    fireEvent.change(screen.getByLabelText('Elkészítés (nem kötelező)'), { target: { value: 'Keverd el.' } })
    fireEvent.click(screen.getByRole('button', { name: 'Mentés' }))

    expect((await screen.findByText('Elmentettem a receptet: Kakaó.')).getAttribute('role')).toBe('status')
    expect(window.location.hash).toBe('#/receptek')
    expect(calls.find((c) => c.key === createUrl)?.body).toEqual({
      name: 'Kakaó',
      servings: 2,
      instructions: 'Keverd el.',
      ingredients: [{ ingredientId: 'i-tej', amount: 5, unit: 'dl' }, { ingredientId: 'i-kakao', amount: 2, unit: 'dkg' }],
    })
    expect(sessionStorage.getItem(draftKey)).toBeNull()
  })

  it('puts the backend field errors under the right row and focuses the first one', async () => {
    backend({
      [createUrl]: [{
        status: 400,
        body: {
          code: 'VALIDATION_FAILED',
          title: 'Néhány mező hibás. Javítsd a jelölt mezőket, és próbáld újra.',
          errors: { 'ingredients[1].amount': ['A mennyiség 0,001 és 100 000 között legyen, legfeljebb 3 tizedesjeggyel.'] },
        },
      }],
    })
    render(<App />)

    await fillKakao()
    fireEvent.click(screen.getByRole('button', { name: 'Mentés' }))

    const amount = row(2).getByLabelText('Mennyiség')
    expect(await row(2).findByText('A mennyiség 0,001 és 100 000 között legyen, legfeljebb 3 tizedesjeggyel.')).toBeTruthy()
    expect(amount.getAttribute('aria-invalid')).toBe('true')
    await waitFor(() => expect(document.activeElement).toBe(amount))
    expect(row(1).getByLabelText('Mennyiség').getAttribute('aria-invalid')).toBeNull()
  })

  // Review S2: a recipe-level error ("ingredients") is tied to the first row's field and focused.
  it('ties the recipe-level ingredient error to the first row and focuses it', async () => {
    const message = 'Adj meg legalább egy hozzávalót a víz, a só és a bors mellett.'
    backend({
      [createUrl]: [{
        status: 400,
        body: { code: 'VALIDATION_FAILED', title: 'Néhány mező hibás. Javítsd a jelölt mezőket, és próbáld újra.', errors: { ingredients: [message] } },
      }],
    })
    render(<App />)

    await fillKakao()
    fireEvent.click(screen.getByRole('button', { name: 'Mentés' }))

    const first = row(1).getByLabelText('Hozzávaló')
    expect(await row(1).findByText(message)).toBeTruthy()
    expect(first.getAttribute('aria-invalid')).toBe('true')
    await waitFor(() => expect(document.activeElement).toBe(first))
  })

  // Review S2: row errors follow the row index, so deleting a row must not leave them under another row.
  it('clears the row errors when a row is deleted', async () => {
    backend()
    render(<App />)

    await fillKakao()
    fireEvent.change(row(1).getByLabelText('Hozzávaló'), { target: { value: 'ismeretlen' } })
    fireEvent.click(screen.getByRole('button', { name: 'Mentés' }))
    expect(await row(1).findByText('Válassz hozzávalót a listából.')).toBeTruthy()

    fireEvent.click(screen.getByRole('button', { name: 'Sor törlése: 1. hozzávaló' }))

    expect(screen.queryByText('Válassz hozzávalót a listából.')).toBeNull()
    expect(row(1).getByLabelText('Hozzávaló').getAttribute('aria-invalid')).toBeNull()
  })

  it('asks for an ingredient from the list before sending anything', async () => {
    const calls = backend()
    render(<App />)

    await fillKakao()
    fireEvent.change(row(2).getByLabelText('Hozzávaló'), { target: { value: 'kakaó' } })
    fireEvent.click(screen.getByRole('button', { name: 'Mentés' }))

    expect(await row(2).findByText('Válassz hozzávalót a listából.')).toBeTruthy()
    expect(calls.some((c) => c.key === createUrl)).toBe(false)
  })

  it('keeps the draft over a reload and tells so (M14); Mégse forgets it', async () => {
    backend()
    const { unmount } = render(<App />)
    await fillKakao()
    unmount()

    render(<App />)

    expect(await screen.findByText('Visszatöltöttem a félbehagyott receptet.')).toBeTruthy()
    expect((await screen.findByLabelText('Név') as HTMLInputElement).value).toBe('Kakaó')
    expect((row(2).getByLabelText('Hozzávaló') as HTMLInputElement).value).toBe('kakaópor')
    expect((row(2).getByLabelText('Mértékegység') as HTMLSelectElement).value).toBe('dkg')

    fireEvent.click(screen.getByRole('button', { name: 'Mégse' }))
    expect(await screen.findByRole('heading', { name: 'Receptek' })).toBeTruthy()
    expect(sessionStorage.getItem(draftKey)).toBeNull()
  })
})

describe('Új hozzávaló in the picker (N11)', () => {
  beforeEach(() => {
    window.location.hash = '#/receptek/uj'
  })

  it('offers a new ingredient only for an unknown name, creates it and then the row matches it', async () => {
    const calls = backend({ 'POST /api/v1/ingredients': [{ status: 201, body: zabtej }] })
    render(<App />)

    fireEvent.change(await screen.findByLabelText('Név'), { target: { value: 'Zabtejes kakaó' } })
    const field = row(1).getByLabelText('Hozzávaló')
    fireEvent.change(field, { target: { value: 'tej' } })
    expect(row(1).queryByRole('button', { name: /^Új hozzávaló/ })).toBeNull()
    fireEvent.change(field, { target: { value: ' zabtej ' } })
    fireEvent.click(row(1).getByRole('button', { name: 'Új hozzávaló: zabtej' }))

    const create = within(row(1).getByRole('group', { name: 'Új hozzávaló: zabtej' }))
    // Review S3: the button is gone, so the focus moves into the small form.
    await waitFor(() => expect(document.activeElement).toBe(create.getByLabelText('Mivel méred?')))
    fireEvent.change(create.getByLabelText('Mivel méred?'), { target: { value: 'volume' } })
    fireEvent.change(create.getByLabelText('Kategória'), { target: { value: 'beverages' } })
    fireEvent.click(create.getByRole('button', { name: 'Létrehozom' }))

    expect(await row(1).findByText('Létrehoztam: zabtej. Csak a te háztartásodban látszik.')).toBeTruthy()
    expect(calls.find((c) => c.key === 'POST /api/v1/ingredients')?.body).toEqual({ name: 'zabtej', dimension: 'volume', defaultCategory: 'beverages' })
    await waitFor(() => expect(row(1).queryByRole('button', { name: 'Új hozzávaló: zabtej' })).toBeNull())
    expect(document.activeElement).toBe(field)
    expect(Array.from((row(1).getByLabelText('Mértékegység') as HTMLSelectElement).options, (o) => o.value)).toEqual(['ml', 'dl', 'l'])
  })

  // Review S1: the missing choice is tied to its field and gets the focus (ux_flows a11y).
  it('ties a missing measure to its field and focuses it, without a request', async () => {
    const calls = backend()
    render(<App />)

    await screen.findByRole('group', { name: '1. hozzávaló' })
    fireEvent.change(row(1).getByLabelText('Hozzávaló'), { target: { value: 'zabtej' } })
    fireEvent.click(row(1).getByRole('button', { name: 'Új hozzávaló: zabtej' }))
    const create = within(row(1).getByRole('group', { name: 'Új hozzávaló: zabtej' }))
    fireEvent.click(create.getByRole('button', { name: 'Létrehozom' }))

    const measure = create.getByLabelText('Mivel méred?')
    await waitFor(() => expect(measure.getAttribute('aria-invalid')).toBe('true'))
    expect(document.getElementById(measure.getAttribute('aria-describedby')!)?.textContent)
      .toBe('Válaszd ki, mivel méred: tömeg, térfogat vagy darab.')
    await waitFor(() => expect(document.activeElement).toBe(measure))
    expect(calls.some((c) => c.key === 'POST /api/v1/ingredients')).toBe(false)
  })

  // Review S3: Mégse removes the small form; the focus goes back to the ingredient field.
  it('gives the focus back to the ingredient field on Mégse', async () => {
    backend()
    render(<App />)

    await screen.findByRole('group', { name: '1. hozzávaló' })
    const field = row(1).getByLabelText('Hozzávaló')
    fireEvent.change(field, { target: { value: 'zabtej' } })
    fireEvent.click(row(1).getByRole('button', { name: 'Új hozzávaló: zabtej' }))
    fireEvent.click(within(row(1).getByRole('group', { name: 'Új hozzávaló: zabtej' })).getByRole('button', { name: 'Mégse' }))

    await waitFor(() => expect(document.activeElement).toBe(field))
  })

  it('shows the name conflict and keeps the small form open', async () => {
    backend({
      'POST /api/v1/ingredients': [{ status: 409, body: { code: 'INGREDIENT_NAME_TAKEN', title: 'Ilyen nevű hozzávaló már van. Válaszd ki a listából.' } }],
    })
    render(<App />)

    await screen.findByRole('group', { name: '1. hozzávaló' })
    fireEvent.change(row(1).getByLabelText('Hozzávaló'), { target: { value: 'Tej2' } })
    fireEvent.click(row(1).getByRole('button', { name: 'Új hozzávaló: Tej2' }))
    const create = within(row(1).getByRole('group', { name: 'Új hozzávaló: Tej2' }))
    fireEvent.change(create.getByLabelText('Mivel méred?'), { target: { value: 'volume' } })
    fireEvent.click(create.getByRole('button', { name: 'Létrehozom' }))

    expect((await create.findByText('Ilyen nevű hozzávaló már van. Válaszd ki a listából.')).getAttribute('role')).toBe('alert')
    expect(create.getByRole('button', { name: 'Létrehozom' })).toBeTruthy()
  })

  it('is offered on the pantry form too', async () => {
    window.location.hash = '#/'
    backend({ 'GET /api/v1/pantry-items': [{ status: 200, body: [] }] })
    render(<App />)

    const form = within(await screen.findByRole('form', { name: 'Új tétel' }))
    fireEvent.change(form.getByLabelText('Hozzávaló'), { target: { value: 'zabtej' } })

    expect(form.getByRole('button', { name: 'Új hozzávaló: zabtej' })).toBeTruthy()
  })
})
