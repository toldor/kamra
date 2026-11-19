import { useState } from 'react'
import { api, ApiError, errorMessage } from './api/client'

// Signing out, from the Profilom panel or the Profilom page (ui_polish_minispec 1.-2.).
export function useLogout(onSignedOut: (notice?: string) => void) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function logout() {
    setBusy(true)
    setError(null)
    try {
      await api.logout()
      onSignedOut()
    } catch (caught) {
      // 401: the session had already ended - the user wanted to be signed out, and is.
      if (caught instanceof ApiError && caught.status === 401) {
        onSignedOut()
        return
      }
      setError(errorMessage(caught))
      setBusy(false)
    }
  }

  return { logout, busy, error }
}
