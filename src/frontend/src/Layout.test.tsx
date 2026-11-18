import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import App from './App'
import { resetClientForTests } from './api/client'
import { notFoundRoute, pageTitle, routes, type Route } from './useHashRoute'

type Reply = { status: number; body?: unknown }

// ui_polish_minispec S1 (1.-7.): the Profilom panel and page, the menu icons, the tab titles and the unknown address.
// A fake backend as in ChatPage.test.tsx: each route answers from a queue (the last reply repeats).
function fakeBackend(routes: Record<string, Reply[]>) {
  const calls: string[] = []
  vi.stubGlobal('fetch', vi.fn(async (url: string, init: RequestInit = {}) => {
    const key = `${init.method ?? 'GET'} ${url}`
    calls.push(key)
    const queue = routes[key]
    if (!queue) throw new Error(`Unexpected request: ${key}`)
    const reply = queue.length > 1 ? queue.shift()! : queue[0]
    return new Response(reply.body === undefined ? null : JSON.stringify(reply.body), { status: reply.status })
  }))
  return calls
}

const ok = (body: unknown): Reply => ({ status: 200, body })
const signedIn = ok({ email: 'tomi@example.com', householdId: 'h-1' })

function backend(extra: Record<string, Reply[]> = {}) {
  return fakeBackend({
    'GET /api/v1/auth/me': [signedIn],
    'GET /api/v1/auth/antiforgery': [ok({ requestToken: 'token-1' })],
    'GET /api/v1/chat/availability': [ok({ enabled: true })],
    'POST /api/v1/auth/logout': [{ status: 204 }],
    ...extra,
  })
}

// The Készlet page, empty.
const emptyPantry: Record<string, Reply[]> = {
  'GET /api/v1/pantry-items': [ok([])],
  'GET /api/v1/categories': [ok([])],
  'GET /api/v1/ingredients': [ok([])],
  'GET /api/v1/item-proposals/availability': [ok({ enabled: false })],
}

const profileButton = () => screen.findByRole('button', { name: 'Profilom' })
const panel = () => within(screen.getByRole('region', { name: 'Profilom' }))

beforeEach(() => {
  resetClientForTests()
  // The Kérdezz page needs nothing but the switch, so the frame is tested there.
  window.location.hash = '#/kerdezz'
})
afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
  window.location.hash = ''
  document.title = ''
})

describe('the Profilom panel (ui_polish_minispec 1.)', () => {
  it('opens from the band with the e-mail, the Profilom link and the logout; closed it shows none of them', async () => {
    backend()
    render(<App />)

    const button = await profileButton()
    expect(button.getAttribute('aria-expanded')).toBe('false')
    expect(screen.queryByText('Bejelentkezve: tomi@example.com')).toBeNull()
    expect(screen.queryByRole('button', { name: 'Kijelentkezés' })).toBeNull()

    fireEvent.click(button)

    expect(button.getAttribute('aria-expanded')).toBe('true')
    expect(button.getAttribute('aria-controls')).toBe(screen.getByRole('region', { name: 'Profilom' }).id)
    expect(panel().getByText('Bejelentkezve: tomi@example.com')).toBeTruthy()
    expect(panel().getByRole('link', { name: 'Profilom' }).getAttribute('href')).toBe('#/profil')
    expect(panel().getByRole('button', { name: 'Kijelentkezés' })).toBeTruthy()
  })

  it('closes on Escape and gives the focus back to its button', async () => {
    backend()
    render(<App />)
    const button = await profileButton()
    fireEvent.click(button)
    panel().getByRole('link', { name: 'Profilom' }).focus()

    fireEvent.keyDown(document.activeElement!, { key: 'Escape' })

    expect(button.getAttribute('aria-expanded')).toBe('false')
    expect(screen.queryByRole('region', { name: 'Profilom' })).toBeNull()
    expect(document.activeElement).toBe(button)
  })

  it('closes on a click outside, but not on a click inside', async () => {
    backend()
    render(<App />)
    const button = await profileButton()
    fireEvent.click(button)

    fireEvent.mouseDown(panel().getByText('Bejelentkezve: tomi@example.com'))
    expect(button.getAttribute('aria-expanded')).toBe('true')

    fireEvent.mouseDown(screen.getByRole('heading', { level: 1 }))
    expect(button.getAttribute('aria-expanded')).toBe('false')
  })

  it('closes when another page opens', async () => {
    backend()
    render(<App />)
    const button = await profileButton()
    fireEvent.click(button)

    fireEvent.click(panel().getByRole('link', { name: 'Profilom' }))
    window.location.hash = '#/profil'

    expect(await screen.findByRole('heading', { level: 1, name: 'Profilom' })).toBeTruthy()
    expect(button.getAttribute('aria-expanded')).toBe('false')
  })

  // Mutation H3: a page opened from the keyboard or the browser's Back button has no click to close the panel.
  it('closes when the page changes without a click, as with the Back button', async () => {
    backend()
    render(<App />)
    const button = await profileButton()
    fireEvent.click(button)

    window.location.hash = '#/profil'

    expect(await screen.findByRole('heading', { level: 1, name: 'Profilom' })).toBeTruthy()
    expect(button.getAttribute('aria-expanded')).toBe('false')
  })

  // Review R1: Tab out of the panel closes it, so it cannot cover the focused control (WCAG 2.4.11); focus that only
  // leaves for the page itself (a click on plain text) does not.
  it('closes when the focus moves to a control outside it', async () => {
    backend()
    render(<App />)
    const button = await profileButton()
    fireEvent.click(button)
    const link = panel().getByRole('link', { name: 'Profilom' })

    fireEvent.focusOut(link, { relatedTarget: null })
    expect(button.getAttribute('aria-expanded')).toBe('true')

    fireEvent.focusOut(link, { relatedTarget: screen.getByRole('heading', { level: 1 }) })
    expect(button.getAttribute('aria-expanded')).toBe('false')
  })

  // Review R1: a browser that does not focus a clicked button (Safari) leaves the focus on the page; Escape still closes.
  it('closes on Escape when the focus is not in it', async () => {
    backend()
    render(<App />)
    const button = await profileButton()
    fireEvent.click(button)
    button.blur()

    fireEvent.keyDown(document.body, { key: 'Escape' })

    expect(button.getAttribute('aria-expanded')).toBe('false')
    expect(document.activeElement).toBe(button)
  })

  // Review R3: an old error does not come back with the panel.
  it('shows a failed logout, and not again after the panel is reopened', async () => {
    backend({ 'POST /api/v1/auth/logout': [{ status: 500, body: { code: 'INTERNAL_ERROR', title: 'Váratlan hiba történt. Próbáld újra később.' } }] })
    render(<App />)
    const button = await profileButton()
    fireEvent.click(button)
    fireEvent.click(panel().getByRole('button', { name: 'Kijelentkezés' }))
    expect(await panel().findByText('Váratlan hiba történt. Próbáld újra később.')).toBeTruthy()

    fireEvent.click(button)
    fireEvent.click(button)

    expect(panel().queryByText('Váratlan hiba történt. Próbáld újra később.')).toBeNull()
  })

  it('signs out from the panel', async () => {
    const calls = backend()
    render(<App />)
    fireEvent.click(await profileButton())

    fireEvent.click(panel().getByRole('button', { name: 'Kijelentkezés' }))

    expect(await screen.findByRole('heading', { name: 'Bejelentkezés' })).toBeTruthy()
    expect(calls).toContain('POST /api/v1/auth/logout')
  })
})

describe('the Profilom page (ui_polish_minispec 2.)', () => {
  beforeEach(() => {
    window.location.hash = '#/profil'
  })

  it('shows the e-mail and the note, and signs out', async () => {
    const calls = backend()
    render(<App />)

    expect(await screen.findByRole('heading', { level: 1, name: 'Profilom' })).toBeTruthy()
    expect(screen.getByText('E-mail-cím: tomi@example.com')).toBeTruthy()
    expect(screen.getByText('A kijelentkezés minden eszközödön kiléptet.')).toBeTruthy()

    fireEvent.click(screen.getByRole('button', { name: 'Kijelentkezés' }))

    expect(await screen.findByRole('heading', { name: 'Bejelentkezés' })).toBeTruthy()
    expect(calls).toContain('POST /api/v1/auth/logout')
  })
})

describe('the menu (ui_polish_minispec 3.-4.)', () => {
  it('keeps the names of the links, with a hidden icon in each', async () => {
    backend()
    render(<App />)
    await profileButton()

    for (const name of ['Készlet', 'Mit főzzek?', 'Receptek', 'Bevásárlólista', 'Kérdezz']) {
      const link = screen.getByRole('link', { name })
      expect(link.querySelector('svg')?.getAttribute('aria-hidden'), name).toBe('true')
    }
    expect((await profileButton()).querySelector('svg')?.getAttribute('aria-hidden')).toBe('true')
  })
})

describe('the tab title (ui_polish_minispec 6.)', () => {
  it('names every page', () => {
    const all: Route[] = [routes.pantry, routes.recommendations, routes.recipes, routes.newRecipe, routes.shopping, routes.chat, routes.profile,
      '#/fozes/r-1', notFoundRoute]
    expect(all.map(pageTitle)).toEqual([
      'Készlet – Kamra', 'Mit főzzek? – Kamra', 'Receptek – Kamra', 'Új recept – Kamra', 'Bevásárlólista – Kamra', 'Kérdezz – Kamra',
      'Profilom – Kamra', 'Főzés – Kamra', 'Nincs ilyen oldal – Kamra',
    ])
  })

  it('follows the page, and names the sign-in screen after signing out', async () => {
    backend(emptyPantry)
    render(<App />)
    await profileButton()
    await waitFor(() => expect(document.title).toBe('Kérdezz – Kamra'))

    window.location.hash = '#/'
    await screen.findByRole('heading', { level: 1, name: 'Készlet' })
    await waitFor(() => expect(document.title).toBe('Készlet – Kamra'))

    fireEvent.click(await profileButton())
    fireEvent.click(panel().getByRole('button', { name: 'Kijelentkezés' }))
    await screen.findByRole('heading', { name: 'Bejelentkezés' })
    await waitFor(() => expect(document.title).toBe('Bejelentkezés – Kamra'))
  })
})

describe('an unknown address (ui_polish_minispec 7.)', () => {
  it('says there is no such page and links to the pantry; no menu item is current', async () => {
    window.location.hash = '#/ismeretlen'
    backend()
    render(<App />)

    expect(await screen.findByRole('heading', { level: 1, name: 'Nincs ilyen oldal' })).toBeTruthy()
    expect(screen.getByText('A megnyitott cím nem létezik a Kamrában.')).toBeTruthy()
    expect(screen.getByRole('link', { name: 'Ugrás a Készlethez' }).getAttribute('href')).toBe('#/')
    expect(screen.getByRole('navigation', { name: 'Oldalak' }).querySelector('[aria-current]')).toBeNull()
  })

  it('shows the pantry for an empty address', async () => {
    window.location.hash = ''
    backend(emptyPantry)
    render(<App />)

    expect(await screen.findByRole('heading', { level: 1, name: 'Készlet' })).toBeTruthy()
  })
})
