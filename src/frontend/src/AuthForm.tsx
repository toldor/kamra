import { useState, type FormEvent } from 'react'
import { api, ApiError, type Me } from './api/client'
import { FALLBACK_MESSAGE } from './api/messages'

type Mode = 'login' | 'register'

const text = {
  login: { title: 'Bejelentkezés', submit: 'Bejelentkezés', switchLabel: 'Még nincs fiókod? Regisztrálj' },
  register: { title: 'Regisztráció', submit: 'Fiók létrehozása', switchLabel: 'Már van fiókod? Jelentkezz be' },
}

export function AuthForm({ mode, onSignedIn, onSwitchMode }: {
  mode: Mode
  onSignedIn: (me: Me) => void
  onSwitchMode: () => void
}) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})

  async function submit(event: FormEvent) {
    event.preventDefault()
    setBusy(true)
    setError(null)
    setFieldErrors({})
    try {
      await (mode === 'login' ? api.login : api.register)({ email, password })
      const me = await api.me()
      if (me) onSignedIn(me)
    } catch (caught) {
      // ux_flows H4: the e-mail stays filled in; only the message changes.
      setError(caught instanceof ApiError ? caught.message : FALLBACK_MESSAGE)
      if (caught instanceof ApiError) setFieldErrors(caught.errors)
    } finally {
      setBusy(false)
    }
  }

  const fieldError = (field: string) => fieldErrors[field]?.join(' ')

  return (
    <main>
      <h1>{text[mode].title}</h1>
      <form onSubmit={submit} noValidate>
        <label htmlFor="email">E-mail-cím</label>
        <input
          id="email"
          type="email"
          autoComplete="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          aria-invalid={fieldError('email') ? true : undefined}
          aria-describedby={fieldError('email') ? 'email-error' : undefined}
        />
        {fieldError('email') && <p id="email-error" className="field-error">{fieldError('email')}</p>}

        <label htmlFor="password">Jelszó</label>
        <input
          id="password"
          type="password"
          autoComplete={mode === 'login' ? 'current-password' : 'new-password'}
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          aria-invalid={fieldError('password') ? true : undefined}
          aria-describedby={fieldError('password') ? 'password-error' : undefined}
        />
        {fieldError('password') && <p id="password-error" className="field-error">{fieldError('password')}</p>}
        {mode === 'register' && !fieldError('password') && (
          <p className="hint">Legalább 15 karakter. Egy hosszabb, könnyen megjegyezhető mondat is jó.</p>
        )}

        <div role="alert" aria-live="assertive" className="form-error">{error}</div>

        <button type="submit" disabled={busy}>{busy ? 'Kérlek, várj…' : text[mode].submit}</button>
      </form>
      <button type="button" className="link" onClick={onSwitchMode}>{text[mode].switchLabel}</button>
    </main>
  )
}
