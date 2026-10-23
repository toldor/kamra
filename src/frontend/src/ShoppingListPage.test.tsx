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

const ok = (body: unknown): Reply => ({ status: 200, body })
const noContent: Reply = { status: 204 }

const ingredient = (name: string, dimension: string, minimum: object | null = null) => ({
  id: `i-${name}`, name, dimension, defaultCategory: 'other', minimum,
})
const ingredients = [
  ingredient('paprika', 'mass'),
  ingredient('tej', 'volume', { value: 500, unit: 'ml' }),
  ingredient('tejföl', 'mass'),
]

// 22:30 UTC on Oct 17 is already Oct 18 in Budapest: the reason shows the Budapest day.
const depleted = (extra: object = {}) => ({
  id: 's-paprika', ingredientId: 'i-paprika', ingredientName: 'paprika', dimension: 'mass', trigger: 'depleted',
  createdAt: '2026-10-17T22:30:00Z', recipeName: 'Lecsó', currentAmount: null, minimumAmount: null, suggestedAmount: null, ...extra,
})
const belowMinimum = (extra: object = {}) => ({
  id: 's-tej', ingredientId: 'i-tej', ingredientName: 'tej', dimension: 'volume', trigger: 'belowMinimum',
  createdAt: '2026-10-18T08:00:00Z', recipeName: null,
  currentAmount: { value: 400, unit: 'ml' }, minimumAmount: { value: 500, unit: 'ml' }, suggestedAmount: { value: 100, unit: 'ml' }, ...extra,
})
const item = (name: string, amount: object | null, checked = false) => ({
  id: `l-${name}`, ingredientId: `i-${name}`, ingredientName: name, amount, checked,
})

const suggestions = 'GET /api/v1/shopping-suggestions'
const list = 'GET /api/v1/shopping-list'
const addItem = 'POST /api/v1/shopping-list/items'

function backend(extra: Record<string, Reply[]> = {}) {
  return fakeBackend({
    'GET /api/v1/auth/me': [ok({ email: 'tomi@example.com', householdId: 'h-1' })],
    'GET /api/v1/auth/antiforgery': [ok({ requestToken: 'token-1' })],
    'GET /api/v1/ingredients': [ok(ingredients)],
    [suggestions]: [ok([])],
    [list]: [ok([])],
    ...extra,
  })
}

const count = (calls: { key: string }[], key: string) => calls.filter((c) => c.key === key).length
const bodyOf = (calls: { key: string; body: unknown }[], key: string) => calls.find((c) => c.key === key)?.body
const article = (name: string) => screen.findByRole('article', { name })
const form = (name: string) => screen.getByRole('form', { name })
const notice = (text: string) => screen.findByText(text)

beforeEach(() => {
  resetClientForTests()
  window.location.hash = '#/bevasarlolista'
})
afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
  window.location.hash = ''
})

describe('shopping list page', () => {
  it('is reached from the menu', async () => {
    window.location.hash = '#/mit-fozzek'
    backend({ 'GET /api/v1/recommendations': [ok({ pantryIsEmpty: true, makeable: [], almostMakeable: [] })] })
    render(<App />)

    fireEvent.click(await screen.findByRole('link', { name: 'Bevásárlólista' }))

    expect(await screen.findByRole('heading', { level: 1, name: 'Bevásárlólista' })).toBeTruthy()
    expect(screen.getByRole('link', { name: 'Bevásárlólista' }).getAttribute('aria-current')).toBe('page')
    expect(window.location.hash).toBe('#/bevasarlolista')
  })

  it('shows an error and loads again on retry', async () => {
    backend({ [list]: [{ status: 500, body: { code: 'INTERNAL_ERROR', title: 'Váratlan hiba történt. Próbáld újra később.' } }, ok([])] })
    render(<App />)

    expect((await screen.findByText('Váratlan hiba történt. Próbáld újra később.')).getAttribute('role')).toBe('alert')
    fireEvent.click(screen.getByRole('button', { name: 'Újra' }))

    expect(await screen.findByText('A listád üres. Vegyél fel tételt alább.')).toBeTruthy()
  })

  it('tells what to do with an empty list, with and without suggestions', async () => {
    backend()
    render(<App />)

    expect(await screen.findByText('A listád üres. Vegyél fel tételt alább.')).toBeTruthy()
    expect(screen.queryByRole('heading', { name: /Javaslatok/ })).toBeNull()
    cleanup()

    backend({ [suggestions]: [ok([depleted()])] })
    render(<App />)

    expect(await screen.findByText('A listád üres. Fogadj el egy javaslatot, vagy vegyél fel tételt alább.')).toBeTruthy()
    expect(screen.getByRole('heading', { name: 'Javaslatok (1)' })).toBeTruthy()
  })

  it('shows the list with amounts, checked items and the rule about suggestions', async () => {
    backend({ [list]: [ok([item('paprika', { value: 500, unit: 'g' }), item('tejföl', null, true)])] })
    render(<App />)

    const paprika = await screen.findByRole('checkbox', { name: 'paprika – 500 g' })
    expect((paprika as HTMLInputElement).checked).toBe(false)
    expect((screen.getByRole('checkbox', { name: 'tejföl' }) as HTMLInputElement).checked).toBe(true)
    expect(screen.getByText('Amíg egy hozzávaló pipa nélkül a listán van, nem javaslom újra.')).toBeTruthy()
  })
})

describe('shopping suggestions', () => {
  it('explains a depleted suggestion and accepts it without an amount', async () => {
    const calls = backend({
      [suggestions]: [ok([depleted()]), ok([])],
      'POST /api/v1/shopping-suggestions/s-paprika/accept': [noContent],
      [list]: [ok([]), ok([item('paprika', null)])],
    })
    render(<App />)

    const row = await article('paprika')
    expect(row.textContent).toContain('Elfogyott (okt. 18., főzés: Lecsó).')
    expect((within(row).getByLabelText('Mennyit vegyél?') as HTMLInputElement).value).toBe('')
    expect(row.textContent).toContain('Ha üresen hagyod, mennyiség nélkül kerül a listára.')
    expect(row.textContent).toContain('Hozzáadom: felkerül a bevásárlólistára. Nem kell: nem ajánlom újra, amíg újra nem veszel belőle.')

    fireEvent.click(within(row).getByRole('button', { name: 'Hozzáadom' }))

    expect(await notice('Felvettem a listára: paprika.')).toBeTruthy()
    expect(bodyOf(calls, 'POST /api/v1/shopping-suggestions/s-paprika/accept')).toEqual({})
    expect(await screen.findByRole('checkbox', { name: 'paprika' })).toBeTruthy()
    expect(count(calls, suggestions)).toBe(2)
  })

  it('keeps the buttons disabled until the reload after a decision has arrived', async () => {
    let release = () => {}
    const reloaded = new Promise<void>((resolve) => {
      release = resolve
    })
    const calls = backend({
      [suggestions]: [ok([depleted(), belowMinimum()]), { status: 200, body: [belowMinimum()], wait: reloaded }],
      'POST /api/v1/shopping-suggestions/s-paprika/accept': [noContent],
    })
    render(<App />)

    fireEvent.click(within(await article('paprika')).getByRole('button', { name: 'Hozzáadom' }))

    await waitFor(() => expect(count(calls, suggestions)).toBe(2))
    const other = () => within(screen.getByRole('article', { name: 'tej' })).getByRole('button', { name: 'Hozzáadom' }) as HTMLButtonElement
    expect(other().disabled).toBe(true)
    release()
    await waitFor(() => expect(screen.queryByRole('article', { name: 'paprika' })).toBeNull())
    expect(other().disabled).toBe(false)
  })

  it('names the day only when the stock ran out by hand', async () => {
    backend({ [suggestions]: [ok([depleted({ recipeName: null })])] })
    render(<App />)

    expect((await article('paprika')).textContent).toContain('Elfogyott (okt. 18.).')
  })

  it('explains a below-minimum suggestion and accepts the prefilled amount', async () => {
    const calls = backend({
      [suggestions]: [ok([belowMinimum()]), ok([])],
      'POST /api/v1/shopping-suggestions/s-tej/accept': [noContent],
    })
    render(<App />)

    const row = await article('tej')
    expect(row.textContent).toContain('A minimumszint (500 ml) alá csökkent: 400 ml van.')
    expect((within(row).getByLabelText('Mennyit vegyél?') as HTMLInputElement).value).toBe('100')
    expect((within(row).getByLabelText('Mértékegység') as HTMLSelectElement).value).toBe('ml')
    expect(row.textContent).toContain('Ennyi hiányzik a minimumszintig.')
    expect(row.textContent).toContain('Nem kell: nem ajánlom újra, amíg a készlet a minimumszint fölé nem kerül.')

    fireEvent.change(within(row).getByLabelText('Mértékegység'), { target: { value: 'l' } })
    fireEvent.change(within(row).getByLabelText('Mennyit vegyél?'), { target: { value: '1' } })
    fireEvent.click(within(row).getByRole('button', { name: 'Hozzáadom' }))

    expect(await notice('Felvettem a listára: tej 1 l.')).toBeTruthy()
    expect(bodyOf(calls, 'POST /api/v1/shopping-suggestions/s-tej/accept')).toEqual({ amount: 1, unit: 'l' })
  })

  it('says what happens after a rejection, for both reasons', async () => {
    const calls = backend({
      [suggestions]: [ok([depleted(), belowMinimum({ minimumAmount: null, suggestedAmount: null })]), ok([belowMinimum()]), ok([])],
      'POST /api/v1/shopping-suggestions/s-paprika/reject': [noContent],
      'POST /api/v1/shopping-suggestions/s-tej/reject': [noContent],
    })
    render(<App />)

    // The minimum was removed meanwhile: the reason still says how much there is.
    expect((await article('tej')).textContent).toContain('A minimumszint alá csökkent: 400 ml van.')
    // No minimum, so nothing is prefilled: the hint is the one for an empty field.
    expect((await article('tej')).textContent).toContain('Ha üresen hagyod, mennyiség nélkül kerül a listára.')
    fireEvent.click(within(await article('paprika')).getByRole('button', { name: 'Nem kell' }))
    const rejected = await notice('Rendben. Amíg újra nem veszel belőle, nem ajánlom újra: paprika.')
    // The pressed button is gone with its row, so the focus moves to the notice (ux_flows a11y).
    await waitFor(() => expect(document.activeElement).toBe(rejected))

    fireEvent.click(within(await article('tej')).getByRole('button', { name: 'Nem kell' }))
    expect(await notice('Rendben. Amíg a készlet a minimumszint fölé nem kerül, nem ajánlom újra: tej.')).toBeTruthy()
    expect(bodyOf(calls, 'POST /api/v1/shopping-suggestions/s-tej/reject')).toEqual({})
  })

  it('shows an invalid amount at its field and keeps the suggestion', async () => {
    backend({
      [suggestions]: [ok([depleted()])],
      'POST /api/v1/shopping-suggestions/s-paprika/accept': [{
        status: 400,
        body: { code: 'VALIDATION_FAILED', title: 'Hibás adatok.', errors: { amount: ['A mennyiség 0,001 és 100 000 között legyen.'] } },
      }],
    })
    render(<App />)

    const input = within(await article('paprika')).getByLabelText('Mennyit vegyél?')
    fireEvent.change(input, { target: { value: '200000' } })
    fireEvent.click(within(await article('paprika')).getByRole('button', { name: 'Hozzáadom' }))

    await waitFor(() => expect(input.getAttribute('aria-invalid')).toBe('true'))
    expect(document.getElementById(input.getAttribute('aria-describedby')!)?.textContent).toBe('A mennyiség 0,001 és 100 000 között legyen.')
    expect(screen.getByRole('article', { name: 'paprika' })).toBeTruthy()
  })

  it('refreshes by itself when the suggestion was decided elsewhere', async () => {
    const calls = backend({
      [suggestions]: [ok([depleted()]), ok([])],
      'POST /api/v1/shopping-suggestions/s-paprika/accept': [{
        status: 409,
        body: { code: 'SHOPPING_SUGGESTION_STATE_CONFLICT', title: 'Erről a javaslatról már másképp döntöttél. Frissítsd a listát.' },
      }],
    })
    render(<App />)

    fireEvent.click(within(await article('paprika')).getByRole('button', { name: 'Hozzáadom' }))

    expect(await screen.findByText('Erről a javaslatról már másképp döntöttél, például egy másik ablakban. Frissítettem a listát.')).toBeTruthy()
    await waitFor(() => expect(screen.queryByRole('article', { name: 'paprika' })).toBeNull())
    expect(count(calls, suggestions)).toBe(2)
  })
})

describe('list items', () => {
  it('adds an item by hand, with or without an amount', async () => {
    const calls = backend({ [addItem]: [ok(item('tejföl', { value: 200, unit: 'g' }))] })
    render(<App />)

    await screen.findByText('A listád üres. Vegyél fel tételt alább.')
    const add = form('Új tétel a listára')
    expect(add.textContent).toContain('Ha a hozzávaló már a listán van, a mennyiség hozzáadódik; ha kipipáltad, az új mennyiséggel kerül vissza.')
    fireEvent.change(within(add).getByLabelText('Hozzávaló'), { target: { value: 'tejföl' } })
    fireEvent.change(within(add).getByLabelText('Mennyiség (nem kötelező)'), { target: { value: '200' } })
    fireEvent.click(within(add).getByRole('button', { name: 'Felveszem' }))

    expect(await notice('Felvettem a listára: tejföl 200 g.')).toBeTruthy()
    expect(bodyOf(calls, addItem)).toEqual({ ingredientId: 'i-tejföl', amount: 200, unit: 'g' })

    await waitFor(() => expect((within(add).getByLabelText('Hozzávaló') as HTMLInputElement).value).toBe(''))
    fireEvent.change(within(add).getByLabelText('Hozzávaló'), { target: { value: 'Paprika ' } })
    fireEvent.click(within(add).getByRole('button', { name: 'Felveszem' }))

    expect(await notice('Felvettem a listára: paprika.')).toBeTruthy()
    expect(calls.filter((c) => c.key === addItem)[1].body).toEqual({ ingredientId: 'i-paprika' })
  })

  it('asks for an ingredient from the list and sends nothing without one', async () => {
    const calls = backend()
    render(<App />)

    await screen.findByText('A listád üres. Vegyél fel tételt alább.')
    const add = form('Új tétel a listára')
    fireEvent.change(within(add).getByLabelText('Hozzávaló'), { target: { value: 'sárkánygyümölcs' } })
    fireEvent.click(within(add).getByRole('button', { name: 'Felveszem' }))

    expect((await within(add).findByText('Válassz hozzávalót a listából.')).className).toBe('field-error')
    expect(count(calls, addItem)).toBe(0)
  })

  it('checks an item off and deletes one', async () => {
    const calls = backend({
      [list]: [ok([item('paprika', { value: 500, unit: 'g' })]), ok([item('paprika', { value: 500, unit: 'g' }, true)]), ok([])],
      'PUT /api/v1/shopping-list/items/l-paprika': [ok(item('paprika', { value: 500, unit: 'g' }, true))],
      'DELETE /api/v1/shopping-list/items/l-paprika': [noContent],
    })
    render(<App />)

    fireEvent.click(await screen.findByRole('checkbox', { name: 'paprika – 500 g' }))
    // The tick shows at once, before the answer arrives (found by the e2e test).
    expect((screen.getByRole('checkbox', { name: 'paprika – 500 g' }) as HTMLInputElement).checked).toBe(true)
    await waitFor(() => expect(count(calls, list)).toBe(2))
    expect(bodyOf(calls, 'PUT /api/v1/shopping-list/items/l-paprika')).toEqual({ checked: true })

    fireEvent.click(screen.getByRole('button', { name: 'Törlés: paprika' }))

    const deleted = await notice('Töröltem a listáról: paprika.')
    await waitFor(() => expect(document.activeElement).toBe(deleted))
    expect(await screen.findByText('A listád üres. Vegyél fel tételt alább.')).toBeTruthy()
    expect(count(calls, 'DELETE /api/v1/shopping-list/items/l-paprika')).toBe(1)
    expect(bodyOf(calls, 'DELETE /api/v1/shopping-list/items/l-paprika')).toBeUndefined()
  })

  it('takes the tick back when it could not be saved', async () => {
    backend({
      [list]: [ok([item('paprika', null)])],
      'PUT /api/v1/shopping-list/items/l-paprika': [{ status: 0 }],
    })
    render(<App />)

    fireEvent.click(await screen.findByRole('checkbox', { name: 'paprika' }))

    expect(await screen.findByText(/Nem sikerült kapcsolódni a szerverhez/)).toBeTruthy()
    expect((screen.getByRole('checkbox', { name: 'paprika' }) as HTMLInputElement).checked).toBe(false)
  })

  it('refreshes by itself when the item is already gone', async () => {
    backend({
      [list]: [ok([item('paprika', null)]), ok([])],
      'DELETE /api/v1/shopping-list/items/l-paprika': [{
        status: 404, body: { code: 'SHOPPING_LIST_ITEM_NOT_FOUND', title: 'Ez a tétel már nincs a bevásárlólistán.' },
      }],
    })
    render(<App />)

    fireEvent.click(await screen.findByRole('button', { name: 'Törlés: paprika' }))

    expect(await screen.findByText('Ez a tétel már nincs a bevásárlólistán. Frissítettem a listát.')).toBeTruthy()
    expect(await screen.findByText('A listád üres. Vegyél fel tételt alább.')).toBeTruthy()
  })
})

describe('minimum levels', () => {
  it('lists, sets and removes minimum levels', async () => {
    const calls = backend({
      'PUT /api/v1/ingredients/i-tejföl/minimum': [noContent],
      'PUT /api/v1/ingredients/i-tej/minimum': [noContent],
    })
    render(<App />)

    const section = await screen.findByRole('region', { name: 'Minimumszintek' })
    expect(section.textContent).toContain('Ha egy hozzávalóból a minimumszint alá csökken a készlet, felveszem a bevásárlójavaslatok közé.')
    expect(within(section).getByText('tej: 500 ml')).toBeTruthy()

    const set = form('Új minimumszint')
    expect(set.textContent).toContain('Ha a hozzávalónak már van minimumszintje, az új érték lecseréli.')
    fireEvent.change(within(set).getByLabelText('Hozzávaló'), { target: { value: 'tejföl' } })
    fireEvent.change(within(set).getByLabelText('Mennyiség'), { target: { value: '20' } })
    fireEvent.change(within(set).getByLabelText('Mértékegység'), { target: { value: 'dkg' } })
    fireEvent.click(within(set).getByRole('button', { name: 'Beállítom' }))

    expect(await notice('Beállítottam a minimumszintet: tejföl, 20 dkg.')).toBeTruthy()
    expect(bodyOf(calls, 'PUT /api/v1/ingredients/i-tejföl/minimum')).toEqual({ amount: 20, unit: 'dkg' })
    // A new minimum may create a suggestion at once (M1), so the suggestions are loaded again.
    expect(count(calls, suggestions)).toBe(2)

    fireEvent.click(within(section).getByRole('button', { name: 'Minimumszint törlése: tej' }))

    expect(await notice('Töröltem a minimumszintet: tej.')).toBeTruthy()
    expect(bodyOf(calls, 'PUT /api/v1/ingredients/i-tej/minimum')).toEqual({})
  })

  it('asks for the amount of a minimum level and sends nothing without it', async () => {
    const calls = backend()
    render(<App />)

    await screen.findByText('tej: 500 ml')
    const set = form('Új minimumszint')
    fireEvent.change(within(set).getByLabelText('Hozzávaló'), { target: { value: 'tejföl' } })
    fireEvent.click(within(set).getByRole('button', { name: 'Beállítom' }))

    const input = within(set).getByLabelText('Mennyiség')
    await waitFor(() => expect(input.getAttribute('aria-invalid')).toBe('true'))
    expect(document.getElementById(input.getAttribute('aria-describedby')!)?.textContent).toBe('Add meg, mennyi legyen a minimumszint.')
    expect(calls.filter((c) => c.key.startsWith('PUT /api/v1/ingredients/'))).toHaveLength(0)
  })

  it('says so when no minimum is set', async () => {
    backend({ 'GET /api/v1/ingredients': [ok([ingredient('paprika', 'mass')])] })
    render(<App />)

    expect(await screen.findByText('Még nem adtál meg minimumszintet.')).toBeTruthy()
  })
})
