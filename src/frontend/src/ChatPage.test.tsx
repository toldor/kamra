import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import App from './App'
import { resetClientForTests } from './api/client'

type Reply = { status: number; body?: unknown }
type Answer = Reply | (() => Promise<Reply>)

// US-6 "Kérdezz" (us6_minispec K1: A, K2-K3, K11-K12 and its texts): the menu item only when the switch is on, the
// example questions, sending with the earlier messages, the "Megnéztem" line, the chat's own error texts, plain
// text only (ADR-0006) and a new conversation. A fake backend as in QuickEntry.test.tsx: each route answers from a
// queue (the last reply repeats; a function answers later), every request is recorded.
function fakeBackend(routes: Record<string, Answer[]>) {
  const calls: { key: string; body: unknown }[] = []
  vi.stubGlobal('fetch', vi.fn(async (url: string, init: RequestInit = {}) => {
    const key = `${init.method ?? 'GET'} ${url}`
    calls.push({ key, body: init.body ? JSON.parse(init.body as string) : undefined })
    const queue = routes[key]
    if (!queue) throw new Error(`Unexpected request: ${key}`)
    const next = queue.length > 1 ? queue.shift()! : queue[0]
    const reply = typeof next === 'function' ? await next() : next
    return new Response(reply.body === undefined ? null : JSON.stringify(reply.body), { status: reply.status })
  }))
  return calls
}

const ok = (body: unknown): Reply => ({ status: 200, body })
const problem = (status: number, code: string, title: string): Reply => ({ status, body: { code, title } })
const answer = (text: string, toolsUsed: string[] = []) => ok({ answer: text, toolsUsed })

function backend(enabled: boolean, chat: Answer[] = [answer('Szia!')]) {
  return fakeBackend({
    'GET /api/v1/auth/me': [ok({ email: 'tomi@example.com', householdId: 'h-1' })],
    'GET /api/v1/auth/antiforgery': [ok({ requestToken: 'token-1' })],
    'GET /api/v1/chat/availability': [ok({ enabled })],
    'POST /api/v1/chat': chat,
  })
}

const chatPosts = (calls: { key: string; body: unknown }[]) => calls.filter((c) => c.key === 'POST /api/v1/chat').map((c) => c.body)

async function ask(text: string) {
  fireEvent.change(await screen.findByLabelText('Kérdésed'), { target: { value: text } })
  fireEvent.click(screen.getByRole('button', { name: 'Küldés' }))
}

const log = () => within(screen.getByRole('log'))

beforeEach(() => {
  resetClientForTests()
  window.location.hash = '#/kerdezz'
})
afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
  window.location.hash = ''
})

describe('the Kérdezz menu item', () => {
  it('is in the menu when the switch is on', async () => {
    window.location.hash = ''
    backend(true)
    render(<App />)

    const link = await screen.findByRole('link', { name: 'Kérdezz' })
    expect(link.getAttribute('href')).toBe('#/kerdezz')
  })

  it('is not in the menu when the switch is off', async () => {
    const calls = backend(false)
    render(<App />)

    await screen.findByRole('link', { name: 'Készlet' })
    await waitFor(() => expect(calls.some((c) => c.key === 'GET /api/v1/chat/availability')).toBe(true))
    expect(screen.queryByRole('link', { name: 'Kérdezz' })).toBeNull()
  })

  it('is not in the menu when its availability cannot be read', async () => {
    fakeBackend({ 'GET /api/v1/auth/me': [ok({ email: 'tomi@example.com', householdId: 'h-1' })] })
    render(<App />)

    await screen.findByRole('link', { name: 'Készlet' })
    expect(screen.queryByRole('link', { name: 'Kérdezz' })).toBeNull()
  })
})

describe('the Kérdezz page', () => {
  it('shows the title, the introduction, the note and three example questions', async () => {
    backend(true)
    render(<App />)

    expect(await screen.findByRole('heading', { level: 1, name: 'Kérdezz a kamrádról' })).toBeTruthy()
    expect(screen.getByText('Kérdezd meg, mi van otthon, mi jár le hamarosan, mit főzhetsz, vagy mi van a bevásárlólistán. ' +
      'A válaszhoz csak a saját adataidat nézem meg, és semmit nem módosítok.')).toBeTruthy()
    expect(screen.getByText('A beszélgetést nem mentjük: ha elhagyod az oldalt, újrakezdődik.')).toBeTruthy()
    for (const example of ['Mi jár le hamarosan?', 'Mit főzhetek abból, ami otthon van?', 'Mi van a bevásárlólistán?']) {
      expect(screen.getByRole('button', { name: example })).toBeTruthy()
    }
  })

  it('sends an example question and shows the answer with the tools it looked at', async () => {
    const calls = backend(true, [answer('A tej holnap lejár.', ['get_expiring_items', 'get_recommendations'])])
    render(<App />)

    fireEvent.click(await screen.findByRole('button', { name: 'Mi jár le hamarosan?' }))

    expect(await log().findByText('A tej holnap lejár.')).toBeTruthy()
    expect(log().getByText('Mi jár le hamarosan?')).toBeTruthy()
    expect(log().getByText('Megnéztem: a hamarosan lejáró tételeket és az ajánlást.')).toBeTruthy()
    expect(chatPosts(calls)).toEqual([{ question: 'Mi jár le hamarosan?', history: [] }])
    expect(screen.queryByRole('button', { name: 'Mi van a bevásárlólistán?' })).toBeNull()
  })

  it('names every tool in Hungarian and leaves the line out when no tool was used', async () => {
    backend(true, [answer('Minden megvan.', ['get_pantry_items', 'get_expiring_items', 'get_recommendations', 'get_shopping_list']),
      answer('Szia!')])
    render(<App />)

    await ask('Mi a helyzet?')
    expect(await log().findByText(
      'Megnéztem: a készletet, a hamarosan lejáró tételeket, az ajánlást és a bevásárlólistát.')).toBeTruthy()
    await ask('Szia')
    await log().findByText('Szia!')
    expect(log().getAllByText(/^Megnéztem:/)).toHaveLength(1)
  })

  it('labels the two sides for a screen reader', async () => {
    backend(true, [answer('Szia!')])
    render(<App />)

    await ask('Szia')

    await log().findByText('Szia!')
    expect(log().getByText('Te:')).toBeTruthy()
    expect(log().getByText('Kamra:')).toBeTruthy()
  })

  it('sends the earlier questions and answers with the next question', async () => {
    const calls = backend(true, [answer('A tej holnap lejár.'), answer('Igen, 200 g.')])
    render(<App />)

    await ask('Mi jár le?')
    await log().findByText('A tej holnap lejár.')
    await ask('Van tejföl?')
    await log().findByText('Igen, 200 g.')

    expect(chatPosts(calls)[1]).toEqual({
      question: 'Van tejföl?',
      history: [{ role: 'user', text: 'Mi jár le?' }, { role: 'assistant', text: 'A tej holnap lejár.' }],
    })
  })

  it('sends at most the last 10 messages', async () => {
    const calls = backend(true, [answer('Válasz.')])
    render(<App />)

    for (let i = 1; i <= 7; i++) {
      await ask(`Kérdés ${i}`)
      await waitFor(() => expect(log().getAllByText('Válasz.')).toHaveLength(i))
    }

    // Before the 7th question there are 12 messages: the first question and its answer are left out.
    const last = chatPosts(calls)[6] as { history: { text: string }[] }
    expect(last.history).toHaveLength(10)
    expect(last.history[0].text).toBe('Kérdés 2')
  })

  it('shows the question at once and "Gondolkodom…" while it waits', async () => {
    let release: (reply: Reply) => void = () => {}
    backend(true, [() => new Promise<Reply>((resolve) => { release = resolve })])
    render(<App />)

    await ask('Mi jár le?')

    const waiting = await screen.findByRole('button', { name: 'Gondolkodom…' })
    expect((waiting as HTMLButtonElement).disabled).toBe(true)
    expect(log().getByText('Mi jár le?')).toBeTruthy()
    release(answer('A tej.'))
    expect(await log().findByText('A tej.')).toBeTruthy()
    expect(screen.getByRole('button', { name: 'Küldés' })).toBeTruthy()
  })

  it('sends on Enter and adds a new line on Shift+Enter', async () => {
    const calls = backend(true, [answer('Rendben.')])
    render(<App />)
    const field = await screen.findByLabelText('Kérdésed')
    fireEvent.change(field, { target: { value: 'Mi jár le?' } })

    fireEvent.keyDown(field, { key: 'Enter', shiftKey: true })
    // Sending starts at once ("Gondolkodom…"); Shift+Enter must not start it.
    expect(screen.getByRole('button', { name: 'Küldés' })).toBeTruthy()
    expect(chatPosts(calls)).toHaveLength(0)
    fireEvent.keyDown(field, { key: 'Enter' })

    await log().findByText('Rendben.')
    expect(chatPosts(calls)).toHaveLength(1)
    expect((field as HTMLTextAreaElement).value).toBe('')
  })

  it('shows the answer as plain text, never as markup (ADR-0006)', async () => {
    backend(true, [answer('<b>vastag</b> <img src=x onerror="alert(1)">')])
    const { container } = render(<App />)

    await ask('Mi van?')

    expect(await log().findByText('<b>vastag</b> <img src=x onerror="alert(1)">')).toBeTruthy()
    expect(container.querySelector('b, img')).toBeNull()
  })

  it('asks for a question instead of sending an empty one, and allows at most 500 characters', async () => {
    const calls = backend(true)
    render(<App />)

    await ask('   ')

    expect(await screen.findByText('Írd le a kérdésed.')).toBeTruthy()
    expect(chatPosts(calls)).toHaveLength(0)
    expect(screen.getByLabelText('Kérdésed').getAttribute('maxLength')).toBe('500')
  })

  it.each([
    ['LLM_UNAVAILABLE', 503, 'Most nem tudok válaszolni, mert a szövegfeldolgozó nem válaszol. A kérdésed megmaradt: próbáld újra pár perc múlva.'],
    ['LLM_INVALID_RESPONSE', 502, 'Erre most nem sikerült válaszolnom. Kérdezd meg másképp, vagy nézd meg közvetlenül a Készlet, ' +
      'a Mit főzzek? vagy a Bevásárlólista oldalon.'],
    ['RATE_LIMITED', 429, 'Most túl sok kérdést küldtél egymás után. Várj néhány percet.'],
    ['LLM_DISABLED', 503, 'A kérdezés most ki van kapcsolva.'],
  ])('%s: the chat\'s own text, the question stays in the field and out of the history', async (code, status, text) => {
    const calls = backend(true, [problem(status, code, 'backend title'), answer('Most már megy.')])
    render(<App />)

    await ask('Mi jár le?')

    expect(await screen.findByText(text)).toBeTruthy()
    expect((screen.getByLabelText('Kérdésed') as HTMLTextAreaElement).value).toBe('Mi jár le?')
    fireEvent.click(screen.getByRole('button', { name: 'Küldés' }))
    await log().findByText('Most már megy.')
    expect(chatPosts(calls)[1]).toEqual({ question: 'Mi jár le?', history: [] })
    expect(screen.queryByText(text)).toBeNull()
  })

  it('starts a new conversation', async () => {
    const calls = backend(true, [answer('A tej.'), answer('Szia!')])
    render(<App />)
    await ask('Mi jár le?')
    await log().findByText('A tej.')

    fireEvent.click(screen.getByRole('button', { name: 'Új beszélgetés' }))

    expect(screen.queryByText('A tej.')).toBeNull()
    expect(screen.getByRole('button', { name: 'Mi jár le hamarosan?' })).toBeTruthy()
    await ask('Szia')
    await log().findByText('Szia!')
    expect(chatPosts(calls)[1]).toEqual({ question: 'Szia', history: [] })
  })

  it('sends a signed-out user to the sign-in page with the notice', async () => {
    backend(true, [problem(401, 'UNAUTHENTICATED', 'Jelentkezz be a folytatáshoz.')])
    render(<App />)

    await ask('Mi jár le?')

    expect(await screen.findByText('Biztonsági okból kiléptettünk. Jelentkezz be újra, és folytathatod.')).toBeTruthy()
  })
})
