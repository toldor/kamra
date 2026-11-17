import { cleanup, fireEvent, render, screen, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import App from './App'
import { resetClientForTests } from './api/client'
import { chooseTheme, readTheme, startTheme } from './theme'

type Reply = { status: number; body?: unknown }

// ui_polish_minispec S3 (13.): the theme choice in the Profilom panel and on the Profilom page, kept in this browser.
function fakeBackend(routes: Record<string, Reply[]>) {
  vi.stubGlobal('fetch', vi.fn(async (url: string, init: RequestInit = {}) => {
    const queue = routes[`${init.method ?? 'GET'} ${url}`]
    if (!queue) throw new Error(`Unexpected request: ${init.method ?? 'GET'} ${url}`)
    const reply = queue[0]
    return new Response(reply.body === undefined ? null : JSON.stringify(reply.body), { status: reply.status })
  }))
}

const ok = (body: unknown): Reply => ({ status: 200, body })
const backend = () => fakeBackend({
  'GET /api/v1/auth/me': [ok({ email: 'tomi@example.com', householdId: 'h-1' })],
  'GET /api/v1/chat/availability': [ok({ enabled: false })],
})

const theme = () => document.documentElement.dataset.theme
const choices = (where: ReturnType<typeof within>) => within(where.getByRole('group', { name: 'Téma' }))

// The system's choice, as the browser reports it.
function systemPrefers(dark: boolean) {
  vi.stubGlobal('matchMedia', vi.fn((query: string) => ({
    matches: dark && query === '(prefers-color-scheme: dark)', media: query, addEventListener: vi.fn(), removeEventListener: vi.fn(),
  })))
}

beforeEach(() => {
  resetClientForTests()
  localStorage.clear()
  delete document.documentElement.dataset.theme
  window.location.hash = '#/profil'
})
afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
  localStorage.clear()
  window.location.hash = ''
})

describe('the theme choice', () => {
  it('is on the Profilom page and in the panel, "Rendszer szerint" by default, with the note on the page', async () => {
    backend()
    render(<App />)
    // The app's own loading screen is a <main> too, so the page heading is awaited first.
    await screen.findByRole('heading', { level: 1, name: 'Profilom' })
    const page = within(screen.getByRole('main'))

    expect((choices(page).getByRole('radio', { name: 'Rendszer szerint' }) as HTMLInputElement).checked).toBe(true)
    expect(choices(page).getAllByRole('radio').map((r) => r.getAttribute('aria-label') ?? r.closest('label')?.textContent))
      .toEqual(['Rendszer szerint', 'Világos', 'Sötét'])
    expect(page.getByText('A téma ebben a böngészőben marad meg.')).toBeTruthy()
    // Review T3: the radios look like the other radio groups (the .check rule), not like text fields.
    expect(choices(page).getAllByRole('radio').every((r) => r.closest('label')?.classList.contains('check'))).toBe(true)

    fireEvent.click(screen.getByRole('button', { name: 'Profilom' }))
    const panel = within(screen.getByRole('region', { name: 'Profilom' }))
    expect((choices(panel).getByRole('radio', { name: 'Rendszer szerint' }) as HTMLInputElement).checked).toBe(true)
  })

  it('switches the page to dark and remembers it; the panel and the page show the same choice', async () => {
    backend()
    render(<App />)
    // The app's own loading screen is a <main> too, so the page heading is awaited first.
    await screen.findByRole('heading', { level: 1, name: 'Profilom' })
    const page = within(screen.getByRole('main'))
    fireEvent.click(screen.getByRole('button', { name: 'Profilom' }))
    const panel = within(screen.getByRole('region', { name: 'Profilom' }))

    fireEvent.click(choices(panel).getByRole('radio', { name: 'Sötét' }))

    expect(theme()).toBe('dark')
    expect(localStorage.getItem('kamra:theme')).toBe('dark')
    expect((choices(page).getByRole('radio', { name: 'Sötét' }) as HTMLInputElement).checked).toBe(true)
    // The panel stays open after a choice.
    expect(screen.getByRole('button', { name: 'Profilom' }).getAttribute('aria-expanded')).toBe('true')

    fireEvent.click(choices(page).getByRole('radio', { name: 'Világos' }))
    expect(theme()).toBe('light')
    expect(localStorage.getItem('kamra:theme')).toBe('light')

    fireEvent.click(choices(page).getByRole('radio', { name: 'Rendszer szerint' }))
    expect(localStorage.getItem('kamra:theme')).toBeNull()
  })
})

describe('the theme while the app is open (review T6)', () => {
  // A system whose setting can change, with the listener the app registered.
  function changingSystem() {
    const system = { matches: false, listeners: [] as (() => void)[] }
    vi.stubGlobal('matchMedia', vi.fn(() => ({
      get matches() { return system.matches },
      addEventListener: (_type: string, listener: () => void) => system.listeners.push(listener),
      removeEventListener: vi.fn(),
    })))
    return { switchTo(dark: boolean) { system.matches = dark; system.listeners.forEach((l) => l()) } }
  }

  it('follows the system when it changes, while "Rendszer szerint" is chosen', () => {
    const system = changingSystem()
    startTheme()
    expect(theme()).toBe('light')

    system.switchTo(true)

    expect(theme()).toBe('dark')
  })

  it('keeps a chosen theme when the system changes', () => {
    const system = changingSystem()
    localStorage.setItem('kamra:theme', 'light')
    startTheme()

    system.switchTo(true)

    expect(theme()).toBe('light')
  })
})

describe('the theme on load', () => {
  it('follows the system when nothing was chosen', () => {
    systemPrefers(true)
    startTheme()
    expect(theme()).toBe('dark')

    systemPrefers(false)
    startTheme()
    expect(theme()).toBe('light')
  })

  it('brings back a stored choice over the system', () => {
    systemPrefers(true)
    localStorage.setItem('kamra:theme', 'light')

    startTheme()

    expect(readTheme()).toBe('light')
    expect(theme()).toBe('light')
  })

  it('ignores an unknown stored value', () => {
    localStorage.setItem('kamra:theme', 'pink')
    expect(readTheme()).toBe('system')
  })

  it('works without storage (a private window or blocked site data)', () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => { throw new Error('blocked') })
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => { throw new Error('blocked') })

    expect(readTheme()).toBe('system')
    expect(() => chooseTheme('dark')).not.toThrow()
    expect(theme()).toBe('dark')
    // Mutation L5: the choice still shows until the page is reloaded.
    expect(readTheme()).toBe('dark')
    chooseTheme('system')
  })
})
