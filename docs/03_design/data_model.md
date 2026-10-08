# Adatmodell

Adatbázis: PostgreSQL – sémaváltozás csak EF Core migrációval (lásd `AGENTS.md` 5. pont).
Migrációk helye: `src/backend/KamraApp.Infrastructure/Persistence/Migrations/`

## Megvalósított táblák

### Households (Háztartás)

Egy fiók = egy háztartás ([ADR-0006](../02_architecture/adr/0006-cookie-auth-identity.md)); a Domain-entitás: [Household.cs](../../src/backend/KamraApp.Domain/Households/Household.cs).

| Mező | Típus | Leírás |
|---|---|---|
| Id | uuid (PK) | GUID v7, a Domain generálja ([ADR-0005](../02_architecture/adr/0005-postgresql-ef-core-migraciok.md)) |
| OwnerUserId | uuid (FK → AspNetUsers.Id, egyedi) | A háztartást létrehozó felhasználó; a felhasználó törlésekor a háztartás is törlődik (cascade) |
| CreatedAt | timestamptz | Létrehozás időpontja (UTC) |

### Identity-táblák (ASP.NET Core Identity)

`AspNetUsers`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens` – a keretrendszer sémája (`IdentityUserContext<AppUser, Guid>`), szerepkör-táblák nélkül. A felhasználónév az e-mail-cím (egyedi index a normalizált alakon), a jelszó PBKDF2-hash. A háztartás azonosítója `household_id` claimként az `AspNetUserClaims`-ben van, és innen kerül a bejelentkezési cookie-ba.

## Tervezett entitások

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
| `InitialIdentityAndHousehold` | Identity-táblák és `Households` | Megvalósítva |

## Ismert hiányosságok

- Séma tervezés alatt, változhat.
- A séma még nem követi teljesen a [CONTEXT.md](../../CONTEXT.md) fogalmait és a [scope_contract.md](../01_product/scope_contract.md) story-jait. Hiányzik: kanonikus Hozzávaló (ADR-0003), készletmozgás-napló csökkenési okkal, főzés-esemény, minimumszint, bevásárlójavaslat és tételjavaslat, valamint a kapcsolatok és az adatélettartam. A teljes újratervezés külön adatmodell-tervezési lépés.
- Egyidejű készletlevonás (például két fül): a készlettételen konkurenciakezelés kell (optimista zárolás vagy atomikus feltételes frissítés), hogy ne vesszen el levonás. A megoldásról az adatmodell-tervezéskor döntünk, lehetséges ADR-téma.
- A fix kategórialista az alapértelmezett eltarthatósági napértékekkel és azok forrásával itt készül el (a [scope_contract.md](../01_product/scope_contract.md) Korlátok / Adat része erre hivatkozik).
