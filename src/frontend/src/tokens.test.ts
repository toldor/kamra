import { describe, expect, it } from 'vitest'
import css from './index.css?raw'

// ADR-0013, ux_flows a11y: the design tokens keep WCAG 2.2 AA contrast. Text needs 4.5:1, focus rings,
// field borders and the menu indicator 3:1 (1.4.11). The values are read from index.css itself, so a
// changed token is checked too.
const tokens = Object.fromEntries([...css.matchAll(/--(c-[a-z-]+):\s*(#[0-9a-f]{6});/g)].map((m) => [m[1], m[2]]))

function luminance(hex: string) {
  const [r, g, b] = [1, 3, 5].map((i) => {
    const c = parseInt(hex.slice(i, i + 2), 16) / 255
    return c <= 0.04045 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4
  })
  return 0.2126 * r + 0.7152 * g + 0.0722 * b
}

function contrast(a: string, b: string) {
  const [hi, lo] = [luminance(tokens[a]), luminance(tokens[b])].sort((x, y) => y - x)
  return (hi + 0.05) / (lo + 0.05)
}

const text: [string, string][] = [
  ['c-ink', 'c-ground'],
  ['c-ink', 'c-surface'],
  ['c-muted', 'c-ground'],
  ['c-muted', 'c-surface'],
  ['c-band-ink', 'c-band'],
  ['c-band-muted', 'c-band'],
  ['c-on-primary', 'c-primary'],
  ['c-on-primary', 'c-primary-hover'],
  ['c-on-primary', 'c-field'],
  ['c-primary', 'c-surface'],
  ['c-primary', 'c-ground'],
  ['c-on-primary', 'c-accent'],
  ['c-on-primary', 'c-expired'],
  ['c-primary', 'c-neutral-tag'],
  ['c-missing', 'c-surface'],
  ['c-missing', 'c-ground'],
]

const nonText: [string, string][] = [
  ['c-focus', 'c-surface'],
  ['c-focus', 'c-ground'],
  ['c-band-ink', 'c-band'],
  ['c-field', 'c-surface'],
  ['c-indicator', 'c-band'],
  ['c-accent', 'c-surface'],
]

describe('design tokens', () => {
  it('reads every token the pairs use', () => {
    for (const name of [...text, ...nonText].flat()) expect(tokens[name], name).toMatch(/^#[0-9a-f]{6}$/)
  })

  it.each(text)('%s on %s is at least 4.5:1', (fg, bg) => {
    expect(contrast(fg, bg)).toBeGreaterThanOrEqual(4.5)
  })

  it.each(nonText)('%s against %s is at least 3:1', (fg, bg) => {
    expect(contrast(fg, bg)).toBeGreaterThanOrEqual(3)
  })

  // The blue ring fails on the olive band (about 2:1), so the band uses its light ink for focus.
  it('gives the band its own focus color', () => {
    expect(contrast('c-focus', 'c-band')).toBeLessThan(3)
    expect(css).toMatch(/\.band :focus-visible\s*\{\s*outline-color: var\(--c-band-ink\);/)
  })
})
