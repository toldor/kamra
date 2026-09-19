# Adatmodell

Adatbázis: PostgreSQL – sémaváltozás csak EF Core migrációval (lásd `AGENTS.md` 5. pont).
Migrációk helye: `src/backend/KamraApp.Infrastructure/Migrations/`

## Entitások

### PantryItem (Kamra tétel)

| Mező | Típus | Kötelező | Leírás |
|---|---|---|---|
| Id | Guid | igen | Elsődleges kulcs |
| Name | string | igen | Termék neve |
| Quantity | decimal | igen | Mennyiség |
| Unit | string | igen | Mértékegység (pl. kg, db, l) |
| Category | string | igen | Kategória (lejáratbecsléshez) |
| ExpiryDate | DateOnly? | nem | Lejárati dátum (null = becsült) |
| ExpiryEstimated | bool | igen | Igaz, ha az LLM becsülte |
| CreatedAt | DateTimeOffset | igen | Létrehozás időpontja |
| UpdatedAt | DateTimeOffset | igen | Utolsó módosítás |

### Recipe (Recept)

| Mező | Típus | Kötelező | Leírás |
|---|---|---|---|
| Id | Guid | igen | Elsődleges kulcs |
| Name | string | igen | Recept neve |
| Servings | int | igen | Alapértelmezett adagszám |
| Ingredients | RecipeIngredient[] | igen | Szükséges alapanyagok |

### RecipeIngredient

| Mező | Típus | Leírás |
|---|---|---|
| IngredientName | string | Alapanyag neve |
| Quantity | decimal | Szükséges mennyiség |
| Unit | string | Mértékegység |

### ShoppingListItem (Bevásárlólista tétel)

| Mező | Típus | Leírás |
|---|---|---|
| Id | Guid | Elsődleges kulcs |
| Name | string | Termék neve |
| Quantity | decimal | Szükséges mennyiség |
| Unit | string | Mértékegység |
| AddedAt | DateTimeOffset | Hozzáadás időpontja |

## Migrációk

| Név | Leírás | Állapot |
|---|---|---|
| – | Még nincs migráció | – |

## Ismert hiányosságok

- Séma tervezés alatt, változhat.
- Felhasználó entitás (autentikáció) még nem tervezett.
