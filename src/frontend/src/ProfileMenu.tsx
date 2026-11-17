import { useEffect, useId, useRef, useState, type KeyboardEvent } from 'react'
import type { Me } from './api/client'
import { Icon } from './Icon'
import { useLogout } from './useLogout'
import { routes, type Route } from './useHashRoute'

// ui_polish_minispec 1.: the Profilom button on the band and its panel - a plain disclosure (aria-expanded), not an
// ARIA menu, so Tab moves through it as usual. Escape closes it and gives the focus back to the button; a click
// outside and another page close it too.
export function ProfileMenu({ me, route, onSignedOut }: { me: Me; route: Route; onSignedOut: (notice?: string) => void }) {
  const [open, setOpen] = useState(false)
  const button = useRef<HTMLButtonElement>(null)
  const box = useRef<HTMLDivElement>(null)
  const panelId = useId()
  const { logout, busy, error } = useLogout(onSignedOut)

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
    document.addEventListener('mousedown', outside)
    return () => document.removeEventListener('mousedown', outside)
  }, [open])

  function keyDown(event: KeyboardEvent) {
    if (event.key === 'Escape' && open) {
      setOpen(false)
      button.current?.focus()
    }
  }

  return (
    <div ref={box} className="profile" onKeyDown={keyDown}>
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
      {open && (
        <section id={panelId} aria-label="Profilom" className="profile-panel">
          <p className="who">Bejelentkezve: {me.email}</p>
          <a href={routes.profile} onClick={() => setOpen(false)}>Profilom</a>
          <button type="button" className="link" onClick={() => void logout()} disabled={busy}>Kijelentkezés</button>
          <p role="alert" className="form-error">{error}</p>
        </section>
      )}
    </div>
  )
}
