// ADR-0007: the backend `title` is always a safe Hungarian message. This catalog only overrides the
// codes for which ux_flows.md defines a different text or next step.
const overrides: Record<string, string> = {
  UNAUTHENTICATED: 'Biztonsági okból kiléptettünk. Jelentkezz be újra, és folytathatod.',
}

export const NETWORK_MESSAGE =
  'Nem sikerült kapcsolódni a szerverhez. Ellenőrizd az internetkapcsolatot, és próbáld újra.'

export const FALLBACK_MESSAGE = 'Váratlan hiba történt. Próbáld újra később.'

export function messageFor(code: string, title: string | undefined): string {
  return overrides[code] ?? title ?? FALLBACK_MESSAGE
}
