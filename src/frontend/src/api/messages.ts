// ADR-0007: the backend `title` is always a safe Hungarian message. This catalog only overrides the
// codes for which ux_flows.md defines a different text or next step.
const overrides: Record<string, string> = {
  UNAUTHENTICATED: 'Biztonsági okból kiléptettünk. Jelentkezz be újra, és folytathatod.',
  PANTRY_ITEM_NOT_FOUND: 'Ez a tétel már nincs a kamrádban. Frissítettem a listát.',
  // K5: the shopping list page reloads by itself after these.
  SHOPPING_LIST_ITEM_NOT_FOUND: 'Ez a tétel már nincs a bevásárlólistán. Frissítettem a listát.',
  SHOPPING_SUGGESTION_NOT_FOUND: 'Ez a javaslat már nem érhető el. Frissítettem a listát.',
  SHOPPING_SUGGESTION_STATE_CONFLICT: 'Erről a javaslatról már másképp döntöttél, például egy másik ablakban. Frissítettem a listát.',
}

export const NETWORK_MESSAGE =
  'Nem sikerült kapcsolódni a szerverhez. Ellenőrizd az internetkapcsolatot, és próbáld újra.'

export const FALLBACK_MESSAGE = 'Váratlan hiba történt. Próbáld újra később.'

export function messageFor(code: string, title: string | undefined): string {
  return overrides[code] ?? title ?? FALLBACK_MESSAGE
}
