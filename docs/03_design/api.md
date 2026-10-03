# REST API

Alap URL: `/api/v1`
Hibák: RFC 7807 `ProblemDetails` (lásd [error_handling.md](error_handling.md)); stílus, verziózás és hibamodell: [ADR-0007](../02_architecture/adr/0007-rest-api-hibamodell.md)
OpenAPI spec: `src/backend/KamraApp.Api/openapi.json` (generált, ne szerkeszd kézzel)

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

## Végpontok

### Kamra (`/pantry`)

| Metódus | Útvonal | Leírás | Állapot |
|---|---|---|---|
| GET | `/pantry` | Összes készlettétel listázása | Nem kész |
| POST | `/pantry` | Új készlettétel hozzáadása | Nem kész |
| PUT | `/pantry/{id}` | Készlettétel módosítása | Nem kész |
| DELETE | `/pantry/{id}` | Készlettétel törlése (csökkenési okkal) | Nem kész |
| POST | `/pantry/quick-add` | Egy mondatos bevitel: tételjavaslatok készítése | Nem kész |

### Receptek (`/recipes`)

| Metódus | Útvonal | Leírás | Állapot |
|---|---|---|---|
| GET | `/recipes/recommendations` | Ajánlás a jelenlegi készletből | Nem kész |
| POST | `/recipes/{id}/cook` | Főzés: készletcsökkentés | Nem kész |

### Bevásárlólista (`/shopping-list`)

| Metódus | Útvonal | Leírás | Állapot |
|---|---|---|---|
| GET | `/shopping-list` | Bevásárlólista lekérése | Nem kész |

### Rendszer

| Metódus | Útvonal | Leírás |
|---|---|---|
| GET | `/health` | Health check (nyilvános): 200 `Healthy`, ha a folyamat fut és az adatbázis elérhető |

## Ismert hiányosságok

- Az auth-végpontokon és a `/health`-en kívül még egyetlen végpont sincs implementálva.
- A rate limit a kapcsolat IP-címét használja. Proxy vagy Docker Desktop porttovábbítás mögött minden kérés ugyanarról a (gateway-) címről érkezhet, ilyenkor a limit közös az összes kliensre. Megbízható reverse proxy jelenleg nincs, ezért a továbbított IP-fejléceket (`X-Forwarded-For`) szándékosan nem fogadjuk el, mert azt a kliens hamisíthatná.
- Az OpenAPI-leírás generálása a walking skeleton S4 szakaszában kerül be; addig a `openapi.json` nem létezik.
- A végpontlista még a scope_contract előtti állapotot tükrözi. Hiányzik többek között a tételjavaslatok jóváhagyása és elvetése, a kézi készletcsökkentés, a receptkezelés, a bevásárlójavaslatok elfogadása és elutasítása. A lista az API-tervezéskor igazodik a [scope_contract.md](../01_product/scope_contract.md) US-1–US-6 story-jaihoz.
