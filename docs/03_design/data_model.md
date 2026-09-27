# Adatmodell

Adatbázis: PostgreSQL – sémaváltozás csak EF Core migrációval (lásd `AGENTS.md` 5. pont).
Migrációk helye: `src/backend/KamraApp.Infrastructure/Migrations/`

## Entitások

### PantryItem (Készlettétel)

| Mező | Típus | Kötelező | Leírás |
|---|---|---|---|
| Id | Guid | igen | Elsődleges kulcs |
| Name | string | igen | Hozzávaló neve |
| Quantity | decimal | igen | Mennyiség |
| Unit | string | igen | Mértékegység (pl. kg, db, l) |
| Category | string | igen | Kategória (lejáratbecsléshez) |
| ExpiryDate | DateOnly | igen | Lejárati dátum (megadott vagy becsült) |
| ExpiryEstimated | bool | igen | Igaz, ha a lejárat a kategória alapértelmezett eltarthatóságából becsült (becsült lejárat) |
| CreatedAt | DateTimeOffset | igen | Létrehozás időpontja |
| UpdatedAt | DateTimeOffset | igen | Utolsó módosítás |

### Recipe (Recept)

| Mező | Típus | Kötelező | Leírás |
|---|---|---|---|
| Id | Guid | igen | Elsődleges kulcs |
| Name | string | igen | Recept neve |
| Servings | int | igen | Alapértelmezett adagszám |
| Ingredients | RecipeIngredient[] | igen | Szükséges hozzávalók |

### RecipeIngredient

| Mező | Típus | Leírás |
|---|---|---|
| IngredientName | string | Hozzávaló neve |
| Quantity | decimal | Szükséges mennyiség |
| Unit | string | Mértékegység |

### ShoppingListItem (Bevásárlólista tétel)

| Mező | Típus | Leírás |
|---|---|---|
| Id | Guid | Elsődleges kulcs |
| Name | string | Hozzávaló neve |
| Quantity | decimal | Szükséges mennyiség |
| Unit | string | Mértékegység |
| AddedAt | DateTimeOffset | Hozzáadás időpontja |

## Migrációk

| Név | Leírás | Állapot |
|---|---|---|
| – | Még nincs migráció | – |

## Ismert hiányosságok

- Séma tervezés alatt, változhat.
- A séma még nem követi teljesen a [CONTEXT.md](../../CONTEXT.md) fogalmait és a [scope_contract.md](../01_product/scope_contract.md) story-jait. Hiányzik: Háztartás/felhasználó, kanonikus Hozzávaló (ADR-0003), készletmozgás-napló csökkenési okkal, főzés-esemény, minimumszint, bevásárlójavaslat és tételjavaslat, valamint a kapcsolatok és az adatélettartam. A teljes újratervezés külön adatmodell-tervezési lépés.
