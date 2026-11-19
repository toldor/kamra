import { useId, useSyncExternalStore } from 'react'
import { chooseTheme, onThemeChange, readTheme, type ThemeChoice as Choice } from './theme'

const options: [Choice, string][] = [['system', 'Rendszer szerint'], ['light', 'Világos'], ['dark', 'Sötét']]

// ui_polish_minispec 13.: the theme choice in the Profilom panel and on the Profilom page; both show the same value.
export function ThemeChoice() {
  const name = useId()
  const current = useSyncExternalStore(onThemeChange, readTheme)
  return (
    <fieldset className="theme-choice">
      <legend>Téma</legend>
      {options.map(([value, label]) => (
        <label key={value} className="check">
          <input type="radio" name={name} value={value} checked={current === value} onChange={() => chooseTheme(value)} />
          {label}
        </label>
      ))}
    </fieldset>
  )
}
