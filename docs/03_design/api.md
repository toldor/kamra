# REST API

Alap URL: `/api/v1`
Hibák: RFC 7807 `ProblemDetails` (lásd [error_handling.md](error_handling.md))
OpenAPI spec: `src/backend/KamraApp.Api/openapi.json` (generált, ne szerkeszd kézzel)

## Végpontok

### Kamra (`/pantry`)

| Metódus | Útvonal | Leírás | Állapot |
|---|---|---|---|
| GET | `/pantry` | Összes tétel listázása | Nem kész |
| POST | `/pantry` | Új tétel hozzáadása | Nem kész |
| PUT | `/pantry/{id}` | Tétel módosítása | Nem kész |
| DELETE | `/pantry/{id}` | Tétel törlése | Nem kész |
| POST | `/pantry/quick-add` | Természetes nyelvű gyorsbevitel | Nem kész |

### Receptek (`/recipes`)

| Metódus | Útvonal | Leírás | Állapot |
|---|---|---|---|
| GET | `/recipes/suggestions` | Javaslatok a jelenlegi készletből | Nem kész |
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
