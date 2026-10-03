import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import App from './App'
import { api, ApiError, resetClientForTests } from './api/client'

type Reply = { status: number; body?: unknown }

// A fake backend: each route answers from a queue (the last reply repeats), and every request is recorded.
function fakeBackend(routes: Record<string, Reply[]>) {
  const calls: { url: string; init: RequestInit }[] = []
  const fetchMock = vi.fn(async (url: string, init: RequestInit = {}) => {
    calls.push({ url, init })
    const key = `${init.method ?? 'GET'} ${url}`
    const queue = routes[key]
    if (!queue) throw new Error(`Unexpected request: ${key}`)
    const reply = queue.length > 1 ? queue.shift()! : queue[0]
    return new Response(reply.body === undefined ? null : JSON.stringify(reply.body), { status: reply.status })
  })
  vi.stubGlobal('fetch', fetchMock)
  return calls
}

const notSignedIn: Reply = { status: 401, body: { code: 'UNAUTHENTICATED', title: 'Jelentkezz be a folytatáshoz.' } }
const token: Reply = { status: 200, body: { requestToken: 'token-1' } }
const lockedOut = 'Túl sok sikertelen próbálkozás. Várj 5 percet, és próbáld újra.'

beforeEach(() => resetClientForTests())
afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
})

describe('api client', () => {
  it('sends the antiforgery token header and turns ProblemDetails into an ApiError', async () => {
    const calls = fakeBackend({
      'GET /api/v1/auth/antiforgery': [token],
      'POST /api/v1/auth/login': [{ status: 429, body: { code: 'LOGIN_LOCKED_OUT', title: lockedOut } }],
    })

    const error = await api.login({ email: 'tomi@example.com', password: 'x' }).catch((e: unknown) => e)

    expect(error).toBeInstanceOf(ApiError)
    expect(error).toMatchObject({ status: 429, code: 'LOGIN_LOCKED_OUT', message: lockedOut })
    const login = calls.find((c) => c.init.method === 'POST')!
    expect((login.init.headers as Record<string, string>)['X-XSRF-TOKEN']).toBe('token-1')
  })

  it('retries once with a fresh antiforgery token when the cached one is stale', async () => {
    const calls = fakeBackend({
      'GET /api/v1/auth/antiforgery': [token, { status: 200, body: { requestToken: 'token-2' } }],
      'POST /api/v1/auth/login': [
        { status: 400, body: { code: 'ANTIFORGERY_TOKEN_INVALID', title: 'A kérés biztonsági ellenőrzése nem sikerült.' } },
        { status: 204 },
      ],
    })

    await api.login({ email: 'tomi@example.com', password: 'correct horse battery staple' })

    const tokens = calls.filter((c) => c.init.method === 'POST').map((c) => (c.init.headers as Record<string, string>)['X-XSRF-TOKEN'])
    expect(tokens).toEqual(['token-1', 'token-2'])
  })

  it('reports a network failure with a Hungarian message instead of a technical error', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => { throw new TypeError('Failed to fetch') }))

    await expect(api.me()).rejects.toMatchObject({ code: 'NETWORK_ERROR', message: expect.stringContaining('Nem sikerült kapcsolódni') })
  })
})

describe('sign-in screens', () => {
  it('shows the uniform H4 message after a failed login and keeps the e-mail filled in', async () => {
    fakeBackend({
      'GET /api/v1/auth/me': [notSignedIn],
      'GET /api/v1/auth/antiforgery': [token],
      'POST /api/v1/auth/login': [{ status: 401, body: { code: 'INVALID_CREDENTIALS', title: 'Hibás e-mail-cím vagy jelszó.' } }],
    })
    render(<App />)

    fireEvent.change(await screen.findByLabelText('E-mail-cím'), { target: { value: 'tomi@example.com' } })
    fireEvent.change(screen.getByLabelText('Jelszó'), { target: { value: 'a wrong but long password' } })
    fireEvent.click(screen.getByRole('button', { name: 'Bejelentkezés' }))

    expect(await screen.findByText('Hibás e-mail-cím vagy jelszó.')).toBeTruthy()
    expect((screen.getByLabelText('E-mail-cím') as HTMLInputElement).value).toBe('tomi@example.com')
  })

  it('shows the field error from the backend under the password field on registration', async () => {
    fakeBackend({
      'GET /api/v1/auth/me': [notSignedIn],
      'GET /api/v1/auth/antiforgery': [token],
      'POST /api/v1/auth/register': [{
        status: 400,
        body: { code: 'VALIDATION_FAILED', title: 'Néhány mező hibás.', errors: { password: ['A jelszó legalább 15 és legfeljebb 128 karakter legyen.'] } },
      }],
    })
    render(<App />)

    fireEvent.click(await screen.findByRole('button', { name: /Regisztrálj/ }))
    fireEvent.change(screen.getByLabelText('E-mail-cím'), { target: { value: 'tomi@example.com' } })
    fireEvent.change(screen.getByLabelText('Jelszó'), { target: { value: 'short' } })
    fireEvent.click(screen.getByRole('button', { name: 'Fiók létrehozása' }))

    const fieldError = await screen.findByText('A jelszó legalább 15 és legfeljebb 128 karakter legyen.')
    const passwordInput = screen.getByLabelText('Jelszó')
    expect(passwordInput.getAttribute('aria-describedby')).toBe(fieldError.id)
    await waitFor(() => expect(document.activeElement).toBe(passwordInput))
  })

  it('returns to sign-in with the expired-session message when the session is gone', async () => {
    fakeBackend({
      'GET /api/v1/auth/me': [{ status: 200, body: { email: 'tomi@example.com', householdId: 'h-1' } }],
      'GET /api/v1/auth/antiforgery': [token],
      'POST /api/v1/auth/logout': [notSignedIn],
    })
    render(<App />)

    fireEvent.click(await screen.findByRole('button', { name: 'Kijelentkezés' }))

    expect(await screen.findByRole('heading', { name: 'Bejelentkezés' })).toBeTruthy()
    expect(screen.getByRole('status').textContent).toBe('Biztonsági okból kiléptettünk. Jelentkezz be újra, és folytathatod.')
  })

  it('shows the empty pantry after a successful registration', async () => {
    fakeBackend({
      'GET /api/v1/auth/me': [notSignedIn, { status: 200, body: { email: 'tomi@example.com', householdId: 'h-1' } }],
      'GET /api/v1/auth/antiforgery': [token],
      'POST /api/v1/auth/register': [{ status: 204 }],
    })
    render(<App />)

    fireEvent.click(await screen.findByRole('button', { name: /Regisztrálj/ }))
    fireEvent.change(screen.getByLabelText('E-mail-cím'), { target: { value: 'tomi@example.com' } })
    fireEvent.change(screen.getByLabelText('Jelszó'), { target: { value: 'correct horse battery staple' } })
    fireEvent.click(screen.getByRole('button', { name: 'Fiók létrehozása' }))

    await waitFor(() => expect(screen.getByText('Még üres a kamrád.')).toBeTruthy())
  })
})
