import type { components } from './schema'
import { FALLBACK_MESSAGE, messageFor, NETWORK_MESSAGE } from './messages'

// The only place that talks to the backend (AGENTS.md: no fetch from components).
// The base URL lives here and nowhere else (ADR-0007).
const BASE_URL = '/api/v1'

export type Me = components['schemas']['MeResponse']
export type LoginRequest = components['schemas']['LoginRequest']
export type RegisterRequest = components['schemas']['RegisterRequest']

export class ApiError extends Error {
  readonly status: number
  readonly code: string
  readonly errors: Record<string, string[]>

  constructor(status: number, code: string, message: string, errors: Record<string, string[]> = {}) {
    super(message)
    this.status = status
    this.code = code
    this.errors = errors
  }
}

// Every component shows errors through this: an ApiError already carries the user-facing text.
export function errorMessage(caught: unknown): string {
  return caught instanceof ApiError ? caught.message : FALLBACK_MESSAGE
}

// ADR-0006: every state-changing request carries the antiforgery token. The token is bound to the
// signed-in user, so it is dropped after login, registration and logout and fetched again lazily
// before the next POST - a failed refresh can then never turn a successful login into an error.
let antiforgeryToken: string | null = null

async function send(method: 'GET' | 'POST', path: string, body?: unknown): Promise<Response> {
  const headers: Record<string, string> = {}
  if (body !== undefined) headers['Content-Type'] = 'application/json'
  if (method !== 'GET' && antiforgeryToken) headers['X-XSRF-TOKEN'] = antiforgeryToken

  let response: Response
  try {
    response = await fetch(BASE_URL + path, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
    })
  } catch {
    throw new ApiError(0, 'NETWORK_ERROR', NETWORK_MESSAGE)
  }

  if (!response.ok) throw await toApiError(response)
  return response
}

async function toApiError(response: Response): Promise<ApiError> {
  const problem = (await response.json().catch(() => ({}))) as {
    code?: string
    title?: string
    errors?: Record<string, string[]>
  }
  const code = problem.code ?? 'UNKNOWN_ERROR'
  return new ApiError(response.status, code, messageFor(code, problem.title), problem.errors ?? {})
}

async function refreshAntiforgeryToken(): Promise<void> {
  const response = await send('GET', '/auth/antiforgery')
  antiforgeryToken = ((await response.json()) as components['schemas']['AntiforgeryTokenResponse']).requestToken
}

async function post(path: string, body: unknown = {}): Promise<void> {
  if (!antiforgeryToken) await refreshAntiforgeryToken()
  try {
    await send('POST', path, body)
  } catch (error) {
    // A stale token (e.g. the user signed in or out in another tab): retry once with a fresh one.
    if (!(error instanceof ApiError && error.code === 'ANTIFORGERY_TOKEN_INVALID')) throw error
    await refreshAntiforgeryToken()
    await send('POST', path, body)
  }
}

export const api = {
  // null = not signed in (the backend answered 401).
  async me(): Promise<Me | null> {
    try {
      return (await (await send('GET', '/auth/me')).json()) as Me
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) return null
      throw error
    }
  },

  async login(request: LoginRequest): Promise<void> {
    await post('/auth/login', request)
    antiforgeryToken = null
  },

  async register(request: RegisterRequest): Promise<void> {
    await post('/auth/register', request)
    antiforgeryToken = null
  },

  async logout(): Promise<void> {
    await post('/auth/logout')
    antiforgeryToken = null
  },
}

// Tests start every case without a cached token.
export function resetClientForTests(): void {
  antiforgeryToken = null
}
