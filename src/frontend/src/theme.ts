// ui_polish_minispec 13.: the colour theme - "Rendszer szerint" (the default), "Világos" or "Sötét", kept in this
// browser only (localStorage "kamra:theme", Ü9). The page always carries the resolved theme on <html data-theme>, so
// the CSS has one dark palette; "Rendszer szerint" follows the system's setting, also when it changes.
export type ThemeChoice = 'system' | 'light' | 'dark'

const key = 'kamra:theme'
const changed = 'kamra:theme-changed'
const systemDark = () => window.matchMedia?.('(prefers-color-scheme: dark)')
// Without storage the choice still shows on this page until it is reloaded.
let unstored: ThemeChoice = 'system'

// Storage may be unavailable (private window, blocked site data); then the system decides.
export function readTheme(): ThemeChoice {
  try {
    const stored = localStorage.getItem(key)
    return stored === 'light' || stored === 'dark' ? stored : 'system'
  } catch {
    return unstored
  }
}

function apply(choice: ThemeChoice) {
  document.documentElement.dataset.theme = choice === 'system' ? (systemDark()?.matches ? 'dark' : 'light') : choice
}

export function chooseTheme(choice: ThemeChoice) {
  unstored = choice
  try {
    if (choice === 'system') localStorage.removeItem(key)
    else localStorage.setItem(key, choice)
  } catch {
    // Without storage the choice lasts until the page is reloaded.
  }
  apply(choice)
  window.dispatchEvent(new Event(changed))
}

// main.tsx, before the first render (Ü8: no inline script under the CSP).
export function startTheme() {
  apply(readTheme())
  systemDark()?.addEventListener('change', () => {
    if (readTheme() === 'system') apply('system')
  })
}

// For useSyncExternalStore: every theme choice on the page shows the same value.
export function onThemeChange(listener: () => void) {
  window.addEventListener(changed, listener)
  return () => window.removeEventListener(changed, listener)
}
