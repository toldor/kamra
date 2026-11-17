// ui_polish_minispec 13.: the colour theme - "Rendszer szerint" (the default), "Világos" or "Sötét", kept in this
// browser only (localStorage "kamra:theme", Ü9).
export type ThemeChoice = 'system' | 'light' | 'dark'

export function readTheme(): ThemeChoice {
  throw new Error('Not implemented')
}

export function chooseTheme(choice: ThemeChoice): void {
  throw new Error(`Not implemented: ${choice}`)
}

export function startTheme(): void {
  throw new Error('Not implemented')
}
