import { useEffect, useId, useRef, useState, type FocusEvent } from 'react'
import type { Me } from './api/client'
import { Icon } from './Icon'
import { useLogout } from './useLogout'
import { routes, type Route } from './useHashRoute'

// ui_polish_minispec 1.: the Profilom button on the band and its panel - a plain disclosure (aria-expanded), not an
// ARIA menu, so Tab moves through it as usual. Escape closes it and gives the focus back to the button; a click
// outside, the focus moving to a control outside (review R1) and another page close it too.
export function ProfileMenu({ me, route, onSignedOut }: { me: Me; route: Route; onSignedOut: (notice?: string) => void }) {
  const [open, setOpen] = useState(false)
  const button = useRef<HTMLButtonElement>(null)
  const box = useRef<HTMLDivElement>(null)
  const panelId = useId()

  const [openedOn, setOpenedOn] = useState(route)
  if (route !== openedOn) {
    setOpenedOn(route)
    setOpen(false)
  }

  useEffect(() => {
    if (!open) return
    const outside = (event: MouseEvent) => {
      if (!box.current?.contains(event.target as Node)) setOpen(false)
    }
    // Review R1: on the document, so it works when the focus is not in the panel (Safari does not focus a clicked button).
    const escape = (event: KeyboardEvent) => {
      if (event.key !== 'Escape') return
      setOpen(false)
      button.current?.focus()
    }
    document.addEventListener('mousedown', outside)
    document.addEventListener('keydown', escape)
    return () => {
      document.removeEventListener('mousedown', outside)
      document.removeEventListener('keydown', escape)
    }
  }, [open])

  // Review R1: Tab out of the panel closes it; focus that falls back to the page itself (a click on plain text) does not.
  function focusOut(event: FocusEvent) {
    if (event.relatedTarget && !box.current?.contains(event.relatedTarget as Node)) setOpen(false)
  }

  return (
    <div ref={box} className="profile" onBlur={focusOut}>
      <button
        ref={button}
        type="button"
        className="profile-button"
        aria-expanded={open}
        aria-controls={open ? panelId : undefined}
        onClick={() => setOpen((current) => !current)}
      >
        <Icon name="profile" />
        Profilom
      </button>
      {open && <ProfilePanel id={panelId} me={me} onSignedOut={onSignedOut} onClose={() => setOpen(false)} />}
    </div>
  )
}

// Its own component, so a failed logout's message goes with the panel (review R3).
function ProfilePanel({ id, me, onSignedOut, onClose }: { id: string; me: Me; onSignedOut: (notice?: string) => void; onClose: () => void }) {
  const { logout, busy, error } = useLogout(onSignedOut)
  return (
    <section id={id} aria-label="Profilom" className="profile-panel">
      <p className="who">Bejelentkezve: {me.email}</p>
      <a href={routes.profile} onClick={onClose}>Profilom</a>
      <button type="button" className="link" onClick={() => void logout()} disabled={busy}>Kijelentkezés</button>
      <p role="alert" className="form-error">{error}</p>
    </section>
  )
}
