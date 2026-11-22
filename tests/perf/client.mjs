// A browser-like API client for the scripts in this folder: a cookie jar and the antiforgery header the SPA sends.
// Node 24, built-in modules only.
export class Browser {
  cookies = new Map()
  token = ''

  constructor(base) {
    this.base = base
  }

  async call(method, path, body) {
    const headers = { 'Content-Type': 'application/json', Cookie: [...this.cookies].map(([k, v]) => `${k}=${v}`).join('; ') }
    if (this.token) headers['X-XSRF-TOKEN'] = this.token
    const response = await fetch(this.base + path, { method, headers, body: body ? JSON.stringify(body) : undefined })
    for (const cookie of response.headers.getSetCookie()) {
      const [pair] = cookie.split(';')
      const at = pair.indexOf('=')
      this.cookies.set(pair.slice(0, at), pair.slice(at + 1))
    }
    return response
  }

  async refreshToken() {
    this.token = (await (await this.call('GET', '/api/v1/auth/antiforgery')).json()).requestToken
  }
}
