# Hibakezelés

Minden API hiba RFC 7807 `ProblemDetails` formátumban érkezik, stabil `code` mezővel.
Stack trace soha nem kerül a kliensnek.
A döntések indoklása: [ADR-0007](../02_architecture/adr/0007-rest-api-hibamodell.md).

## Formátum

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "A készlettétel nem található.",
  "status": 404,
  "code": "PANTRY_ITEM_NOT_FOUND",
  "correlationId": "0HNP1E56RH33M"
}
```

- A `type` az ASP.NET Core alapértelmezése (a státuszkódhoz tartozó RFC 9110 szakasz); saját hiba-URI nincs.
- A `correlationId` a szerver által generált kérésazonosító; ugyanez az érték jön az `X-Correlation-Id` válaszfejlécben, és ezzel kereshető a naplóban ([ADR-0011](../02_architecture/adr/0011-serilog-strukturalt-naplozas.md)).
- A leképezés egy helyen történik: [AppExceptionHandler.cs](../../src/backend/KamraApp.Api/ErrorHandling/AppExceptionHandler.cs). A használati esetek az `AppException` ([AppException.cs](../../src/backend/KamraApp.Application/Common/AppException.cs)) leszármazottait dobják; a kategória határozza meg a státuszt.

## HTTP státusz kategóriák

| Státusz | Kategória | Leírás |
|---|---|---|
| 400 | Validációs hiba | Érvénytelen bemenet |
| 401 | Nem autentikált | Hiányzó, lejárt vagy érvénytelen bejelentkezési cookie, illetve hibás belépési adatok |
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
| `INTERNAL_ERROR` | 500 | Váratlan hiba; a válasz nem tartalmaz belső részletet, a részletek a naplóba kerülnek | Megvalósítva |
| `NOT_FOUND` | 404 | Ismeretlen útvonal (keretrendszer által generált 404) | Megvalósítva |
| `VALIDATION_FAILED` | 400 | Mezőnkénti validációs hiba; az `errors` mező magyar üzeneteket tartalmaz (kulcs: a JSON-mező neve) | Megvalósítva |
| `ANTIFORGERY_TOKEN_INVALID` | 400 | Hiányzó vagy érvénytelen antiforgery token módosító kérésnél | Megvalósítva |
| `UNAUTHENTICATED` | 401 | Bejelentkezés nélküli, lejárt vagy hamisított cookie-val érkező kérés | Megvalósítva |
| `INVALID_CREDENTIALS` | 401 | Hibás e-mail-cím vagy jelszó (a kettő megkülönböztethetetlen, ux_flows H4) | Megvalósítva |
| `FORBIDDEN` | 403 | Bejelentkezett, de nincs jogosultsága | Megvalósítva |
| `EMAIL_ALREADY_REGISTERED` | 409 | A regisztrációs e-mail-cím foglalt (kis- és nagybetűtől függetlenül) | Megvalósítva |
| `RATE_LIMITED` | 429 | Túl sok kérés ugyanarról az IP-címről a bejelentkezésnél vagy a regisztrációnál | Megvalósítva |
| `LOGIN_LOCKED_OUT` | 429 | A fiók 5 sikertelen bejelentkezés után 5 percre zárolva (ux_flows H5) | Megvalósítva |
| `METHOD_NOT_ALLOWED` | 405 | A végpont nem támogatja a HTTP-metódust | Megvalósítva |
| `REQUEST_REJECTED` | 4xx | Egyéb, a keretrendszer által elutasított kérés (pl. 415, vagy `BadHttpRequestException` saját 4xx státusszal), saját kód nélkül | Megvalósítva |
| `INGREDIENT_NOT_FOUND` | 404 | Hozzávaló nem létezik, visszavont, vagy más háztartás saját hozzávalója | Megvalósítva |
| `INGREDIENT_NAME_TAKEN` | 409 | Saját hozzávaló neve foglalt a háztartásban vagy a rendszerlistán | Tervezett |
| `PANTRY_ITEM_NOT_FOUND` | 404 | Készlettétel nem létezik, 0-ra csökkent, vagy más háztartásé (a válasz a három esetben azonos) | Megvalósítva |
| `PANTRY_ITEM_MODIFIED` | 409 | A készlettétel a kliens által látott verzió óta megváltozott ([ADR-0008](../02_architecture/adr/0008-konkurencia-es-idempotencia.md)); a kliens újratölti; elavult verziónál minden más ellenőrzés előtt | Megvalósítva |
| `RECIPE_NOT_FOUND` | 404 | Recept nem létezik | Tervezett |
| `INSUFFICIENT_STOCK` | 409 | A megerősített felhasznált mennyiség több a jelenlegi készletnél; a válasz a friss mennyiségeket tartalmazza (US-4) | Tervezett |
| `IDEMPOTENCY_CONFLICT` | 409 | A kérésazonosítót már egy eltérő tartalmú (recept, adagszám vagy mennyiség) főzés használta ([ADR-0008](../02_architecture/adr/0008-konkurencia-es-idempotencia.md)) | Tervezett |
| `SHOPPING_LIST_ITEM_NOT_FOUND` | 404 | Bevásárlólista-tétel nem létezik | Tervezett |
| `SHOPPING_SUGGESTION_NOT_FOUND` | 404 | Bevásárlójavaslat nem létezik | Tervezett |
| `SHOPPING_SUGGESTION_STATE_CONFLICT` | 409 | Elutasított bevásárlójavaslat elfogadása vagy elfogadott elutasítása ([ADR-0008](../02_architecture/adr/0008-konkurencia-es-idempotencia.md)) | Tervezett |
| `ITEM_PROPOSAL_STATE_CONFLICT` | 409 | Elvetett tételjavaslat jóváhagyása vagy jóváhagyott elvetése (ADR-0007, ADR-0008) | Tervezett |
| `LLM_UNAVAILABLE` | 503 | Az LLM nem érhető el (időtúllépés vagy kiesés újrapróbálás után) | Tervezett |
| `LLM_INVALID_RESPONSE` | 502 | Az LLM válasza nem felel meg a sémának | Tervezett |
| `ITEM_PROPOSAL_NO_ITEMS` | 400 | A szövegből egyetlen tétel sem nyerhető ki | Tervezett |

## Felhasználói üzenetek

A `title` mindig biztonságos, általános magyar szöveg; mezőnkénti validációs hibánál az `errors` magyar üzeneteket tartalmaz. A frontend üzenetkatalógusa csak azokat a kódokat írja felül, amelyekhez a [ux_flows.md](../01_product/ux_flows.md) külön szöveget vagy teendőt rendel; minden más kódnál a `title` jelenik meg. Váratlan hibánál (500) a válasz csak általános adatokat tartalmaz: `status`, az általános magyar `title` („Váratlan hiba történt. Próbáld újra később.”), `code: INTERNAL_ERROR` és a `correlationId`; a kivétel üzenete, típusa és stack trace-e csak a naplóba kerül. A keretrendszer által generált hibák (pl. ismeretlen útvonal) is kapnak kódot és magyar `title`-t.

## Ismert hiányosságok

- Ha a kivétel azután történik, hogy a válasz küldése már elkezdődött (például streamelt válasz közben), az ASP.NET Core nem tud ProblemDetails-t írni: a kliens csonka választ kap, a kivételt a keretrendszer és a kérésnapló naplózza. A jelenlegi végpontok a választ egyben írják ki.

- A hibakódok listája bővülni fog az implementáció során.
- Ismételt jóváhagyás: jóváhagyott tételjavaslat újbóli jóváhagyása 200 ugyanazzal az eredménnyel; ellentmondó művelet (elvetett jóváhagyása, jóváhagyott elvetése) 409. Az egyidejű kérések atomikus kezelése: [ADR-0008](../02_architecture/adr/0008-konkurencia-es-idempotencia.md).
