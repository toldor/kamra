import type { components } from './schema'
import { messageFor, NETWORK_MESSAGE } from './messages'

// The only place that talks to the backend (AGENTS.md: no fetch from components).
// The base URL lives here and nowhere else (ADR-0007).
const BASE_URL = '/api/v1'

export type Me = components['schemas']['MeResponse']
export type Credentials = components['schemas']['LoginRequest']

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

// ADR-0006: every state-changing request carries the antiforgery token. The token is bound to the
// signed-in user, so it is fetched again after login, registration and logout.
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
  await send('POST', path, body)
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

  async login(credentials: Credentials): Promise<void> {
    await post('/auth/login', credentials)
    await refreshAntiforgeryToken()
  },

  async register(credentials: Credentials): Promise<void> {
    await post('/auth/register', credentials)
    await refreshAntiforgeryToken()
  },

  async logout(): Promise<void> {
    await post('/auth/logout')
    await refreshAntiforgeryToken()
  },
}

// Tests start every case without a cached token.
export function resetClientForTests(): void {
  antiforgeryToken = null
}
