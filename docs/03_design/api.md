# REST API

Alap URL: `/api/v1`
Hibák: RFC 7807 `ProblemDetails` (lásd [error_handling.md](error_handling.md)); stílus, verziózás és hibamodell: [ADR-0007](../02_architecture/adr/0007-rest-api-hibamodell.md)
OpenAPI spec: `src/backend/KamraApp.Api/openapi.json` (generált, ne szerkeszd kézzel)

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
| GET | `/health` | Health check |

## Ismert hiányosságok

- Egyetlen végpont sincs még implementálva.
- Az autentikáció/autorizáció tervezés alatt.
- A végpontlista még a scope_contract előtti állapotot tükrözi. Hiányzik többek között a tételjavaslatok jóváhagyása és elvetése, a kézi készletcsökkentés, a receptkezelés, a bevásárlójavaslatok elfogadása és elutasítása, valamint a regisztráció és a bejelentkezés. A lista az API-tervezéskor igazodik a [scope_contract.md](../01_product/scope_contract.md) US-1–US-6 story-jaihoz.
