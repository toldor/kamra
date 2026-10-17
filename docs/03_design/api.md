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
| GET | `/ingredients?search=` | Választható hozzávalók: rendszer (nem visszavont) és saját; hozzávalónként a háztartás minimumszintje (`minimum`) vagy null | US-1, US-3 | `VALIDATION_FAILED` | Megvalósítva |
| POST | `/ingredients` | Saját hozzávaló (név, dimenzió, alapértelmezett kategória) → 201 | US-1 | `VALIDATION_FAILED`, `INGREDIENT_NAME_TAKEN` | Tervezett |
| GET | `/pantry-items?search=&category=&expiringSoon=` | Készlet (csak `Quantity > 0`), tételenként `version`, becsült és lejárt jelöléssel | US-1 | `VALIDATION_FAILED` | Megvalósítva |
| POST | `/pantry-items` | Készlettétel rögzítése (`Added` mozgás) → 201 | US-1 | `VALIDATION_FAILED`, `INGREDIENT_NOT_FOUND` | Megvalósítva |
| PUT | `/pantry-items/{id}` | Szerkesztés, csökkentés és törlés (0-ra csökkentés); kötelező `version`, csökkenésnél kötelező ok | US-1 | `VALIDATION_FAILED`, `PANTRY_ITEM_NOT_FOUND`, `PANTRY_ITEM_MODIFIED` | Megvalósítva |
| GET | `/recipes` | Receptek (induló és saját, archivált nélkül) | US-3 | – | Tervezett – okt. 23. (a felület most nem használja) |
| GET | `/recipes/{id}` | Recept részletei | US-3 | `RECIPE_NOT_FOUND` | Tervezett – okt. 23. (a recept szövegét a főzés előnézete adja) |
| POST | `/recipes` | Saját recept → 201 | US-3 | `VALIDATION_FAILED`, `INGREDIENT_NOT_FOUND` | Tervezett |
| POST | `/recipes/{id}/archive` | Recept archiválása a saját háztartásban (az induló is) → 204; ismételt → 204 | US-3 | `RECIPE_NOT_FOUND` | Tervezett |
| GET | `/recommendations` | Elkészíthető és majdnem elkészíthető receptek indoklással és hiánnyal | US-3 | – | Megvalósítva |
| GET | `/recipes/{id}/cooking-preview?servings=` | Megerősítő képernyő: hozzávalónként szükséges, elérhető és alapértelmezett felhasznált mennyiség, FEFO-felosztás | US-4 | `RECIPE_NOT_FOUND`, `VALIDATION_FAILED`, `RECIPE_ARCHIVED` (tervezett) | Megvalósítva |
| POST | `/cookings` | Főzés megerősítése (`requestId`, recept, adagszám, felhasznált mennyiségek) → 201; ismételt kérés → 200 azonos törzzsel | US-4 | `VALIDATION_FAILED`, `RECIPE_NOT_FOUND`, `INSUFFICIENT_STOCK`, `PANTRY_ITEM_MODIFIED`, `IDEMPOTENCY_CONFLICT`, `RECIPE_ARCHIVED` (tervezett; az ismételt kérés nem) | Megvalósítva |
| GET | `/shopping-list` | Bevásárlólista-tételek magyar betűrendben | US-5 | – | Megvalósítva |
| POST | `/shopping-list/items` | Felvétel → 200 a sorral; azonos hozzávalónál összeadódik, kipipált tételnél az új mennyiséggel indul újra | US-3, US-4, US-5 | `VALIDATION_FAILED`, `INGREDIENT_NOT_FOUND` | Megvalósítva |
| PUT | `/shopping-list/items/{id}` | Kipipálás → 200 a sorral; megadott mennyiség lecseréli, hiányzó megtartja | US-5 | `VALIDATION_FAILED`, `SHOPPING_LIST_ITEM_NOT_FOUND` | Megvalósítva |
| DELETE | `/shopping-list/items/{id}` | Tétel törlése → 204 | US-5 | `SHOPPING_LIST_ITEM_NOT_FOUND` | Megvalósítva |
| GET | `/shopping-suggestions` | Nyitott javaslatok, legrégebbi elöl, indokkal és javasolt mennyiséggel | US-5 | – | Megvalósítva |
| POST | `/shopping-suggestions/{id}/accept` | Elfogadás `{ amount?, unit? }` → 204; ismétlés → 204 | US-5 | `VALIDATION_FAILED`, `INGREDIENT_NOT_FOUND`, `SHOPPING_SUGGESTION_NOT_FOUND`, `SHOPPING_SUGGESTION_STATE_CONFLICT` | Megvalósítva |
| POST | `/shopping-suggestions/{id}/reject` | Elutasítás → 204; ismétlés → 204 | US-5 | `SHOPPING_SUGGESTION_NOT_FOUND`, `SHOPPING_SUGGESTION_STATE_CONFLICT` | Megvalósítva |
| PUT | `/ingredients/{id}/minimum` | Minimumszint beállítása `{ amount, unit }` vagy törlése `{}` → 204 | US-5 | `VALIDATION_FAILED`, `INGREDIENT_NOT_FOUND` | Megvalósítva |

A tételjavaslat (`/item-proposals`, US-2) és a chat (US-6) végpontjai a 2. és a 3. lépcső előtt kerülnek be.

### Ajánlás (US-3) – a megvalósított szerződés

- **Válasz:** `{ pantryIsEmpty, makeable: [...], almostMakeable: [...] }`; receptenként `{ recipeId, name, servings, soonExpiringIngredients, expiredIngredients, shortages: [{ ingredientName, missing, available, required, unit }] }`. A hiány mennyiségei a recept egységében vannak („tejföl: 50 g hiányzik (150 g van, 200 g kell)”), az alapértelmezett adagszámra.
- **Szabályok:** az alaphozzávalók (víz, só, bors) nem számítanak; elkészíthető, ha nincs hiány, majdnem elkészíthető 1–2 hiányzó vagy kevés hozzávalóval. A „felhasznált” tételek az alapértelmezett adagszám FEFO-felosztása (a lejártak elsőként); a lejárt tétel készletnek számít és jelölt, de a rangsoron nem javít.
- **Rangsor:** elkészíthetők – a felhasznált hamarosan lejáró tételek száma (csökkenő), a nem lejárt felhasznált tételek legkorábbi lejárata, magyar névsor; majdnem elkészíthetők – kevesebb hiány, a felhasznált hamarosan lejárók száma, név.
- **Üres készletnél** nincs ajánlás (`pantryIsEmpty: true`, üres listák), a ux_flows üres-állapot szövegéhez.

### Főzés (US-4) – a megvalósított szerződés

- **Előnézet** (`GET /recipes/{id}/cooking-preview?servings=`): `servings` 1–100, hiányzó érték esetén a recept adagszáma. Válasz: `{ recipeId, name, servings, recipeServings, instructions, staples, lines: [{ ingredientId, ingredientName, unit, required, available, defaultUsed, shortage, allocations: [{ expiryDate, amount, expired }] }] }`. Minden mennyiség a recept egységében van; `defaultUsed` = min(szükséges, van), az `allocations` ennek FEFO-felosztása (a lejártak elsőként).
- **Megerősítés** (`POST /cookings`): `{ requestId, recipeId, servings, used: [{ ingredientId, amount }] }` → 201, ismételt kérésre 200 ugyanazzal a törzzsel: `{ cookingId, recipeId, recipeName, servings, cookedAt, lines: [{ ingredientName, unit, required, used, shortage }], depletedIngredients, suggestedIngredients }`. A `used` pontosan a recept nem alaphozzávalóit sorolja fel, a recept egységében; alapegységre váltva legfeljebb 3 tizedes.
- **Szabályok:** a szükséges mennyiség g-ban és ml-ben egészre kerekedik (a fél felfelé), db-ban mindig felfelé; a levonás FEFO szerint, tételenként `Consumed` mozgással a főzés azonosítójával; 0 felhasználásnál nincs mozgás; a hiány nem könyvelődik; a felhasznált mennyiség a készlet erejéig több lehet a szükségesnél.
- **Hibák:** `INSUFFICIENT_STOCK` – a friss mennyiségek az `available` kiterjesztésben, levonás nélkül; `IDEMPOTENCY_CONFLICT` – a `requestId`-t eltérő tartalmú főzés használta; `PANTRY_ITEM_MODIFIED` – egy tétel közben megváltozott. Bármely ütközés után a szerver előbb a `requestId`-t keresi: ha közben ugyanez a kérés mentődött, 200-at ad ([ADR-0008](../02_architecture/adr/0008-konkurencia-es-idempotencia.md)).

### Bevásárlólista (US-5) – a megvalósított szerződés

- **Mennyiség a válaszban:** `{ value, unit }` objektum, vagy null, ha nincs mennyiség; a `value` a megjelenített egységben, záró nullák nélkül. (A nullázható `unit` mező a generált OpenAPI-ban a közös `Unit` sémát is nullázhatóvá tenné: [V-37](../07_ai/verification_log.md).)
- **Tétel:** `{ id, ingredientId, ingredientName, amount, checked }`.
- **Javaslat:** `{ id, ingredientId, ingredientName, dimension, trigger, createdAt, recipeName, currentAmount, minimumAmount, suggestedAmount }`. `trigger`: `depleted` (elfogyott) vagy `belowMinimum`. `recipeName`: a főzés receptje, ha az fogyasztotta el. Minimumszintnél a három mennyiség a minimum egységében: a jelenlegi összmennyiség, a minimum és a hozzá hiányzó rész; ha a minimumot közben törölték, csak a jelenlegi mennyiség, alapegységben; elfogyott-javaslatnál mind null.
- **Kérések:** a mennyiség és az egység együtt vagy egyik sem; a mennyiség 0,001 és 100 000 között, legfeljebb 3 tizedessel, JSON-számként; az egység a hozzávaló dimenziójából. Az összeadott mennyiség a sor egységében legfeljebb 100 000 (`VALIDATION_FAILED`, `amount`); eltérő egységek a kisebbikben adódnak össze.
- **Szabályok:** javaslatot csak az *elfogyott* ok vált ki (a főzés is); a *kidobtam* és a *hibás rögzítés* nem; nincs új javaslat, amíg egy nyitott vagy fel nem oldott elutasított létezik, vagy a hozzávaló pipa nélkül a listán van. A főzés válaszában a `suggestedIngredients` sorolja fel, mi lett javaslat.
- **Konkurencia:** minden írás hozzávalónkénti zár alatt, egy tranzakcióban ([ADR-0008](../02_architecture/adr/0008-konkurencia-es-idempotencia.md) 4–6. pont). Más háztartás tétele és javaslata 404, a nem létezővel azonos válasszal.

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

- A 409-es `available` mennyiségeket a felület még nem emeli ki, és az „Újraszámolás” visszaállítja az alapértelmezett értékeket; a teljes eltérés-kiemelés okt. 23-ig készül.
- Az archivált recept előnézetének és első főzésének tiltása az archiválással együtt készül.
- Az ajánlás az összes illeszkedő receptet visszaadja, lapozás nélkül; háztartási méretnél (20–40 recept) erre nincs szükség.
- Egy készlettétel friss adatát (például verzióütközés után) a felület a teljes készletlistából veszi; tételenkénti `GET /pantry-items/{id}` végpont nincs, háztartási méretnél erre nincs szükség.

- A rate limit a kapcsolat IP-címét használja. Proxy vagy Docker Desktop porttovábbítás mögött minden kérés ugyanarról a (gateway-) címről érkezhet, ilyenkor a limit közös az összes kliensre. Megbízható reverse proxy jelenleg nincs, ezért a továbbított IP-fejléceket (`X-Forwarded-For`) szándékosan nem fogadjuk el, mert azt a kliens hamisíthatná.
- Az `openapi.json` a sikeres válaszokat és a kérés-sémákat írja le; a ProblemDetails-hibaválaszok még nincsenek benne végpontonként, azok listája ebben a dokumentumban és az [error_handling.md](error_handling.md)-ben van.
- A CI elbukik, ha a generált `openapi.json` vagy a belőle generált frontend-típusok eltérnek a commitolttól ([ci.yml](../../.github/workflows/ci.yml)).
