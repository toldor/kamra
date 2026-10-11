import { useEffect, useRef, useState, type FormEvent } from 'react'
import { api, ApiError, errorMessage, type Me } from './api/client'
import { FALLBACK_MESSAGE } from './api/messages'

type Mode = 'login' | 'register'

const text = {
  login: { title: 'Bejelentkezés', submit: 'Bejelentkezés', switchLabel: 'Még nincs fiókod? Regisztrálj' },
  register: { title: 'Regisztráció', submit: 'Fiók létrehozása', switchLabel: 'Már van fiókod? Jelentkezz be' },
}

export function AuthForm({ mode, notice, onSignedIn, onSwitchMode }: {
  mode: Mode
  // ux_flows: why the user is here, e.g. after an expired session.
  notice?: string
  onSignedIn: (me: Me) => void
  onSwitchMode: () => void
}) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const emailInput = useRef<HTMLInputElement>(null)
  const passwordInput = useRef<HTMLInputElement>(null)

  // ux_flows a11y: move focus to the first invalid field, so a screen reader reads its label and error.
  useEffect(() => {
    if (fieldErrors.email) emailInput.current?.focus()
    else if (fieldErrors.password) passwordInput.current?.focus()
  }, [fieldErrors])

  async function submit(event: FormEvent) {
    event.preventDefault()
    setBusy(true)
    setError(null)
    setFieldErrors({})
    try {
      await (mode === 'login' ? api.login : api.register)({ email, password })
      const me = await api.me()
      if (me) onSignedIn(me)
      else setError(FALLBACK_MESSAGE)
    } catch (caught) {
      // ux_flows H4: the e-mail stays filled in; only the message changes.
      setError(errorMessage(caught))
      if (caught instanceof ApiError) setFieldErrors(caught.errors)
    } finally {
      setBusy(false)
    }
  }

  const fieldError = (field: string) => fieldErrors[field]?.join(' ')
  const showHint = mode === 'register' && !fieldError('password')

  return (
    <main>
      <h1>{text[mode].title}</h1>
      {notice && <p role="status" className="notice">{notice}</p>}
      <form onSubmit={submit} noValidate>
        <label htmlFor="email">E-mail-cím</label>
        <input
          ref={emailInput}
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
          ref={passwordInput}
          id="password"
          type="password"
          autoComplete={mode === 'login' ? 'current-password' : 'new-password'}
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          aria-invalid={fieldError('password') ? true : undefined}
          aria-describedby={fieldError('password') ? 'password-error' : showHint ? 'password-hint' : undefined}
        />
        {fieldError('password') && <p id="password-error" className="field-error">{fieldError('password')}</p>}
        {showHint && (
          <p id="password-hint" className="hint">Legalább 15 karakter. Egy hosszabb, könnyen megjegyezhető mondat is jó.</p>
        )}

        <div role="alert" className="form-error">{error}</div>

        <button type="submit" disabled={busy}>{busy ? 'Kérlek, várj…' : text[mode].submit}</button>
      </form>
      <button type="button" className="link" onClick={onSwitchMode}>{text[mode].switchLabel}</button>
    </main>
  )
}
