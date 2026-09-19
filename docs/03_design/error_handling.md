# Hibakezelés

Minden API hiba RFC 7807 `ProblemDetails` formátumban érkezik, stabil `code` mezővel.
Stack trace soha nem kerül a kliensnek.

## Formátum

```json
{
  "type": "https://kamra.app/errors/PANTRY_ITEM_NOT_FOUND",
  "title": "A kamra tétel nem található.",
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

## Hibakódok

| Kód | Státusz | Leírás | Állapot |
|---|---|---|---|
| `PANTRY_ITEM_NOT_FOUND` | 404 | Kamra tétel nem létezik | Tervezett |
| `INVALID_QUANTITY` | 400 | Érvénytelen mennyiség | Tervezett |
| `LLM_PARSE_FAILED` | 400 | LLM nem tudta értelmezni a bemenetet | Tervezett |
| `RECIPE_NOT_FOUND` | 404 | Recept nem létezik | Tervezett |
| `INSUFFICIENT_STOCK` | 409 | Nincs elég alapanyag főzéshez | Tervezett |

## Ismert hiányosságok

- A hibakódok listája bővülni fog az implementáció során.
