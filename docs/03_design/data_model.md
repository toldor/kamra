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

## Mennyiségek ([ADR-0002](../02_architecture/adr/0002-fix-atvalthato-mertekegysegek.md))

- Minden hozzávalónak fix **dimenziója** van: tömeg, térfogat vagy darab. A készlettétel és a recepthozzávaló egysége csak a hozzávaló dimenziójából választható (tömeg: g, dkg, kg; térfogat: ml, dl, l; darab: db), így a készlet és a recept mennyisége mindig összevethető.
- A mennyiség **alapegységben** tárolódik (g, ml, db), `numeric(12,3)` típusban; a számítás (illesztés, hiány, levonás, minimumszint) mindig alapegységben fut. A felhasználó által bevitt egység (például dl) külön tárolódik, és csak a megjelenítést befolyásolja: a 30 dkg és a 300 g ugyanaz a mennyiség.
- Kerekítés csak a főzés adagskálázásánál van (US-4): g és ml egészre, db felfelé egészre.

## Tervezett entitások

### Ingredient (Hozzávaló) – [ADR-0003](../02_architecture/adr/0003-kanonikus-hozzavalo-lista.md)

| Mező | Típus | Leírás |
|---|---|---|
| Id | uuid (PK) | GUID v7; rendszer-hozzávalónál a seed JSON-ban rögzített ([ADR-0005](../02_architecture/adr/0005-postgresql-ef-core-migraciok.md)) |
| HouseholdId | uuid (FK → Households.Id), null | null: rendszer-hozzávaló; kitöltve: a háztartás saját hozzávalója, más háztartásban nem látszik |
| Name | text | Megjelenített név |
| NormalizedName | text | Kisbetűs, egységesített szóközű alak az egyediséghez |
| Dimension | text | `Mass` / `Volume` / `Count` (lásd Mennyiségek) |
| DefaultCategory | text | A készlettétel alapértelmezett kategóriája |
| SeedKey | text, null | Rendszer-hozzávalónál egyedi, olvasható, nem változtatható kulcs; az alaphozzávalókat (víz, só, bors) a Domain ezzel azonosítja |
| RetiredAt | timestamptz, null | Visszavont rendszer-hozzávaló |

- Egyediség: rendszer-hozzávalónál a `NormalizedName`, saját hozzávalónál a (`HouseholdId`, `NormalizedName`) pár egyedi (részleges egyedi indexek). Saját hozzávaló neve nem egyezhet aktív rendszer-hozzávaló nevével (a use case ellenőrzi).
- Visszavonás törlés helyett: rendszer-hozzávaló nem törölhető a seedből; a visszavont hozzávaló új készlettételhez és recepthez nem választható, a meglévő hivatkozások változatlanul működnek.
- A saját hozzávaló az MVP-ben csak létrehozható; átnevezés és törlés nincs (Ismert korlátok).

### Kategória

Fix lista a Domainben (enum), alapértelmezett eltarthatósági napértékkel; az adatbázisban szövegként tárolódik. A becsült lejárat = a bevitel napja + a kategória napértéke; ez szervezési segédadat, nem igazolt eltarthatóság, és a felületen becsültként jelölt. A lista és a napértékek forrással: lásd a Kategórialista szakaszt.

#### Kategórialista

Forrás: USDA FSIS [FoodKeeper](https://catalog.data.gov/dataset/fsis-foodkeeper-data) (CC0), adatverzió 108, a [FoodKeeperResearch](https://github.com/BrandonChenze/FoodKeeperResearch) GitHub-tükörből (a hivatalos JSON nem volt letölthető); szúrópróba a hivatalos [FSIS hűtési táblázattal](https://www.foodsafety.gov/food-safety-charts/cold-food-storage-charts), lásd [V-18](../07_ai/verification_log.md).
Számítási szabály: a kategória napértéke a jellemző termékei FoodKeeper-minimumai közül a legkisebb, bontatlan termékre és a szokásos tárolási helyre; a becslés így inkább korábbra esik.

| Kategória (enum) | Nap | Jellemző termékek (FoodKeeper-tartomány) |
|---|---|---|
| Tejtermék (`Dairy`) | 7 | joghurt 1–2 hét, túró (quark) 7–10 nap, író 1–2 hét; tej és tejföl: a FoodKeeper a csomagon lévő dátumot írja |
| Sajt (`Cheese`) | 21 | szeletelt 3–4 hét, reszelt 1 hónap, kemény 6 hónap |
| Tojás (`Eggs`) | 21 | héjas tojás 3–5 hét |
| Friss hús, baromfi, hal (`FreshMeatFish`) | 1 | darált hús 1–2 nap, baromfi 1–2 nap, hal 1–2 nap, szelet és sült 3–5 nap |
| Felvágott, húskészítmény (`ProcessedMeat`) | 7 | bacon 7 nap, virsli (bontatlan) 2 hét |
| Zöldség (`Vegetables`) | 3 | gomba és levélsaláta 3–7 nap, uborka 4–6 nap, paprika 4–14 nap, káposzta 1–2 hét, burgonya 1–2 hónap |
| Gyümölcs (`Fruit`) | 2 | eper 2–3 nap, banán 3 nap, őszibarack és körte 3–5 nap, citrus 10 nap, alma 3 hét |
| Pékáru (`Bakery`) | 1 | friss bagel 1–2 nap (a kifli közelítése), házi/pékségi kenyér 3–5 nap, csomagolt kenyér 14–18 nap |
| Száraz élelmiszer (`DryGoods`) | 90 | teljes kiőrlésű liszt 3–6 hónap, fehér liszt 6–12 hónap, száraz bab 1–2 év, rizs és tészta 2 év |
| Konzerv, befőtt, szósz (`CannedAndSauces`) | 90 | majonéz 3–6 hónap, lekvár 6–18 hónap, savas konzerv 12–18 hónap, ketchup 1 év |
| Fagyasztott (`Frozen`) | 90 | darált hús fagyasztva 3–4 hónap, szelet és sült 4–12 hónap |
| Ital (`Beverages`) | 21 | dobozos gyümölcslé 3 hét |
| Készétel, maradék (`PreparedFood`) | 3 | deli és készételek jellemzően 3–4 nap |
| Egyéb (`Other`) | – | nincs becslés, a lejárat kötelező (US-1) |

### PantryItem (Készlettétel)

| Mező | Típus | Leírás |
|---|---|---|
| Id | uuid (PK) | GUID v7 |
| HouseholdId | uuid (FK → Households.Id) | A tulajdonos háztartás; minden lekérdezés szűri |
| IngredientId | uuid (FK → Ingredient.Id) | A hozzávaló |
| Quantity | numeric(12,3) | Aktuális mennyiség alapegységben; ≥ 0 (CHECK) |
| EnteredUnit | text | A bevitt egység a megjelenítéshez (g, dkg, kg, ml, dl, l, db); a hozzávaló dimenziójából |
| Category | text | Kategória; alapértéke a hozzávaló `DefaultCategory`-ja, tételenként felülírható |
| ExpiryDate | date | Lejárat: megadott vagy becsült; mindig kitöltött |
| ExpiryEstimated | boolean | Igaz, ha a lejárat a kategóriából becsült |
| CreatedAt, UpdatedAt | timestamptz | Létrehozás és utolsó módosítás (UTC) |
| (verzió) | – | Konkurenciakezeléshez; a megoldás: [ADR-0008](../02_architecture/adr/0008-konkurencia-es-idempotencia.md) |

- A 0-ra csökkent tétel sora megmarad (a napló és a G1 hivatkozik rá), a készletlista csak a `Quantity > 0` tételeket mutatja.
- Index: (`HouseholdId`, `IngredientId`, `ExpiryDate`) a FEFO-levonáshoz és az illesztéshez.
- Mennyiséget csak a készletmozgás-naplóval együtt, egy tranzakcióban lehet módosítani. Invariáns: tételenként `Quantity` = a napló `Delta`-inak összege (teszt ellenőrzi, CAP-08).

### StockMovement (Készletmozgás-napló)

Csak hozzáfűzhető: sor nem módosul és nem törlődik.

| Mező | Típus | Leírás |
|---|---|---|
| Id | uuid (PK) | GUID v7 |
| HouseholdId | uuid (FK → Households.Id) | A tulajdonos háztartás |
| PantryItemId | uuid (FK → PantryItem.Id) | Az érintett készlettétel |
| Delta | numeric(12,3) | Előjeles változás alapegységben (+ bevitel, − csökkenés) |
| Reason | text | `Added` (bevitel), `Consumed` (*elfogyott*), `Discarded` (*kidobtam*), `Corrected` (*hibás rögzítés*) |
| CookingId | uuid (FK → Cooking.Id), null | Kitöltve, ha a mozgást főzés okozta (mindig `Consumed`) |
| ExpiryDateAtMovement | date | A készlettétel lejárata a mozgás pillanatában; a metrikák (megmentett főzés, G1) ezzel számolnak, így a lejárat utólagos szerkesztése nem írja át a múltat |
| OccurredAt | timestamptz | Időpont (UTC) |

- A kézi csökkentésnél a felhasználó választja az okot (US-1); a mennyiség szerkesztéssel történő növelése `Corrected`, mert az új vásárlás új készlettétel.
- A nem mennyiségi mezők (lejárat, kategória) szerkesztése nem naplózódik.
- A metrikák a `Reason` szerint számolnak ([metrics.md](../01_product/metrics.md)): a `Corrected` kimarad, a `Discarded` pazarlás.

### Recipe (Recept)

| Mező | Típus | Leírás |
|---|---|---|
| Id | uuid (PK) | GUID v7; induló receptnél a seed JSON-ban rögzített ([ADR-0005](../02_architecture/adr/0005-postgresql-ef-core-migraciok.md)) |
| HouseholdId | uuid (FK → Households.Id), null | null: induló recept; kitöltve: a háztartás saját receptje |
| Name | text | A recept neve |
| Servings | integer | Alapértelmezett adagszám; > 0 (CHECK) |
| Instructions | text | Elkészítés |
| Source | text | `Seed` (induló receptkészlet) vagy `User` (kézi felvitel); az AI-receptötlet (stretch) új értékkel bővítené |
| SeedKey | text, null | Induló receptnél egyedi, nem változtatható kulcs |
| CreatedAt | timestamptz | Létrehozás (UTC) |
| RetiredAt | timestamptz, null | Archivált (saját) vagy visszavont (induló) recept: nem jelenik meg az ajánlásban, a főzések hivatkozása megmarad |

- A saját recept az MVP-ben létrehozható és archiválható; szerkesztés nincs (Ismert korlátok).

### RecipeIngredient (Recept hozzávalója)

| Mező | Típus | Leírás |
|---|---|---|
| RecipeId | uuid (PK, FK → Recipe.Id) | A recept |
| IngredientId | uuid (PK, FK → Ingredient.Id) | A hozzávaló; receptenként egyszer |
| Quantity | numeric(12,3) | Mennyiség az alapértelmezett adagszámra, alapegységben; > 0 |
| EnteredUnit | text | A megjelenítés egysége |

- Az alaphozzávalók (víz, só, bors) szerepelhetnek a receptben, de az illesztés, a hiányszámítás és a levonás figyelmen kívül hagyja őket.
- Az induló receptkészlet minden hozzávalója az induló hozzávaló-listán van (ADR-0003, integrációs teszt).

### Cooking (Főzés)

| Mező | Típus | Leírás |
|---|---|---|
| Id | uuid (PK) | GUID v7 |
| HouseholdId | uuid (FK → Households.Id) | A tulajdonos háztartás |
| RecipeId | uuid (FK → Recipe.Id) | A megfőzött recept |
| Servings | integer | A megadott adagszám; > 0 |
| RequestId | uuid | A megerősítő képernyő kérésazonosítója; (`HouseholdId`, `RequestId`) egyedi – ez biztosítja, hogy az ismételt kérés ne vonjon le újra ([ADR-0008](../02_architecture/adr/0008-konkurencia-es-idempotencia.md)) |
| CookedAt | timestamptz | A megerősítés időpontja (UTC) |

### CookingLine (Főzés sora)

| Mező | Típus | Leírás |
|---|---|---|
| CookingId | uuid (PK, FK → Cooking.Id) | A főzés |
| IngredientId | uuid (PK, FK → Ingredient.Id) | A hozzávaló (alaphozzávaló nem kerül ide) |
| RequiredQuantity | numeric(12,3) | Szükséges mennyiség: a recept mennyisége az adagszámmal skálázva és kerekítve (US-4) |
| UsedQuantity | numeric(12,3) | A felhasználó által megerősített, ténylegesen felhasznált mennyiség; 0 ≤ `UsedQuantity` |

- A hiány = max(0, `RequiredQuantity` − `UsedQuantity`); nem könyvelődik, csak megjelenik, és a bevásárlólistára tehető. A felhasznált mennyiség a szükségesnél több is lehet (a készlet erejéig): ilyenkor nincs hiány.
- A készlettételenkénti (FEFO) levonás a `StockMovement` `CookingId`-s soraiban van; hozzávalónként ezek összege = `UsedQuantity` (teszt ellenőrzi).
- A „megmentett főzés” (North Star) a főzés mozgásaiból, a mozgáskori lejárat (`ExpiryDateAtMovement`) alapján számolódik: van-e köztük olyan készlettétel, amely a főzés napján hamarosan lejáró volt ([metrics.md](../01_product/metrics.md)).

### Bevásárlólista (vázlat – a US-5 előtt véglegesítve)

**IngredientMinimum (Minimumszint):** (`HouseholdId`, `IngredientId`) PK, `MinimumQuantity` numeric(12,3) alapegységben, > 0. Opcionális; ha nincs sor, nincs minimumszint-figyelés.

**ShoppingSuggestion (Bevásárlójavaslat):**

| Mező | Típus | Leírás |
|---|---|---|
| Id | uuid (PK) | GUID v7 |
| HouseholdId | uuid (FK) | A tulajdonos háztartás |
| IngredientId | uuid (FK) | A hozzávaló |
| Trigger | text | `Depleted` (*elfogyott* okkal 0-ra fogyott) vagy `BelowMinimum` |
| State | text | `Open` / `Accepted` / `Rejected` |
| CreatedAt, ResolvedAt | timestamptz | Keletkezés; elfogadás vagy elutasítás |
| ClearedAt | timestamptz, null | Elutasított javaslatnál: a hozzávaló újra a feltétel fölé került |

- A javaslat a kiváltó csökkenéssel egy tranzakcióban keletkezik; *kidobtam* ok nem vált ki javaslatot.
- Háztartásonként és hozzávalónként legfeljebb egy „blokkoló” javaslat lehet (`Open`, vagy `Rejected` és `ClearedAt` null) – részleges egyedi index. Amíg az elfogadott tétel a listán van, nem keletkezik új javaslat.

**ShoppingListItem (Bevásárlólista-tétel):**

| Mező | Típus | Leírás |
|---|---|---|
| Id | uuid (PK) | GUID v7 |
| HouseholdId | uuid (FK) | A tulajdonos háztartás |
| IngredientId | uuid (FK) | A hozzávaló; (`HouseholdId`, `IngredientId`) egyedi, így egy hozzávaló egyszer szerepel |
| Quantity | numeric(12,3), null | Mennyiség alapegységben; újabb hozzáadásnál összeadódik; null, ha a forrás nem ad mennyiséget (például elfogyott-javaslat) |
| EnteredUnit | text, null | A megjelenítés egysége |
| Checked | boolean | Kipipálva |
| AddedAt | timestamptz | Felvétel (UTC) |

- Forrás: elfogadott javaslat, kézi felvétel, majdnem elkészíthető recept hiánya (US-3) vagy a főzés hiánya (US-4).
- Mennyiség összevonása azonos hozzávalónál: két ismert mennyiség összeadódik; ismeretlen (null) és ismert találkozásakor az ismert marad (és az egysége); két ismeretlenből ismeretlen lesz. Az upsert ezt kifejezetten kezeli (`CASE`/`COALESCE`), mert a PostgreSQL-ben a null-lal végzett összeadás null; mindkét beszúrási sorrend tesztelve.
- Nyitott a US-5 előtt: kipipált tételhez újra hozzáadott mennyiség kezelése; a „feltétel fölé kerül” pontos vizsgálata (a bevitel tranzakciójában).

## Migrációk

| Név | Leírás | Állapot |
|---|---|---|
| `InitialIdentityAndHousehold` | Identity-táblák és `Households` | Megvalósítva |

## Ismert hiányosságok

- Csak az 1. lépcső táblái szerepelnek; a tételjavaslat (US-2) a 2. lépcső előtt kerül be.
- A bevásárlólista szerkezete vázlat, a US-5 előtt véglegesedik.
- A saját hozzávaló nem nevezhető át és nem törölhető; a saját recept nem szerkeszthető, csak archiválható.
- A kategória-napértékek amerikai (USDA FoodKeeper) adatokon alapulnak, a magyar termékekre közelítések ([V-18](../07_ai/verification_log.md)).
- A táblák a migrációkkal együtt, storyként készülnek el; a „Megvalósított táblák” szakasz ennek megfelelően bővül.
