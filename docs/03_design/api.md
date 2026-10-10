# REST API

Alap URL: `/api/v1`
Hibák: RFC 7807 `ProblemDetails` (lásd [error_handling.md](error_handling.md)); stílus, verziózás és hibamodell: [ADR-0007](../02_architecture/adr/0007-rest-api-hibamodell.md)
OpenAPI spec: [`src/backend/KamraApp.Api/openapi.json`](../../src/backend/KamraApp.Api/openapi.json) – OpenAPI 3.1, build közben generálódik (`Microsoft.Extensions.ApiDescription.Server`), ne szerkeszd kézzel; a frontend típusai ebből készülnek (`cd src/frontend && npm run gen:api`). Futás közben csak `Development` környezetben érhető el: `/openapi/v1.json`, valamint a Scalar API-felület a `/scalar` alatt ([ADR-0007](../02_architecture/adr/0007-rest-api-hibamodell.md)); élesben (Docker Compose) nincs közzétéve.

## Autentikáció

Cookie-alapú session ASP.NET Core Identity-vel ([ADR-0006](../02_architecture/adr/0006-cookie-auth-identity.md)): `HttpOnly`, `Secure`, `SameSite=Strict`, 14 nap csúszó lejárattal.

- **Alapértelmezetten minden végpont bejelentkezést követel** (globális `AuthorizeFilter`); a nyilvános végpontok kifejezetten jelöltek. Bejelentkezés nélkül: 401 `UNAUTHENTICATED`.
- **Antiforgery:** minden nem biztonságos (POST, PUT, DELETE) kéréshez az `X-XSRF-TOKEN` fejlécben kell a `GET /auth/antiforgery` által adott token. A token a bejelentkezett felhasználóhoz kötött, ezért bejelentkezés, regisztráció és kijelentkezés után újat kell kérni. Hiányzó vagy hibás token: 400 `ANTIFORGERY_TOKEN_INVALID`.
- **Rate limit:** a `register` és a `login` végpont IP-címenként alapértelmezetten 10 kérés / 60 mp (`RateLimiting:Auth:PermitLimit`, `RateLimiting:Auth:WindowSeconds`); túllépésnél 429 `RATE_LIMITED`.

### Auth (`/auth`)

| Metódus | Útvonal | Hozzáférés | Kérés | Válasz | Hibakódok |
|---|---|---|---|---|---|
| GET | `/auth/antiforgery` | nyilvános | – | 200 `{ "requestToken": "..." }` | – |
| POST | `/auth/register` | nyilvános, rate limit | `{ "email", "password" }` (e-mail ≤ 256, jelszó 15–128 karakter) | 204, a felhasználó bejelentkezett | `VALIDATION_FAILED`, `EMAIL_ALREADY_REGISTERED`, `ANTIFORGERY_TOKEN_INVALID`, `RATE_LIMITED` |
| POST | `/auth/login` | nyilvános, rate limit | `{ "email", "password" }` | 204, a felhasználó bejelentkezett | `VALIDATION_FAILED`, `INVALID_CREDENTIALS`, `LOGIN_LOCKED_OUT`, `ANTIFORGERY_TOKEN_INVALID`, `RATE_LIMITED` |
| POST | `/auth/logout` | bejelentkezett | – | 204; a cookie törölve, és a felhasználó minden korábbi sessionje érvénytelen (új security stamp) | `UNAUTHENTICATED`, `ANTIFORGERY_TOKEN_INVALID` |
| GET | `/auth/me` | bejelentkezett | – | 200 `{ "email", "householdId" }` | `UNAUTHENTICATED` |

## Végpontok – 1. lépcső

Útvonal-szintű terv; a kérés- és válaszsémák a megvalósítással együtt kerülnek az OpenAPI-ba. Minden végpont bejelentkezést követel, és csak a saját háztartás adatát éri el: más háztartás erőforrására 404, a nem létezővel megkülönböztethetetlenül (QA-1). A konkurencia- és idempotencia-szabályok: [ADR-0008](../02_architecture/adr/0008-konkurencia-es-idempotencia.md).

| Metódus | Útvonal | Leírás | Story | Fő hibakódok | Állapot |
|---|---|---|---|---|---|
| GET | `/categories` | Kategórialista az alapértelmezett eltarthatósággal | US-1 | – | Megvalósítva |
| GET | `/ingredients?search=` | Választható hozzávalók: rendszer (nem visszavont) és saját | US-1, US-3 | `VALIDATION_FAILED` | Megvalósítva |
| POST | `/ingredients` | Saját hozzávaló (név, dimenzió, alapértelmezett kategória) → 201 | US-1 | `VALIDATION_FAILED`, `INGREDIENT_NAME_TAKEN` | Tervezett |
| GET | `/pantry-items?search=&category=&expiringSoon=` | Készlet (csak `Quantity > 0`), tételenként `version`, becsült és lejárt jelöléssel | US-1 | `VALIDATION_FAILED` | Megvalósítva |
| POST | `/pantry-items` | Készlettétel rögzítése (`Added` mozgás) → 201 | US-1 | `VALIDATION_FAILED`, `INGREDIENT_NOT_FOUND` | Megvalósítva |
| PUT | `/pantry-items/{id}` | Szerkesztés, csökkentés és törlés (0-ra csökkentés); kötelező `version`, csökkenésnél kötelező ok | US-1 | `VALIDATION_FAILED`, `PANTRY_ITEM_NOT_FOUND`, `PANTRY_ITEM_MODIFIED` | Megvalósítva |
| GET | `/recipes` | Receptek (induló és saját, archivált nélkül) | US-3 | – | Tervezett |
| GET | `/recipes/{id}` | Recept részletei | US-3 | `RECIPE_NOT_FOUND` | Tervezett |
| POST | `/recipes` | Saját recept → 201 | US-3 | `VALIDATION_FAILED`, `INGREDIENT_NOT_FOUND` | Tervezett |
| POST | `/recipes/{id}/archive` | Saját recept archiválása → 204 | US-3 | `RECIPE_NOT_FOUND`, `FORBIDDEN` (induló recept) | Tervezett |
| GET | `/recommendations` | Elkészíthető és majdnem elkészíthető receptek indoklással és hiánnyal | US-3 | – | Tervezett |
| GET | `/recipes/{id}/cooking-preview?servings=` | Megerősítő képernyő: hozzávalónként szükséges, elérhető és alapértelmezett felhasznált mennyiség, FEFO-felosztás | US-4 | `RECIPE_NOT_FOUND`, `VALIDATION_FAILED` | Tervezett |
| POST | `/cookings` | Főzés megerősítése (`requestId`, recept, adagszám, felhasznált mennyiségek) → 201; ismételt kérés → 200 azonos törzzsel | US-4 | `VALIDATION_FAILED`, `RECIPE_NOT_FOUND`, `INSUFFICIENT_STOCK`, `PANTRY_ITEM_MODIFIED`, `IDEMPOTENCY_CONFLICT` | Tervezett |
| GET | `/shopping-list` | Bevásárlólista-tételek | US-5 | – | Tervezett |
| POST | `/shopping-list/items` | Felvétel; azonos hozzávalónál a mennyiség összeadódik (upsert) | US-3, US-4, US-5 | `VALIDATION_FAILED`, `INGREDIENT_NOT_FOUND` | Tervezett |
| PUT | `/shopping-list/items/{id}` | Kipipálás, mennyiség módosítása | US-5 | `SHOPPING_LIST_ITEM_NOT_FOUND` | Tervezett |
| DELETE | `/shopping-list/items/{id}` | Tétel törlése → 204 | US-5 | `SHOPPING_LIST_ITEM_NOT_FOUND` | Tervezett |
| GET | `/shopping-suggestions` | Nyitott bevásárlójavaslatok | US-5 | – | Tervezett |
| POST | `/shopping-suggestions/{id}/accept` | Elfogadás: a tétel a listára kerül | US-5 | `SHOPPING_SUGGESTION_NOT_FOUND`, `SHOPPING_SUGGESTION_STATE_CONFLICT` | Tervezett |
| POST | `/shopping-suggestions/{id}/reject` | Elutasítás | US-5 | `SHOPPING_SUGGESTION_NOT_FOUND`, `SHOPPING_SUGGESTION_STATE_CONFLICT` | Tervezett |
| PUT | `/ingredients/{id}/minimum` | Minimumszint beállítása vagy törlése (null) | US-5 | `INGREDIENT_NOT_FOUND`, `VALIDATION_FAILED` | Tervezett |

A tételjavaslat (`/item-proposals`, US-2) és a chat (US-6) végpontjai a 2. és a 3. lépcső előtt kerülnek be.

### Készlet (US-1) – a megvalósított szerződés

- **Kategóriák:** `GET /categories` → `[{ category, shelfLifeDays }]`; az `other` kategóriánál `shelfLifeDays: null` (nincs becslés, a lejárat kötelező).
- **Hozzávalók:** `GET /ingredients?search=` → `[{ id, name, dimension, defaultCategory }]` név szerint rendezve.
- **Készlettétel (válasz):** `{ id, ingredientId, ingredientName, amount, unit, quantity, category, expiryDate, expiryEstimated, expired, soonExpiring, version }`. Az `amount` a bevitt egységben (30 dkg), a `quantity` alapegységben (300 g) van; az `expired` és a `soonExpiring` a budapesti „ma” szerint számolódik (hamarosan lejáró: ma, holnap vagy holnapután).
- **Rögzítés:** `POST /pantry-items` `{ ingredientId, amount, unit, category?, expiryDate? }`. Hiányzó kategória → a hozzávaló alapértelmezett kategóriája; hiányzó lejárat → becsült (az „egyéb” kategóriánál kötelező).
- **Szerkesztés, csökkentés, törlés:** `PUT /pantry-items/{id}` `{ amount, unit, category, expiryDate?, reason?, version }`. Csökkenésnél az ok kötelező (`consumed`, `discarded`, `corrected`), növelésnél csak `corrected` vagy ok nélkül; a törlés `amount: 0` okkal. A 0-ra csökkent tétel és más háztartás tétele is 404 `PANTRY_ITEM_NOT_FOUND`, a nem létezővel azonos válasszal. Hiányzó lejárat → becsült a bevitel napjától. Elavult `version` → 409 `PANTRY_ITEM_MODIFIED`, minden más ellenőrzés előtt; a kliens újratölti a tételt.
- **Bemenet:** az egység (`g`, `dkg`, `kg`, `ml`, `dl`, `l`, `db`), a kategória (`dairy`, `dryGoods`, …) és az ok stringként érkezik, kis- és nagybetűre érzéketlenül; az egységnek a hozzávaló dimenziójából kell lennie. A mennyiség 0,001 (szerkesztésnél 0) és 100 000 között, legfeljebb 3 tizedessel, és csak JSON-számként fogadható el (`"200"` nem). Minden hibás bemenet 400 `VALIDATION_FAILED`, mezőnkénti `errors`-szal.
- **Szűrők:** `search` (a normalizált névben, legfeljebb 100 karakter; a `%` és a `_` szó szerint értendő), `category` (ismeretlen érték → 400), `expiringSoon=true`; érvénytelen `expiringSoon` értéknél nincs szűrés. Rendezés: lejárat, majd név.

### Rendszer

| Metódus | Útvonal | Leírás |
|---|---|---|
| GET | `/health` | Health check (nyilvános): 200 `Healthy`, ha a folyamat fut és az adatbázis elérhető |

## Ismert hiányosságok

- A rate limit a kapcsolat IP-címét használja. Proxy vagy Docker Desktop porttovábbítás mögött minden kérés ugyanarról a (gateway-) címről érkezhet, ilyenkor a limit közös az összes kliensre. Megbízható reverse proxy jelenleg nincs, ezért a továbbított IP-fejléceket (`X-Forwarded-For`) szándékosan nem fogadjuk el, mert azt a kliens hamisíthatná.
- Az `openapi.json` a sikeres válaszokat és a kérés-sémákat írja le; a ProblemDetails-hibaválaszok még nincsenek benne végpontonként, azok listája ebben a dokumentumban és az [error_handling.md](error_handling.md)-ben van.
- A CI elbukik, ha a generált `openapi.json` vagy a belőle generált frontend-típusok eltérnek a commitolttól ([ci.yml](../../.github/workflows/ci.yml)).
