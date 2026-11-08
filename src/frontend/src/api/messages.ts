// ADR-0007: the backend `title` is always a safe Hungarian message. This catalog only overrides the
// codes for which ux_flows.md defines a different text or next step.
const overrides: Record<string, string> = {
  UNAUTHENTICATED: 'Biztonsági okból kiléptettünk. Jelentkezz be újra, és folytathatod.',
  PANTRY_ITEM_NOT_FOUND: 'Ez a tétel már nincs a kamrádban. Frissítettem a listát.',
  // K5: the shopping list page reloads by itself after these.
  SHOPPING_LIST_ITEM_NOT_FOUND: 'Ez a tétel már nincs a bevásárlólistán. Frissítettem a listát.',
  SHOPPING_SUGGESTION_NOT_FOUND: 'Ez a javaslat már nem érhető el. Frissítettem a listát.',
  SHOPPING_SUGGESTION_STATE_CONFLICT: 'Erről a javaslatról már másképp döntöttél, például egy másik ablakban. Frissítettem a listát.',
  // us2_minispec K15: the card goes away after these.
  ITEM_PROPOSAL_NOT_FOUND: 'Ez a javaslat már nem érhető el. Levettem a listáról.',
  ITEM_PROPOSAL_STATE_CONFLICT: 'Ezt a javaslatot közben máshol már kihagytad vagy jóváhagytad. Levettem a listáról.',
  // us2_minispec K15: the sentence fails; the text stays, the manual form is offered.
  LLM_UNAVAILABLE: 'Most nem tudom feldolgozni a mondatot, mert a szövegfeldolgozó nem válaszol. A szöveged megmaradt: próbáld újra pár perc múlva, vagy rögzítsd tételenként.',
  LLM_INVALID_RESPONSE: 'Nem sikerült értelmezni a mondatot. A szöveged megmaradt: próbáld újra, fogalmazd át, vagy rögzítsd tételenként.',
  ITEM_PROPOSAL_NO_ITEMS: 'Nem találtam élelmiszert a mondatban. Írd le, mit vettél és mennyit, például: »2 liter tej, 10 tojás«.',
  LLM_DISABLED: 'A mondatos bevitel most ki van kapcsolva. Rögzítsd tételenként.',
}

export const NETWORK_MESSAGE =
  'Nem sikerült kapcsolódni a szerverhez. Ellenőrizd az internetkapcsolatot, és próbáld újra.'

export const FALLBACK_MESSAGE = 'Váratlan hiba történt. Próbáld újra később.'

export function messageFor(code: string, title: string | undefined): string {
  return overrides[code] ?? title ?? FALLBACK_MESSAGE
}
