import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import App from './App'
import { resetClientForTests } from './api/client'
import { formatOn } from './format'

type Reply = { status: number; body?: unknown }

// US-2 on the Készlet page (us2_minispec K13-K15 and its texts): the quick entry panel, the proposal cards,
// one "Jóváhagyom (n)" for all (K1: A), "Kihagyom" per card, and the error states. A fake backend, as in
// PantryPage.test.tsx: each route answers from a queue (the last reply repeats), every request is recorded.
function fakeBackend(routes: Record<string, Reply[]>) {
  const calls: { key: string; body: unknown }[] = []
  vi.stubGlobal('fetch', vi.fn(async (url: string, init: RequestInit = {}) => {
    const key = `${init.method ?? 'GET'} ${url}`
    calls.push({ key, body: init.body ? JSON.parse(init.body as string) : undefined })
    const queue = routes[key]
    if (!queue) throw new Error(`Unexpected request: ${key}`)
    const reply = queue.length > 1 ? queue.shift()! : queue[0]
    return new Response(reply.body === undefined ? null : JSON.stringify(reply.body), { status: reply.status })
  }))
  return calls
}

const ok = (body: unknown): Reply => ({ status: 200, body })
const problem = (status: number, code: string, title: string, errors?: Record<string, string[]>): Reply =>
  ({ status, body: { code, title, ...(errors ? { errors } : {}) } })

const ingredients = [
  { id: 'i-tej', name: 'tej', dimension: 'volume', defaultCategory: 'dairy' },
  { id: 'i-paprika', name: 'paprika', dimension: 'mass', defaultCategory: 'vegetables' },
]
const milk = {
  id: 'ip-tej', text: '2 liter tejet', ingredientId: 'i-tej', ingredientName: 'tej', dimension: 'volume', amount: 2, unit: 'l',
  unitMismatch: false, expiryDate: '2026-11-13', expiryEstimated: false, expiryText: null, inStock: null,
}
const pesto = {
  id: 'ip-peszto', text: 'egy üveg pesztó', ingredientId: null, ingredientName: null, dimension: null, amount: 1, unit: null,
  unitMismatch: false, expiryDate: null, expiryEstimated: false, expiryText: null, inStock: null,
}
const pepper = {
  id: 'ip-paprika', text: '3 darab paprika', ingredientId: 'i-paprika', ingredientName: 'paprika', dimension: 'mass', amount: 3, unit: null,
  unitMismatch: true, expiryDate: '2026-11-10', expiryEstimated: true, expiryText: 'jövő hétfőig', inStock: null,
}
const sentence = 'Vettem 2 liter tejet és egy üveg pesztót'
const pantryItem = (name: string, amount: number, unit: string) => ({
  id: `p-${name}`, ingredientId: `i-${name}`, ingredientName: name, amount, unit, quantity: amount, category: 'dairy',
  expiryDate: '2026-11-13', expiryEstimated: false, expired: false, soonExpiring: false, version: 1,
})

function backend(available: boolean, extra: Record<string, Reply[]> = {}) {
  return fakeBackend({
    'GET /api/v1/auth/me': [ok({ email: 'tomi@example.com', householdId: 'h-1' })],
    'GET /api/v1/auth/antiforgery': [ok({ requestToken: 'token-1' })],
    'GET /api/v1/categories': [ok([{ category: 'dairy', shelfLifeDays: 7 }, { category: 'vegetables', shelfLifeDays: 3 }])],
    'GET /api/v1/ingredients': [ok(ingredients)],
    'GET /api/v1/pantry-items': [ok([])],
    'GET /api/v1/item-proposals/availability': [ok({ enabled: available })],
    ...extra,
  })
}

const panel = () => within(screen.getByRole('form', { name: 'Mondatos bevitel' }))
const card = (text: string) => within(screen.getByRole('group', { name: `»${text}«` }))

async function submit(text = sentence) {
  fireEvent.change(await screen.findByLabelText('Mit vettél?'), { target: { value: text } })
  fireEvent.click(panel().getByRole('button', { name: 'Feldolgozom' }))
}

beforeEach(() => resetClientForTests())
afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
})

describe('the quick entry panel', () => {
  it('is not shown when the switch is off, and the empty pantry keeps the manual hint', async () => {
    backend(false)
    render(<App />)

    expect(await screen.findByText('Rögzítsd az első tételt az »Új tétel« űrlappal.')).toBeTruthy()
    expect(screen.queryByRole('form', { name: 'Mondatos bevitel' })).toBeNull()
  })

  it('is not shown when its availability cannot be read: the manual form still works', async () => {
    fakeBackend({
      'GET /api/v1/auth/me': [ok({ email: 'tomi@example.com', householdId: 'h-1' })],
      'GET /api/v1/categories': [ok([{ category: 'dairy', shelfLifeDays: 7 }])],
      'GET /api/v1/ingredients': [ok(ingredients)],
      'GET /api/v1/pantry-items': [ok([])],
      'GET /api/v1/item-proposals/availability': [problem(500, 'INTERNAL_ERROR', 'Váratlan hiba történt.')],
    })
    render(<App />)

    expect(await screen.findByRole('form', { name: 'Új tétel' })).toBeTruthy()
    expect(screen.queryByRole('form', { name: 'Mondatos bevitel' })).toBeNull()
  })

  it('is shown with its label, hint and the sentence hint of the empty pantry', async () => {
    backend(true)
    render(<App />)

    expect(await screen.findByRole('form', { name: 'Mondatos bevitel' })).toBeTruthy()
    expect(panel().getByLabelText('Mit vettél?')).toBeTruthy()
    expect(panel().getByText('Írd le egy mondatban, például: »2 liter tej, 10 tojás és fél kiló paradicsom, a tej péntekig jó«. Mentés előtt átnézheted.'))
      .toBeTruthy()
    expect(await screen.findByText('Írd le egy mondatban, mit vettél – például: »vettem 2 liter tejet, 10 tojást és fél kiló paradicsomot, a tej pénteken jár le«.'))
      .toBeTruthy()
  })

  it('"Inkább tételenként rögzítem" moves the focus to the manual form', async () => {
    backend(true)
    render(<App />)

    fireEvent.click(await screen.findByRole('button', { name: 'Inkább tételenként rögzítem' }))

    expect(document.activeElement).toBe(within(screen.getByRole('form', { name: 'Új tétel' })).getByLabelText('Hozzávaló'))
  })

  it('sends the sentence, shows the cards and moves the focus to them', async () => {
    const calls = backend(true, { 'POST /api/v1/item-proposals': [{ status: 201, body: { items: [milk, pesto] } }] })
    render(<App />)

    await submit()

    const heading = await screen.findByRole('heading', { name: 'Javaslatok (2)' })
    await waitFor(() => expect(document.activeElement).toBe(heading))
    expect(calls.find((c) => c.key === 'POST /api/v1/item-proposals')?.body).toEqual({ text: sentence })
    expect(screen.getByText('Nézd át, javíts, ha kell, majd hagyd jóvá. Jóváhagyás nélkül semmi nem kerül a kamrádba.')).toBeTruthy()
    expect((card('2 liter tejet').getByLabelText('Hozzávaló') as HTMLInputElement).value).toBe('tej')
    expect((card('2 liter tejet').getByLabelText('Mennyiség') as HTMLInputElement).value).toBe('2')
    expect((card('2 liter tejet').getByLabelText('Mértékegység') as HTMLSelectElement).value).toBe('l')
    expect((card('2 liter tejet').getByLabelText(/Lejárat/) as HTMLInputElement).value).toBe('2026-11-13')
    expect(card('egy üveg pesztó').getByText('Nem találtam a hozzávalók között: »egy üveg pesztó«. Válassz egyet, vagy vegyél fel újat.')).toBeTruthy()
    expect(screen.getByRole('button', { name: 'Jóváhagyom (2)' })).toBeTruthy()
    expect((screen.getByLabelText('Mit vettél?') as HTMLTextAreaElement).value).toBe(sentence)
  })

  it('explains an estimated date from unrecognised words, a unit that does not fit, and the stock at home', async () => {
    const withStock = { ...milk, inStock: { amount: { value: 0.5, unit: 'l' }, earliestExpiry: '2026-11-12', itemCount: 1 } }
    const severalAtHome = { ...milk, id: 'ip-tej2', text: '1 liter tejet', inStock: { amount: { value: 1.5, unit: 'l' }, earliestExpiry: '2026-11-12', itemCount: 2 } }
    backend(true, { 'POST /api/v1/item-proposals': [{ status: 201, body: { items: [withStock, severalAtHome, pepper] } }] })
    render(<App />)

    await submit()

    await screen.findByRole('heading', { name: 'Javaslatok (3)' })
    expect(card('2 liter tejet').getByText('Már van otthon: tej 0,5 l, nov. 12-én jár le.')).toBeTruthy()
    expect(card('1 liter tejet').getByText('Már van otthon: tej 1,5 l, a legkorábbi nov. 12-én jár le.')).toBeTruthy()
    expect(card('3 darab paprika').getByText('becsült')).toBeTruthy()
    expect(card('3 darab paprika').getByText('»jövő hétfőig«: ezt nem tudtam dátummá alakítani, ezért becsült lejáratot adtam. Javítsd, ha kell.'))
      .toBeTruthy()
    // The manual form's wording (PantryInput.UnitForMessage): an accusative of any ingredient name cannot be formed.
    expect(card('3 darab paprika').getByText('A(z) paprika tömegben mérhető: g, dkg vagy kg. Add meg így a mennyiséget.')).toBeTruthy()
    expect((card('3 darab paprika').getByLabelText('Mértékegység') as HTMLSelectElement).value).toBe('')
  })

  it('sits at the top of the side column, above the filters', async () => {
    backend(true)
    render(<App />)

    const quick = await screen.findByRole('form', { name: 'Mondatos bevitel' })
    expect(quick.compareDocumentPosition(screen.getByRole('search')) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
  })

  it('links the hints to their fields and drops them once the card is fixed', async () => {
    backend(true, { 'POST /api/v1/item-proposals': [{ status: 201, body: { items: [{ ...pesto, amount: null }, pepper] } }] })
    render(<App />)
    await submit()
    await screen.findByRole('heading', { name: 'Javaslatok (2)' })

    const describedBy = (field: HTMLElement) => document.getElementById(field.getAttribute('aria-describedby') ?? '')?.textContent
    const pestoName = card('egy üveg pesztó').getByLabelText('Hozzávaló')
    const pestoAmount = card('egy üveg pesztó').getByLabelText('Mennyiség')
    const pepperUnit = card('3 darab paprika').getByLabelText('Mértékegység')
    expect(describedBy(pestoName)).toBe('Nem találtam a hozzávalók között: »egy üveg pesztó«. Válassz egyet, vagy vegyél fel újat.')
    expect(describedBy(pestoAmount)).toBe('Add meg a mennyiséget.')
    expect(describedBy(card('egy üveg pesztó').getByLabelText('Mértékegység'))).toBe('Add meg a mértékegységet.')
    expect(describedBy(pepperUnit)).toBe('A(z) paprika tömegben mérhető: g, dkg vagy kg. Add meg így a mennyiséget.')
    expect(describedBy(card('3 darab paprika').getByLabelText(/Lejárat/)))
      .toBe('»jövő hétfőig«: ezt nem tudtam dátummá alakítani, ezért becsült lejáratot adtam. Javítsd, ha kell.')

    fireEvent.change(pestoName, { target: { value: 'tej' } })
    fireEvent.change(pepperUnit, { target: { value: 'dkg' } })

    expect(card('egy üveg pesztó').queryByText(/Nem találtam a hozzávalók között/)).toBeNull()
    expect(card('3 darab paprika').queryByText(/tömegben mérhető/)).toBeNull()
  })

  it('shows the busy state while the sentence is processed', async () => {
    let answer: (value: Response) => void = () => {}
    backend(true)
    const original = globalThis.fetch
    vi.stubGlobal('fetch', vi.fn((url: string, init: RequestInit = {}) =>
      `${init.method ?? 'GET'} ${url}` === 'POST /api/v1/item-proposals'
        ? new Promise<Response>((resolve) => { answer = resolve })
        : original(url, init)))
    render(<App />)

    await submit()

    const button = await panel().findByRole('button', { name: 'Feldolgozás…' })
    expect((button as HTMLButtonElement).disabled).toBe(true)
    expect(panel().getByText('Ez néhány másodpercig is tarthat.')).toBeTruthy()
    answer(new Response(JSON.stringify({ items: [milk] }), { status: 201 }))
    expect(await screen.findByRole('heading', { name: 'Javaslatok (1)' })).toBeTruthy()
  })
})

describe('approving and skipping', () => {
  it('approves every card, sends only a recognised or edited expiry, tells what went in and reloads the list', async () => {
    const calls = backend(true, {
      'POST /api/v1/item-proposals': [{ status: 201, body: { items: [milk, { ...pepper, unit: null, unitMismatch: false, expiryText: null }] } }],
      'POST /api/v1/item-proposals/ip-tej/approve': [{ status: 201, body: pantryItem('tej', 2, 'l') }],
      'POST /api/v1/item-proposals/ip-paprika/approve': [{ status: 201, body: pantryItem('paprika', 50, 'dkg') }],
      'GET /api/v1/pantry-items': [ok([]), ok([pantryItem('tej', 2, 'l'), pantryItem('paprika', 50, 'dkg')])],
    })
    render(<App />)
    await submit()
    await screen.findByRole('heading', { name: 'Javaslatok (2)' })

    fireEvent.change(card('3 darab paprika').getByLabelText('Mennyiség'), { target: { value: '50' } })
    fireEvent.change(card('3 darab paprika').getByLabelText('Mértékegység'), { target: { value: 'dkg' } })
    fireEvent.click(screen.getByRole('button', { name: 'Jóváhagyom (2)' }))

    expect(await screen.findByText('Felvettem a kamrádba: tej 2 l, paprika 50 dkg.')).toBeTruthy()
    expect(calls.find((c) => c.key === 'POST /api/v1/item-proposals/ip-tej/approve')?.body)
      .toEqual({ ingredientId: 'i-tej', amount: 2, unit: 'l', expiryDate: '2026-11-13' })
    expect(calls.find((c) => c.key === 'POST /api/v1/item-proposals/ip-paprika/approve')?.body)
      .toEqual({ ingredientId: 'i-paprika', amount: 50, unit: 'dkg' })
    expect(screen.queryByRole('heading', { name: /Javaslatok/ })).toBeNull()
    expect(await screen.findByRole('listitem', { name: /paprika/ })).toBeTruthy()
  })

  it('keeps a card the backend refused, with its field error, and approves the rest', async () => {
    backend(true, {
      'POST /api/v1/item-proposals': [{ status: 201, body: { items: [milk, { ...pepper, unit: 'kg', unitMismatch: false, expiryText: null }] } }],
      'POST /api/v1/item-proposals/ip-tej/approve': [{ status: 201, body: pantryItem('tej', 2, 'l') }],
      'POST /api/v1/item-proposals/ip-paprika/approve': [problem(400, 'VALIDATION_FAILED', 'Néhány mező hibás.',
        { amount: ['A mennyiség 0,001 és 100 000 között legyen, legfeljebb 3 tizedesjeggyel.'] })],
    })
    render(<App />)
    await submit()
    await screen.findByRole('heading', { name: 'Javaslatok (2)' })

    fireEvent.click(screen.getByRole('button', { name: 'Jóváhagyom (2)' }))

    expect(await screen.findByText('Felvettem 1 tételt. Egynél javítás kell, lásd a kártyán.')).toBeTruthy()
    expect(screen.queryByRole('group', { name: '»2 liter tejet«' })).toBeNull()
    // The card takes its field errors in an effect: one render after the notice.
    expect(await card('3 darab paprika').findByText('A mennyiség 0,001 és 100 000 között legyen, legfeljebb 3 tizedesjeggyel.')).toBeTruthy()
    expect(screen.getByRole('button', { name: 'Jóváhagyom (1)' })).toBeTruthy()
  })

  it('does not send a card without an ingredient, an amount or a unit', async () => {
    const calls = backend(true, { 'POST /api/v1/item-proposals': [{ status: 201, body: { items: [pesto] } }] })
    render(<App />)
    await submit()
    await screen.findByRole('heading', { name: 'Javaslatok (1)' })

    fireEvent.click(screen.getByRole('button', { name: 'Jóváhagyom (1)' }))

    expect(await card('egy üveg pesztó').findByText('Válassz hozzávalót a listából.')).toBeTruthy()
    expect(calls.some((c) => c.key.endsWith('/approve'))).toBe(false)
  })

  it('moves the focus to the first card that needs a fix', async () => {
    const olives = { ...pesto, id: 'ip-oliva', text: 'egy doboz olívabogyó' }
    backend(true, { 'POST /api/v1/item-proposals': [{ status: 201, body: { items: [pesto, olives] } }] })
    render(<App />)
    await submit()
    await screen.findByRole('heading', { name: 'Javaslatok (2)' })

    fireEvent.click(screen.getByRole('button', { name: 'Jóváhagyom (2)' }))

    await card('egy doboz olívabogyó').findByText('Válassz hozzávalót a listából.')
    await waitFor(() => expect(document.activeElement).toBe(card('egy üveg pesztó').getByLabelText('Hozzávaló')))
  })

  it('shows the busy state while approving and holds back "Kihagyom"', async () => {
    let answer: (value: Response) => void = () => {}
    backend(true, { 'POST /api/v1/item-proposals': [{ status: 201, body: { items: [milk] } }] })
    const original = globalThis.fetch
    vi.stubGlobal('fetch', vi.fn((url: string, init: RequestInit = {}) =>
      `${init.method ?? 'GET'} ${url}` === 'POST /api/v1/item-proposals/ip-tej/approve'
        ? new Promise<Response>((resolve) => { answer = resolve })
        : original(url, init)))
    render(<App />)
    await submit()
    await screen.findByRole('heading', { name: 'Javaslatok (1)' })

    fireEvent.click(screen.getByRole('button', { name: 'Jóváhagyom (1)' }))

    const button = await screen.findByRole('button', { name: 'Feldolgozás…' })
    expect((button as HTMLButtonElement).disabled).toBe(true)
    expect((screen.getByRole('button', { name: 'Kihagyom: tej' }) as HTMLButtonElement).disabled).toBe(true)
    answer(new Response(JSON.stringify(pantryItem('tej', 2, 'l')), { status: 201 }))
    expect(await screen.findByText('Felvettem a kamrádba: tej 2 l.')).toBeTruthy()
  })

  it('keeps a card that failed for another reason, with the message on it', async () => {
    backend(true, {
      'POST /api/v1/item-proposals': [{ status: 201, body: { items: [milk] } }],
      'POST /api/v1/item-proposals/ip-tej/approve': [problem(500, 'INTERNAL_ERROR', 'Váratlan hiba történt.')],
    })
    render(<App />)
    await submit()
    await screen.findByRole('heading', { name: 'Javaslatok (1)' })

    fireEvent.click(screen.getByRole('button', { name: 'Jóváhagyom (1)' }))

    expect((await card('2 liter tejet').findByRole('alert')).textContent).toBe('Váratlan hiba történt.')
    expect(screen.getByRole('button', { name: 'Jóváhagyom (1)' })).toBeTruthy()
  })

  it('signs out when the session ends during the approval', async () => {
    backend(true, {
      'POST /api/v1/item-proposals': [{ status: 201, body: { items: [milk] } }],
      'POST /api/v1/item-proposals/ip-tej/approve': [problem(401, 'UNAUTHENTICATED', 'Jelentkezz be a folytatáshoz.')],
    })
    render(<App />)
    await submit()
    await screen.findByRole('heading', { name: 'Javaslatok (1)' })

    fireEvent.click(screen.getByRole('button', { name: 'Jóváhagyom (1)' }))

    expect(await screen.findByRole('heading', { name: 'Bejelentkezés' })).toBeTruthy()
    expect(screen.getByText('Biztonsági okból kiléptettünk. Jelentkezz be újra, és folytathatod.')).toBeTruthy()
  })

  it('skips a card with "Kihagyom"', async () => {
    const calls = backend(true, {
      'POST /api/v1/item-proposals': [{ status: 201, body: { items: [milk, pesto] } }],
      'POST /api/v1/item-proposals/ip-tej/reject': [{ status: 204 }],
    })
    render(<App />)
    await submit()
    await screen.findByRole('heading', { name: 'Javaslatok (2)' })

    fireEvent.click(screen.getByRole('button', { name: 'Kihagyom: tej' }))

    expect(await screen.findByText('Kihagytam: tej.')).toBeTruthy()
    expect(calls.some((c) => c.key === 'POST /api/v1/item-proposals/ip-tej/reject')).toBe(true)
    expect(screen.queryByRole('group', { name: '»2 liter tejet«' })).toBeNull()
    expect(screen.getByRole('heading', { name: 'Javaslatok (1)' })).toBeTruthy()
  })

  it('removes a skipped card that is gone already, and keeps one whose skip failed', async () => {
    backend(true, {
      'POST /api/v1/item-proposals': [{ status: 201, body: { items: [milk, pepper] } }],
      'POST /api/v1/item-proposals/ip-tej/reject': [problem(404, 'ITEM_PROPOSAL_NOT_FOUND', 'Nincs ilyen javaslat.')],
      'POST /api/v1/item-proposals/ip-paprika/reject': [problem(500, 'INTERNAL_ERROR', 'Váratlan hiba történt.')],
    })
    render(<App />)
    await submit()
    await screen.findByRole('heading', { name: 'Javaslatok (2)' })

    fireEvent.click(screen.getByRole('button', { name: 'Kihagyom: tej' }))
    expect(await screen.findByText('Ez a javaslat már nem érhető el. Levettem a listáról.')).toBeTruthy()
    expect(screen.queryByRole('group', { name: '»2 liter tejet«' })).toBeNull()

    fireEvent.click(screen.getByRole('button', { name: 'Kihagyom: paprika' }))
    expect((await card('3 darab paprika').findByRole('alert')).textContent).toBe('Váratlan hiba történt.')
    expect(screen.getByRole('heading', { name: 'Javaslatok (1)' })).toBeTruthy()
  })

  it('removes a card decided elsewhere and says so', async () => {
    backend(true, {
      'POST /api/v1/item-proposals': [{ status: 201, body: { items: [milk] } }],
      'POST /api/v1/item-proposals/ip-tej/approve': [problem(409, 'ITEM_PROPOSAL_STATE_CONFLICT', 'Ezt a javaslatot közben már kihagytad vagy jóváhagytad.')],
    })
    render(<App />)
    await submit()
    await screen.findByRole('heading', { name: 'Javaslatok (1)' })

    fireEvent.click(screen.getByRole('button', { name: 'Jóváhagyom (1)' }))

    expect(await screen.findByText('Ezt a javaslatot közben máshol már kihagytad vagy jóváhagytad. Levettem a listáról.')).toBeTruthy()
    expect(screen.queryByRole('heading', { name: /Javaslatok/ })).toBeNull()
  })
})

describe('errors of the sentence', () => {
  it.each([
    ['LLM_UNAVAILABLE', 503, 'Most nem tudom feldolgozni a mondatot, mert a szövegfeldolgozó nem válaszol. A szöveged megmaradt: próbáld újra pár perc múlva, vagy rögzítsd tételenként.'],
    ['LLM_INVALID_RESPONSE', 502, 'Nem sikerült értelmezni a mondatot. A szöveged megmaradt: próbáld újra, fogalmazd át, vagy rögzítsd tételenként.'],
    ['ITEM_PROPOSAL_NO_ITEMS', 400, 'Nem találtam élelmiszert a mondatban. Írd le, mit vettél és mennyit, például: »2 liter tej, 10 tojás«.'],
    ['RATE_LIMITED', 429, 'Most túl sok mondatot küldtél egymás után. Várj néhány percet, vagy rögzítsd tételenként.'],
    ['LLM_DISABLED', 503, 'A mondatos bevitel most ki van kapcsolva. Rögzítsd tételenként.'],
  ])('%s keeps the text and offers the manual form', async (code, status, message) => {
    backend(true, { 'POST /api/v1/item-proposals': [problem(status, code, 'backend title')] })
    render(<App />)

    await submit()

    expect((await panel().findByRole('alert')).textContent).toBe(message)
    expect((screen.getByLabelText('Mit vettél?') as HTMLTextAreaElement).value).toBe(sentence)
    expect(panel().getByRole('button', { name: 'Inkább tételenként rögzítem' })).toBeTruthy()
  })

  it('shows a field error of the sentence under the field', async () => {
    backend(true, { 'POST /api/v1/item-proposals': [problem(400, 'VALIDATION_FAILED', 'Néhány mező hibás.', { text: ['Legfeljebb 500 karaktert írj.'] })] })
    render(<App />)

    await submit('x'.repeat(501))

    expect(await panel().findByText('Legfeljebb 500 karaktert írj.')).toBeTruthy()
    await waitFor(() => expect(document.activeElement).toBe(screen.getByLabelText('Mit vettél?')))
  })
})

describe('formatOn', () => {
  it.each([
    ['2026-11-01', 'nov. 1-jén'],
    ['2026-11-02', 'nov. 2-án'],
    ['2026-11-03', 'nov. 3-án'],
    ['2026-11-10', 'nov. 10-én'],
    ['2026-11-12', 'nov. 12-én'],
    ['2026-11-20', 'nov. 20-án'],
    ['2026-11-21', 'nov. 21-én'],
    ['2026-11-22', 'nov. 22-én'],
    ['2026-11-30', 'nov. 30-án'],
    ['2026-12-31', 'dec. 31-én'],
  ])('%s is "%s"', (date, text) => expect(formatOn(date)).toBe(text))
})
