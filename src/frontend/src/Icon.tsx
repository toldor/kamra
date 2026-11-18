import type { ReactNode } from 'react'

// ui_polish_minispec 4.: the menu icons, drawn here (no icon package, ADR-0013). They take the text color, and they
// are decorative: the text next to them names the link.
const shapes = {
  pantry: <><path d="M4 3v18M20 3v18M4 12h16M4 20h16" /><rect x="7" y="6" width="3.5" height="6" /><rect x="13" y="8" width="4" height="4" /><rect x="7.5" y="15.5" width="5" height="4.5" /></>,
  cook: <><path d="M4 11h16v5a4 4 0 0 1-4 4H8a4 4 0 0 1-4-4z" /><path d="M2 11h2M20 11h2" /><path d="M9 7c0-1.2 1.2-1.2 1.2-2.4M13.8 7c0-1.2 1.2-1.2 1.2-2.4" /></>,
  recipes: <><path d="M12 6.5C10 5 7 4.5 4 5v14c3-.5 6 0 8 1.5 2-1.5 5-2 8-1.5V5c-3-.5-6 0-8 1.5z" /><path d="M12 6.5v14" /></>,
  list: <><path d="M10 6h10M10 12h10M10 18h10" /><path d="M4 6l1.2 1.2L7.5 5M4 12l1.2 1.2L7.5 11" /><circle cx="5.5" cy="18" r="1.2" /></>,
  chat: <><path d="M4 5h16v11H10l-5 4v-4H4z" /><path d="M8 9.5h8M8 12.5h5" /></>,
  profile: <><circle cx="12" cy="8" r="4" /><path d="M4 21c0-4 3.6-6 8-6s8 2 8 6" /></>,
  // ui_polish_minispec 24. (K5): the soon-expiring items.
  clock: <><circle cx="12" cy="12" r="8" /><path d="M12 8v4l3 2" /></>,
  // The copy button of an answer (2026-11-17) and its done state.
  copy: <><rect x="9" y="9" width="11" height="11" rx="2" /><path d="M15 9V5a1 1 0 0 0-1-1H5a1 1 0 0 0-1 1v9a1 1 0 0 0 1 1h4" /></>,
  check: <path d="M5 12.5l4.5 4.5L19 7.5" />,
} satisfies Record<string, ReactNode>

export type IconName = keyof typeof shapes

export function Icon({ name, size = 20 }: { name: IconName; size?: number }) {
  return (
    <svg viewBox="0 0 24 24" width={size} height={size} fill="none" stroke="currentColor" strokeWidth={1.8} strokeLinecap="round"
      strokeLinejoin="round" aria-hidden="true" focusable="false" className="icon">
      {shapes[name]}
    </svg>
  )
}
