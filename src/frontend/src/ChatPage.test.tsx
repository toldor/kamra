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

function backendRoutes(enabled: boolean, chat: Answer[] = [answer('Szia!')]): Record<string, Answer[]> {
  return {
    'GET /api/v1/auth/me': [ok({ email: 'tomi@example.com', householdId: 'h-1' })],
    'GET /api/v1/auth/antiforgery': [ok({ requestToken: 'token-1' })],
    'GET /api/v1/chat/availability': [ok({ enabled })],
    'POST /api/v1/chat': chat,
  }
}

const backend = (enabled: boolean, chat?: Answer[]) => fakeBackend(backendRoutes(enabled, chat))

// The Készlet page, empty: where the user goes between two questions (l3_l5_minispec L5).
const emptyPantry: Record<string, Answer[]> = {
  'GET /api/v1/pantry-items': [ok([])],
  'GET /api/v1/categories': [ok([])],
  'GET /api/v1/ingredients': [ok([])],
  'GET /api/v1/item-proposals/availability': [ok({ enabled: false })],
}

async function goTo(hash: string, heading: string) {
  window.location.hash = hash
  await screen.findByRole('heading', { level: 1, name: heading })
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

// receptotlet_minispec 4. and Q7 (A): the idea under the answer, and the button that hands it to the Új recept form.
describe('a recipe idea in the chat', () => {
  const idea = {
    name: 'Tejes palacsinta',
    servings: 2,
    ingredients: [{ name: 'tej', amount: 0.5, unit: 'l' }, { name: 'finomliszt', amount: 20, unit: 'dkg' }, { name: 'tojás', amount: 2, unit: 'db' }],
    instructions: 'Keverd csomómentesre, és süsd ki.',
  }
  const withIdea = ok({ answer: 'Itt egy ötlet. Mentés előtt nézd át.', toolsUsed: ['get_pantry_items'], recipe: idea })
  const draftKey = 'kamra:recipe-draft'
  const findIdea = async () => within(await log().findByRole('group', { name: 'Receptötlet: Tejes palacsinta' }))
  // The Új recept form loads these when the button opens it.
  const formRoutes: Record<string, Answer[]> = {
    'GET /api/v1/categories': [ok([{ category: 'dairy', shelfLifeDays: 7 }])],
    'GET /api/v1/ingredients': [ok([{ id: 'i-tej', name: 'tej', dimension: 'volume', defaultCategory: 'dairy', minimum: null }])],
  }

  beforeEach(() => sessionStorage.clear())
  afterEach(() => vi.restoreAllMocks())

  it('shows the name, the servings and the ingredients under the answer, without the steps, with the button', async () => {
    backend(true, [withIdea])
    render(<App />)

    await ask('Adj egy palacsintareceptet.')

    expect(await log().findByText('Itt egy ötlet. Mentés előtt nézd át.')).toBeTruthy()
    const group = await findIdea()
    expect(group.getByText('Tejes palacsinta (2 adag)')).toBeTruthy()
    expect(group.getAllByRole('listitem').map((item) => item.textContent)).toEqual(['tej 0,5 l', 'finomliszt 20 dkg', 'tojás 2 db'])
    expect(group.queryByText(/csomómentesre/)).toBeNull()
    expect(group.getByRole('button', { name: 'Szerkesztés és mentés' })).toBeTruthy()
  })

  it('shows no idea and no button under a plain answer', async () => {
    backend(true, [answer('Szia!')])
    render(<App />)

    await ask('Szia')

    expect(await log().findByText('Szia!')).toBeTruthy()
    expect(screen.queryByRole('group', { name: /^Receptötlet/ })).toBeNull()
    expect(screen.queryByRole('button', { name: 'Szerkesztés és mentés' })).toBeNull()
  })

  it('sends only the text of an answer with an idea as an earlier message', async () => {
    const calls = backend(true, [withIdea, answer('Szívesen.')])
    render(<App />)
    await ask('Adj egy palacsintareceptet.')
    await findIdea()

    await ask('Köszönöm')

    await log().findByText('Szívesen.')
    expect(chatPosts(calls)[1]).toEqual({
      question: 'Köszönöm',
      history: [{ role: 'user', text: 'Adj egy palacsintareceptet.' }, { role: 'assistant', text: 'Itt egy ötlet. Mentés előtt nézd át.' }],
    })
  })

  it('opens the Új recept form filled with the idea, without asking when there is no draft', async () => {
    fakeBackend({ ...backendRoutes(true, [withIdea]), ...formRoutes })
    const confirm = vi.spyOn(window, 'confirm')
    render(<App />)
    await ask('Adj egy palacsintareceptet.')

    fireEvent.click((await findIdea()).getByRole('button', { name: 'Szerkesztés és mentés' }))

    expect(await screen.findByRole('heading', { level: 1, name: 'Új recept' })).toBeTruthy()
    expect(window.location.hash).toBe('#/receptek/uj')
    expect(await screen.findByText('Átvettem a chat receptötletét. Mentés előtt nézd át, és javítsd, amit kell.')).toBeTruthy()
    expect((screen.getByLabelText('Név') as HTMLInputElement).value).toBe('Tejes palacsinta')
    expect(confirm).not.toHaveBeenCalled()
  })

  it('asks before replacing a half-filled recipe, and keeps it when the user says no', async () => {
    backend(true, [withIdea])
    const draft = JSON.stringify({ name: 'Kakaó', servings: '2', instructions: '', rows: [] })
    sessionStorage.setItem(draftKey, draft)
    const confirm = vi.spyOn(window, 'confirm').mockReturnValue(false)
    render(<App />)
    await ask('Adj egy palacsintareceptet.')

    fireEvent.click((await findIdea()).getByRole('button', { name: 'Szerkesztés és mentés' }))

    expect(confirm).toHaveBeenCalledWith('A félbehagyott új recepted helyére a chat receptötlete kerül. Folytatod?')
    expect(sessionStorage.getItem(draftKey)).toBe(draft)
    expect(window.location.hash).toBe('#/kerdezz')
  })

  // Review U3: an earlier idea that was taken over is not "your half-filled recipe".
  it('asks with its own text before replacing an earlier recipe idea', async () => {
    backend(true, [withIdea])
    sessionStorage.setItem(draftKey, JSON.stringify({ name: 'Kakaó', servings: '2', instructions: '', rows: [], aiSuggested: true }))
    const confirm = vi.spyOn(window, 'confirm').mockReturnValue(false)
    render(<App />)
    await ask('Adj egy palacsintareceptet.')

    fireEvent.click((await findIdea()).getByRole('button', { name: 'Szerkesztés és mentés' }))

    expect(confirm).toHaveBeenCalledWith('A korábban átvett receptötlet helyére ez kerül. Folytatod?')
  })

  it('replaces the half-filled recipe when the user says yes', async () => {
    fakeBackend({ ...backendRoutes(true, [withIdea]), ...formRoutes })
    sessionStorage.setItem(draftKey, JSON.stringify({ name: 'Kakaó', servings: '2', instructions: '', rows: [] }))
    vi.spyOn(window, 'confirm').mockReturnValue(true)
    render(<App />)
    await ask('Adj egy palacsintareceptet.')

    fireEvent.click((await findIdea()).getByRole('button', { name: 'Szerkesztés és mentés' }))

    expect(await screen.findByRole('heading', { level: 1, name: 'Új recept' })).toBeTruthy()
    expect(((await screen.findByLabelText('Név')) as HTMLInputElement).value).toBe('Tejes palacsinta')
  })
})

describe('the Kérdezz menu item', () => {
  it('is in the menu when the switch is on', async () => {
    // On the Kérdezz page itself: the fake backend answers only the chat, so no other page waits for data.
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
  // ui_polish_minispec 22. (K3): four examples since 2026-11-17, the new one asks for a recipe idea.
  it('shows the title, the introduction, the note and four example questions with icons', async () => {
    backend(true)
    render(<App />)

    expect(await screen.findByRole('heading', { level: 1, name: 'Kérdezz a kamrádról' })).toBeTruthy()
    expect(screen.getByText('Kérdezd meg, mi van otthon, mi jár le hamarosan, mit főzhetsz, vagy mi van a bevásárlólistán. ' +
      'A válaszhoz csak a saját adataidat nézem meg, és semmit nem módosítok.')).toBeTruthy()
    expect(screen.getByText('A beszélgetést nem mentjük el: másik oldalra lépve megmarad, de az oldal újratöltésekor vagy ' +
      'kijelentkezéskor újrakezdődik.')).toBeTruthy()
    for (const example of ['Mi jár le hamarosan?', 'Mit főzhetek abból, ami otthon van?', 'Mi van a bevásárlólistán?',
      'Adj egy receptet abból, ami otthon van.']) {
      expect(screen.getByRole('button', { name: example }).querySelector('svg[aria-hidden="true"]'), example).toBeTruthy()
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

  // T2+T3 review R1 (ADR-0010 5.): an earlier message goes back with at most 2000 characters, so one long answer
  // cannot make every later question fail.
  it('sends a long earlier answer cut to 2000 characters', async () => {
    const calls = backend(true, [answer('a'.repeat(2500)), answer('Rendben.')])
    render(<App />)

    await ask('Mesélj!')
    await log().findByText('a'.repeat(2500))
    await ask('És még?')
    await log().findByText('Rendben.')

    const history = (chatPosts(calls)[1] as { history: { text: string }[] }).history
    expect(history[1].text).toHaveLength(2000)
  })

  // R3: what the user types while the answer is on its way is not lost.
  it('keeps what was typed while the answer was on its way', async () => {
    let release: (reply: Reply) => void = () => {}
    backend(true, [() => new Promise<Reply>((resolve) => { release = resolve })])
    render(<App />)

    await ask('Mi jár le?')
    await screen.findByRole('button', { name: 'Gondolkodom…' })
    fireEvent.change(screen.getByLabelText('Kérdésed'), { target: { value: 'És a tej?' } })
    release(answer('A tej.'))

    await log().findByText('A tej.')
    expect((screen.getByLabelText('Kérdésed') as HTMLTextAreaElement).value).toBe('És a tej?')
  })

  // R4: the waiting question stays the same element, so a screen reader does not read it out twice.
  it('keeps the waiting question as the same element when the answer comes', async () => {
    let release: (reply: Reply) => void = () => {}
    backend(true, [() => new Promise<Reply>((resolve) => { release = resolve })])
    render(<App />)

    await ask('Mi jár le?')
    const waiting = await log().findByText('Mi jár le?')
    release(answer('A tej.'))

    await log().findByText('A tej.')
    expect(log().getByText('Mi jár le?')).toBe(waiting)
  })

  // R5 (AGENTS.md: no technical text for the user): an unknown tool name is left out of the line.
  it('leaves an unknown tool name out of the line', async () => {
    backend(true, [answer('A tej.', ['get_expiring_items', 'get_something_new'])])
    render(<App />)

    await ask('Mi jár le?')

    expect(await log().findByText('Megnéztem: a hamarosan lejáró tételeket.')).toBeTruthy()
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

  // l3_l5_minispec L5: the conversation lives in the app's memory, not on the page.
  it('keeps the conversation and the typed text when going to another page and back', async () => {
    fakeBackend({ ...backendRoutes(true, [answer('A tej.')]), ...emptyPantry })
    render(<App />)
    await ask('Mi jár le?')
    await log().findByText('A tej.')
    fireEvent.change(screen.getByLabelText('Kérdésed'), { target: { value: 'És mit főzzek?' } })

    await goTo('#/', 'Készlet')
    await goTo('#/kerdezz', 'Kérdezz a kamrádról')

    expect(log().getByText('Mi jár le?')).toBeTruthy()
    expect(log().getByText('A tej.')).toBeTruthy()
    expect((screen.getByLabelText('Kérdésed') as HTMLTextAreaElement).value).toBe('És mit főzzek?')
  })

  it('shows the answer that came while the user was on another page', async () => {
    let release: (reply: Reply) => void = () => {}
    fakeBackend({ ...backendRoutes(true, [() => new Promise<Reply>((resolve) => { release = resolve })]), ...emptyPantry })
    render(<App />)
    await ask('Mi jár le?')
    await screen.findByRole('button', { name: 'Gondolkodom…' })

    await goTo('#/', 'Készlet')
    release(answer('A tej.'))
    await goTo('#/kerdezz', 'Kérdezz a kamrádról')

    expect(await log().findByText('A tej.')).toBeTruthy()
    expect(screen.getByRole('button', { name: 'Küldés' })).toBeTruthy()
  })

  it('starts a new conversation after signing out, and a late answer does not reach the next user', async () => {
    let release: (reply: Reply) => void = () => {}
    fakeBackend({
      ...backendRoutes(true, [answer('A tej.'), () => new Promise<Reply>((resolve) => { release = resolve })]),
      'POST /api/v1/auth/logout': [{ status: 204 }],
      'POST /api/v1/auth/login': [{ status: 204 }],
    })
    render(<App />)
    await ask('Mi jár le?')
    await log().findByText('A tej.')
    await ask('És mit főzzek?')
    await screen.findByRole('button', { name: 'Gondolkodom…' })

    // ui_polish_minispec Ü5: the logout is in the Profilom panel.
    fireEvent.click(screen.getByRole('button', { name: 'Profilom' }))
    fireEvent.click(screen.getByRole('button', { name: 'Kijelentkezés' }))
    await screen.findByRole('heading', { name: 'Bejelentkezés' })
    release(answer('Lecsót.'))
    fireEvent.change(screen.getByLabelText('E-mail-cím'), { target: { value: 'tomi@example.com' } })
    fireEvent.change(screen.getByLabelText('Jelszó'), { target: { value: 'correct horse battery staple' } })
    fireEvent.click(screen.getByRole('button', { name: 'Bejelentkezés' }))
    await screen.findByRole('heading', { level: 1, name: 'Kérdezz a kamrádról' })

    await waitFor(() => expect(screen.getByRole('button', { name: 'Küldés' })).toBeTruthy())
    expect(screen.queryByText('A tej.')).toBeNull()
    expect(screen.queryByText('Lecsót.')).toBeNull()
    expect(screen.queryByText('És mit főzzek?')).toBeNull()
  })

  it('sends a signed-out user to the sign-in page with the notice', async () => {
    backend(true, [problem(401, 'UNAUTHENTICATED', 'Jelentkezz be a folytatáshoz.')])
    render(<App />)

    await ask('Mi jár le?')

    expect(await screen.findByText('Biztonsági okból kiléptettünk. Jelentkezz be újra, és folytathatod.')).toBeTruthy()
  })
})

// ui_polish_minispec S6 (K1, K2, K4-K8): the Kérdezz page, livelier.
describe('the livelier Kérdezz page', () => {
  const idea = { name: 'Palacsinta', servings: 2, ingredients: [{ name: 'tej', amount: 0.5, unit: 'l' }], instructions: 'Süsd ki.' }

  afterEach(() => vi.restoreAllMocks())

  it('greets an empty conversation, and the greeting goes with the first message (K6)', async () => {
    backend(true, [answer('Szia!')])
    render(<App />)

    expect(await screen.findByText('Szia! Miben segíthetek a kamrával kapcsolatban?')).toBeTruthy()
    await ask('Szia')
    await log().findByText('Szia!')

    expect(screen.queryByText('Szia! Miben segíthetek a kamrával kapcsolatban?')).toBeNull()
  })

  it('shows the jar next to the answers only, and the dots while waiting (K1, K2)', async () => {
    let release!: (reply: Reply) => void
    backend(true, [() => new Promise<Reply>((resolve) => { release = resolve })])
    render(<App />)
    await ask('Mi jár le?')

    const typing = await waitFor(() => {
      const dots = document.querySelector('.chat-message.typing')
      expect(dots).toBeTruthy()
      return dots!
    })
    expect(typing.closest('[aria-hidden="true"]')).toBeTruthy()
    release(answer('A tej.'))
    await log().findByText('A tej.')

    expect(document.querySelector('.chat-message.typing')).toBeNull()
    const avatars = Array.from(document.querySelectorAll('.chat-avatar'))
    expect(avatars).toHaveLength(1)
    expect(avatars[0].getAttribute('aria-hidden')).toBe('true')
    expect(avatars[0].closest('.chat-row')?.querySelector('.chat-message.assistant')).toBeTruthy()
  })

  it('offers "Másik ötlet" after a recipe idea and sends its question (K4)', async () => {
    const calls = backend(true, [ok({ answer: 'Itt egy ötlet.', toolsUsed: ['get_pantry_items'], recipe: idea }), answer('Egy másik.')])
    render(<App />)
    await ask('Adj egy receptet.')

    fireEvent.click(await log().findByRole('button', { name: 'Másik ötlet' }))

    await log().findByText('Egy másik.')
    expect((chatPosts(calls)[1] as { question: string }).question).toBe('Adj egy másik receptötletet.')
  })

  it('offers "Mit főzzek ezekből?" after the soon-expiring items, only under the last answer (K4)', async () => {
    const calls = backend(true, [answer('A tej holnap.', ['get_expiring_items']), answer('Palacsintát.')])
    render(<App />)
    await ask('Mi jár le?')

    fireEvent.click(await log().findByRole('button', { name: 'Mit főzzek ezekből?' }))

    await log().findByText('Palacsintát.')
    expect((chatPosts(calls)[1] as { question: string }).question).toBe('Mit főzhetek a hamarosan lejáró tételekből?')
    expect(log().queryByRole('button', { name: 'Mit főzzek ezekből?' })).toBeNull()
  })

  it('offers no follow-up under a plain answer', async () => {
    backend(true, [answer('Szia!')])
    render(<App />)
    await ask('Szia')
    await log().findByText('Szia!')

    expect(log().queryByRole('button', { name: /Másik ötlet|Mit főzzek ezekből/ })).toBeNull()
  })

  it('puts an icon before each tool name, the sentence unchanged (K5)', async () => {
    backend(true, [answer('Rendben.', ['get_expiring_items', 'get_recommendations'])])
    render(<App />)
    await ask('Mi van?')

    const line = await log().findByText('Megnéztem: a hamarosan lejáró tételeket és az ajánlást.')
    expect(line.querySelectorAll('svg[aria-hidden="true"]')).toHaveLength(2)
  })

  it('counts the characters of the question (K7)', async () => {
    backend(true)
    render(<App />)
    const field = await screen.findByLabelText('Kérdésed')
    expect(screen.getByText('0 / 500')).toBeTruthy()

    fireEvent.change(field, { target: { value: 'Mi jár le hamarosan?' } })

    const counter = screen.getByText('20 / 500')
    expect(field.getAttribute('aria-describedby')?.split(' ')).toContain(counter.id)
  })

  it('copies an answer, says so, and has no copy button without a clipboard (K8)', async () => {
    const writeText = vi.fn(async () => {})
    vi.stubGlobal('navigator', { ...navigator, clipboard: { writeText } })
    backend(true, [answer('A tej holnap jár le.')])
    render(<App />)
    await ask('Mi jár le?')

    fireEvent.click(await log().findByRole('button', { name: 'Válasz másolása' }))

    expect(writeText).toHaveBeenCalledWith('A tej holnap jár le.')
    expect(await log().findByText('Kimásolva')).toBeTruthy()
    cleanup()

    vi.stubGlobal('navigator', { ...navigator, clipboard: undefined })
    backend(true, [answer('Szia!')])
    render(<App />)
    await ask('Szia')
    await log().findByText('Szia!')
    expect(log().queryByRole('button', { name: 'Válasz másolása' })).toBeNull()
  })
})
