# Hibakezelés

Minden API hiba RFC 7807 `ProblemDetails` formátumban érkezik, stabil `code` mezővel.
Stack trace soha nem kerül a kliensnek.
A döntések indoklása: [ADR-0007](../02_architecture/adr/0007-rest-api-hibamodell.md).

## Formátum

```json
{
  "type": "https://kamra.app/errors/PANTRY_ITEM_NOT_FOUND",
  "title": "A készlettétel nem található.",
  "status": 404,
  "code": "PANTRY_ITEM_NOT_FOUND",
  "correlationId": "abc-123"
}
```

## HTTP státusz kategóriák

| Státusz | Kategória | Leírás |
|---|---|---|
| 400 | Validációs hiba | Érvénytelen bemenet |
| 401 | Nem autentikált | Hiányzó vagy érvénytelen token |
| 403 | Tiltott | Nincs jogosultság |
| 404 | Nem található | Az erőforrás nem létezik |
| 409 | Konfliktus | Üzleti szabály megsértése |
| 429 | Rate limit | Túl sok kérés |
| 500 | Belső hiba | Váratlan szerverhiba |
| 502 | Hibás külső válasz | Az LLM válasza nem felel meg a sémának |
| 503 | Külső szolgáltatás nem elérhető | Az LLM nem válaszolt az újrapróbálás után sem |

## Hibakódok

| Kód | Státusz | Leírás | Állapot |
|---|---|---|---|
| `PANTRY_ITEM_NOT_FOUND` | 404 | Készlettétel nem létezik | Tervezett |
| `INVALID_QUANTITY` | 400 | Érvénytelen mennyiség | Tervezett |
| `LLM_UNAVAILABLE` | 503 | Az LLM nem érhető el (időtúllépés vagy kiesés újrapróbálás után) | Tervezett |
| `LLM_INVALID_RESPONSE` | 502 | Az LLM válasza nem felel meg a sémának | Tervezett |
| `ITEM_PROPOSAL_NO_ITEMS` | 400 | A szövegből egyetlen tétel sem nyerhető ki | Tervezett |
| `RECIPE_NOT_FOUND` | 404 | Recept nem létezik | Tervezett |

## Felhasználói üzenetek

A `title` mindig biztonságos, általános magyar szöveg; mezőnkénti validációs hibánál az `errors` magyar üzeneteket tartalmaz. A frontend üzenetkatalógusa csak azokat a kódokat írja felül, amelyekhez a [ux_flows.md](../01_product/ux_flows.md) külön szöveget vagy teendőt rendel; minden más kódnál a `title` jelenik meg. Váratlan hibánál (500) a válasz csak a `correlationId`-t tartalmazza.

## Ismert hiányosságok

- A hibakódok listája bővülni fog az implementáció során.
- Ismételt jóváhagyás: jóváhagyott tételjavaslat újbóli jóváhagyása 200 ugyanazzal az eredménnyel; ellentmondó művelet (elvetett jóváhagyása, jóváhagyott elvetése) 409. Az egyidejű kérések atomikus kezelése a konkurenciakezelési ADR-ben dől el.
