import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest'
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
const token: Reply = { status: 200, body: { requestToken: 'token-1' } }
const categories: Reply = {
  status: 200,
  body: [
    { category: 'dairy', shelfLifeDays: 7 },
    { category: 'dryGoods', shelfLifeDays: 90 },
    { category: 'other', shelfLifeDays: null },
  ],
}
const ingredients: Reply = {
  status: 200,
  body: [
    { id: 'i-tejfol', name: 'tejföl', dimension: 'mass', defaultCategory: 'dairy' },
    { id: 'i-tej', name: 'tej', dimension: 'volume', defaultCategory: 'dairy' },
  ],
}
const sourCream = {
  id: 'p-1', ingredientId: 'i-tejfol', ingredientName: 'tejföl', amount: 20, unit: 'dkg', quantity: 200,
  category: 'dairy', expiryDate: '2026-10-17', expiryEstimated: true, expired: false, soonExpiring: false, version: 5,
}
const oldMilk = {
  id: 'p-2', ingredientId: 'i-tej', ingredientName: 'tej', amount: 1, unit: 'l', quantity: 1000,
  category: 'dairy', expiryDate: '2026-10-09', expiryEstimated: false, expired: true, soonExpiring: false, version: 7,
}
const rice = {
  id: 'p-3', ingredientId: 'i-rizs', ingredientName: 'rizs', amount: 0.5, unit: 'kg', quantity: 500,
  category: 'dryGoods', expiryDate: '2026-10-11', expiryEstimated: false, expired: false, soonExpiring: true, version: 9,
}
const pantry = (...items: unknown[]): Reply => ({ status: 200, body: items })

function pantryBackend(pantryReplies: Reply[], extra: Record<string, Reply[]> = {}) {
  return fakeBackend({
    'GET /api/v1/auth/me': [signedIn],
    'GET /api/v1/auth/antiforgery': [token],
    'GET /api/v1/categories': [categories],
    'GET /api/v1/ingredients': [ingredients],
    'GET /api/v1/pantry-items': pantryReplies,
    ...extra,
  })
}

const form = () => within(screen.getByRole('form', { name: 'Új tétel' }))
const filters = () => within(screen.getByRole('search'))

// jsdom has no modal dialogs; the browser's showModal/close only toggle the open attribute here.
beforeAll(() => {
  HTMLDialogElement.prototype.showModal = function showModal(this: HTMLDialogElement) { this.setAttribute('open', '') }
  HTMLDialogElement.prototype.close = function close(this: HTMLDialogElement) { this.removeAttribute('open') }
})
beforeEach(() => resetClientForTests())
afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
})

describe('pantry list', () => {
  it('lists items with amount, unit, expiry date and badges', async () => {
    pantryBackend([pantry(oldMilk, rice, sourCream)])
    render(<App />)

    const sourCreamRow = await screen.findByRole('listitem', { name: /tejföl/ })
    expect(sourCreamRow.textContent).toContain('20 dkg')
    expect(sourCreamRow.textContent).toContain('okt. 17.')
    expect(sourCreamRow.textContent).toContain('(becsült)')
    expect(screen.getByRole('listitem', { name: /tej\b/ }).textContent).toContain('Lejárt')
    expect(screen.getByRole('listitem', { name: /rizs/ }).textContent).toContain('0,5 kg')
    expect(screen.getByRole('listitem', { name: /rizs/ }).textContent).toContain('Hamarosan lejár')
  })

  it('shows the empty pantry and the empty filter result', async () => {
    pantryBackend([pantry()], { 'GET /api/v1/pantry-items?search=xyz': [pantry()] })
    render(<App />)

    expect(await screen.findByText('Még üres a kamrád.')).toBeTruthy()
    expect(screen.getByText('Rögzítsd az első tételt az »Új tétel« űrlappal.')).toBeTruthy()

    fireEvent.change(filters().getByLabelText('Keresés'), { target: { value: 'xyz' } })

    expect(await screen.findByText('Nincs a szűrésnek megfelelő tétel.')).toBeTruthy()
  })

  it('shows an error with a retry button', async () => {
    pantryBackend([
      { status: 500, body: { code: 'INTERNAL_ERROR', title: 'Váratlan hiba történt. Próbáld újra később.' } },
      pantry(sourCream),
    ])
    render(<App />)

    expect(await screen.findByText('Váratlan hiba történt. Próbáld újra később.')).toBeTruthy()
    fireEvent.click(screen.getByRole('button', { name: 'Újra' }))

    expect(await screen.findByRole('listitem', { name: /tejföl/ })).toBeTruthy()
  })

  it('sends the search, category and soon-expiring filters', async () => {
    const calls = pantryBackend([pantry(sourCream)], {
      'GET /api/v1/pantry-items?search=tej': [pantry(sourCream)],
      'GET /api/v1/pantry-items?search=tej&category=dairy': [pantry(sourCream)],
      'GET /api/v1/pantry-items?search=tej&category=dairy&expiringSoon=true': [pantry()],
    })
    render(<App />)
    await screen.findByRole('listitem', { name: /tejföl/ })

    fireEvent.change(filters().getByLabelText('Keresés'), { target: { value: 'tej' } })
    await waitFor(() => expect(calls.some((c) => c.url === '/api/v1/pantry-items?search=tej')).toBe(true))
    fireEvent.change(filters().getByLabelText('Kategória'), { target: { value: 'dairy' } })
    fireEvent.click(filters().getByLabelText('Csak a hamarosan lejárók'))

    await waitFor(() =>
      expect(calls.at(-1)?.url).toBe('/api/v1/pantry-items?search=tej&category=dairy&expiringSoon=true'))
    expect(filters().getByRole('option', { name: 'Tejtermék' })).toBeTruthy()
  })
})

describe('new item form', () => {
  it('offers only the units of the chosen ingredient', async () => {
    pantryBackend([pantry()])
    render(<App />)
    await screen.findByText('Még üres a kamrád.')

    fireEvent.change(form().getByLabelText('Hozzávaló'), { target: { value: 'tejföl' } })
    const units = () => form().getAllByRole('option').map((o) => o.textContent)
      .filter((u) => ['g', 'dkg', 'kg', 'ml', 'dl', 'l', 'db'].includes(u ?? ''))
    expect(units()).toEqual(['g', 'dkg', 'kg'])

    fireEvent.change(form().getByLabelText('Hozzávaló'), { target: { value: 'tej' } })
    expect(units()).toEqual(['ml', 'dl', 'l'])
  })

  it('rejects an unknown ingredient without sending a request', async () => {
    const calls = pantryBackend([pantry()])
    render(<App />)
    await screen.findByText('Még üres a kamrád.')

    fireEvent.change(form().getByLabelText('Hozzávaló'), { target: { value: 'sajtt' } })
    fireEvent.change(form().getByLabelText('Mennyiség'), { target: { value: '1' } })
    fireEvent.click(form().getByRole('button', { name: 'Hozzáadás' }))

    expect(await form().findByText('Válassz hozzávalót a listából.')).toBeTruthy()
    expect(calls.some((c) => c.init.method === 'POST')).toBe(false)
  })

  it('adds an item and shows server field errors under the fields', async () => {
    const amountError = 'A mennyiség 0,001 és 100 000 között legyen, legfeljebb 3 tizedesjeggyel.'
    const calls = pantryBackend([pantry(), pantry(sourCream)], {
      'POST /api/v1/pantry-items': [
        { status: 400, body: { code: 'VALIDATION_FAILED', title: 'Néhány mező hibás.', errors: { amount: [amountError] } } },
        { status: 201, body: sourCream },
      ],
    })
    render(<App />)
    await screen.findByText('Még üres a kamrád.')

    fireEvent.change(form().getByLabelText('Hozzávaló'), { target: { value: 'tejföl' } })
    fireEvent.change(form().getByLabelText('Mennyiség'), { target: { value: '0.0001' } })
    fireEvent.click(form().getByRole('button', { name: 'Hozzáadás' }))

    const amountInput = form().getByLabelText('Mennyiség')
    const fieldError = await form().findByText(amountError)
    expect(amountInput.getAttribute('aria-describedby')).toBe(fieldError.id)
    await waitFor(() => expect(document.activeElement).toBe(amountInput))

    fireEvent.change(amountInput, { target: { value: '20' } })
    fireEvent.change(form().getByLabelText('Mértékegység'), { target: { value: 'dkg' } })
    fireEvent.click(form().getByRole('button', { name: 'Hozzáadás' }))

    expect(await screen.findByRole('listitem', { name: /tejföl/ })).toBeTruthy()
    const post = calls.filter((c) => c.init.method === 'POST').at(-1)!
    expect(JSON.parse(post.init.body as string)).toEqual({
      ingredientId: 'i-tejfol', amount: 20, unit: 'dkg', category: 'dairy', expiryDate: null,
    })
  })
})

describe('item editor', () => {
  async function openEditor() {
    fireEvent.click(within(await screen.findByRole('listitem', { name: /tejföl/ })).getByRole('button', { name: 'Módosítás' }))
    return within(screen.getByRole('dialog', { name: 'tejföl módosítása' }))
  }

  it('asks for a reason only when the amount decreases, and sends the version and the reason', async () => {
    const calls = pantryBackend([pantry(sourCream), pantry({ ...sourCream, amount: 15, quantity: 150, version: 6 })], {
      'PUT /api/v1/pantry-items/p-1': [{ status: 200, body: { ...sourCream, amount: 15, quantity: 150, version: 6 } }],
    })
    render(<App />)
    const dialog = await openEditor()

    expect(dialog.queryByLabelText('Miért csökken?')).toBeNull()
    fireEvent.change(dialog.getByLabelText('Mennyiség'), { target: { value: '15' } })
    fireEvent.change(dialog.getByLabelText('Miért csökken?'), { target: { value: 'discarded' } })
    fireEvent.click(dialog.getByRole('button', { name: 'Mentés' }))

    await waitFor(() => expect(screen.queryByRole('dialog')).toBeNull())
    const put = calls.find((c) => c.init.method === 'PUT')!
    expect(JSON.parse(put.init.body as string)).toEqual({
      amount: 15, unit: 'dkg', category: 'dairy', expiryDate: null, reason: 'discarded', version: 5,
    })
    expect((await screen.findByRole('listitem', { name: /tejföl/ })).textContent).toContain('15 dkg')
  })

  it('on a conflict shows the fresh values and highlights what changed', async () => {
    const fresh = { ...sourCream, amount: 15, quantity: 150, version: 6 }
    pantryBackend([pantry(sourCream), pantry(fresh)], {
      'PUT /api/v1/pantry-items/p-1': [{
        status: 409,
        body: { code: 'PANTRY_ITEM_MODIFIED', title: 'Ezt a tételt közben máshol módosították. Nézd át a friss adatokat, és mentsd újra.' },
      }],
    })
    render(<App />)
    const dialog = await openEditor()

    fireEvent.change(dialog.getByLabelText('Mennyiség'), { target: { value: '10' } })
    fireEvent.change(dialog.getByLabelText('Miért csökken?'), { target: { value: 'consumed' } })
    fireEvent.click(dialog.getByRole('button', { name: 'Mentés' }))

    expect(await dialog.findByText('Közben változott – mennyiség: 20 dkg → 15 dkg')).toBeTruthy()
    expect(dialog.getByText(/közben máshol módosították/)).toBeTruthy()
    expect((dialog.getByLabelText('Mennyiség') as HTMLInputElement).value).toBe('15')
  })

  it('on a deleted item closes the dialog, refreshes the list and tells why', async () => {
    const calls = pantryBackend([pantry(sourCream), pantry()], {
      'PUT /api/v1/pantry-items/p-1': [{ status: 404, body: { code: 'PANTRY_ITEM_NOT_FOUND', title: 'Ez a tétel már nincs a kamrádban.' } }],
    })
    render(<App />)
    const dialog = await openEditor()

    fireEvent.click(dialog.getByRole('button', { name: 'Törlés' }))
    fireEvent.change(dialog.getByLabelText('Miért csökken?'), { target: { value: 'consumed' } })
    fireEvent.click(dialog.getByRole('button', { name: 'Törlés' }))

    expect(await screen.findByText('Ez a tétel már nincs a kamrádban. Frissítettem a listát.')).toBeTruthy()
    expect(screen.queryByRole('dialog')).toBeNull()
    expect(calls.filter((c) => c.url === '/api/v1/pantry-items')).toHaveLength(2)
  })
})

describe('session', () => {
  it('an expired session returns to sign-in with the ux_flows message', async () => {
    pantryBackend([{ status: 401, body: { code: 'UNAUTHENTICATED', title: 'Jelentkezz be a folytatáshoz.' } }])
    render(<App />)

    expect(await screen.findByRole('heading', { name: 'Bejelentkezés' })).toBeTruthy()
    expect(screen.getByText('Biztonsági okból kiléptettünk. Jelentkezz be újra, és folytathatod.')).toBeTruthy()
  })
})
