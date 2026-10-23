# Megfeleltetési táblázat (traceability)

Minden elfogadási kritériumhoz: melyik felhasználói útvonal ([ux_flows.md](../01_product/ux_flows.md)), melyik végpont ([api.md](../03_design/api.md)) és képernyő valósítja meg, és melyik teszt igazolja. A kritériumok teljes szövege a [scope_contract.md](../01_product/scope_contract.md)-ben van; a „P”-számok a [test_report.md](test_report.md) kézzel számolt példái. Állapot: 2026-10-23, `develop @ 7b6a484`.

Rövidítések a tesztoszlopban – Unit: `tests/KamraApp.Unit.Tests`, Int.: `tests/KamraApp.Integration.Tests`, Vitest: `src/frontend/src`, E2E: `tests/e2e`.

## Keresztmetszeti követelmény – fiók és adatelkülönítés

| # | Követelmény | Flow | Végpont | Képernyő | Teszt | Állapot |
|---|---|---|---|---|---|---|
| K-1 | Bejelentkezés nélkül egyetlen adatvégpont sem érhető el (401) | – | minden végpont (globális `AuthorizeFilter`) | bejelentkezés | Int.: [AuthTests.cs](../../tests/KamraApp.Integration.Tests/AuthTests.cs), a végpontonkénti 401-esek ([PantryApiTests.cs](../../tests/KamraApp.Integration.Tests/PantryApiTests.cs), [ShoppingListApiTests.cs](../../tests/KamraApp.Integration.Tests/ShoppingListApiTests.cs), [RecipesApiTests.cs](../../tests/KamraApp.Integration.Tests/RecipesApiTests.cs) …); E2E: [auth.spec.ts](../../tests/e2e/auth.spec.ts) | Megvalósítva |
| K-2 | Más háztartás adata nem látható és nem módosítható, akkor sem, ha az azonosító ismert (404) | – | minden háztartáshoz kötött végpont | – | Int.: két háztartásos tesztek minden modulban, köztük P17, P18b; ellenséges: [AdversarialPantryApiTests.cs](../../tests/KamraApp.Integration.Tests/AdversarialPantryApiTests.cs), [AdversarialShoppingListApiTests.cs](../../tests/KamraApp.Integration.Tests/AdversarialShoppingListApiTests.cs), [AdversarialRecipesAndCookingTests.cs](../../tests/KamraApp.Integration.Tests/AdversarialRecipesAndCookingTests.cs) | Megvalósítva |

## US-1 – Készlet kezelése

| # | Követelmény | Flow | Végpont | Képernyő | Teszt | Állapot |
|---|---|---|---|---|---|---|
| US-1/1 | Mezők: hozzávaló, mennyiség, egység, kategória, opcionális lejárat | 1. (kézi ág) | `POST /pantry-items`, `GET /categories`, `GET /ingredients` | Kamra – „Új tétel” | Unit: [PantryItemTests.cs](../../tests/KamraApp.Unit.Tests/PantryItemTests.cs), [QuantityTests.cs](../../tests/KamraApp.Unit.Tests/QuantityTests.cs); Int.: [PantryApiTests.cs](../../tests/KamraApp.Integration.Tests/PantryApiTests.cs); Vitest: [PantryPage.test.tsx](../../src/frontend/src/PantryPage.test.tsx); E2E: [pantry.spec.ts](../../tests/e2e/pantry.spec.ts); P1 | Megvalósítva |
| US-1/2 | Becsült lejárat a kategóriából, jelölve és szerkeszthetően; „egyéb”-nél kötelező | 1. | `POST`/`PUT /pantry-items` | Kamra – lista, szerkesztőablak | Unit: [ExpiryTests.cs](../../tests/KamraApp.Unit.Tests/ExpiryTests.cs); Int.: PantryApiTests; E2E: pantry.spec.ts | Megvalósítva |
| US-1/3 | Érvénytelen bevitel nem mentődik, a hibaüzenet megmondja, mit kell javítani | 1., Hibakezelés | `POST`/`PUT /pantry-items` (`VALIDATION_FAILED`, `INGREDIENT_NOT_FOUND`) | Kamra – mezőhibák | Unit: [PantryUseCaseTests.cs](../../tests/KamraApp.Unit.Tests/PantryUseCaseTests.cs); Int.: PantryApiTests, AdversarialPantryApiTests; Vitest: PantryPage.test.tsx | Megvalósítva |
| US-1/4 | Keresés, kategóriaszűrő, „hamarosan lejáró” szűrő, lejárt jelölés | 1. | `GET /pantry-items?search=&category=&expiringSoon=` | Kamra – szűrők | Int.: PantryApiTests; Vitest: PantryPage.test.tsx | Megvalósítva |
| US-1/5 | Csökkenésnél kötelező ok, minden változás a naplóba; a törlés 0-ra csökkentés okkal | 1. | `PUT /pantry-items/{id}` | Kamra – szerkesztőablak | Unit: PantryItemTests; Int.: [StockMovementLogTests.cs](../../tests/KamraApp.Integration.Tests/StockMovementLogTests.cs), PantryApiTests; E2E: pantry.spec.ts | Megvalósítva |
| US-1/6 | Máshol módosított tétel nem íródik felül csendben (friss adat, kiemelés, újra döntés) | 1. (edge case) | `PUT /pantry-items/{id}` (`PANTRY_ITEM_MODIFIED`) | Kamra – szerkesztőablak | Int.: PantryApiTests (verzióütközés); Vitest: PantryPage.test.tsx | Megvalósítva |

## US-2 – Egy mondatos bevitel

| # | Követelmény | Flow | Végpont | Képernyő | Teszt | Állapot |
|---|---|---|---|---|---|---|
| US-2/1–7 | Tételjavaslat egy mondatból, relatív dátumok, nem átváltható egység, LLM-kiesés, meglévő készlet jelzése, idempotens jóváhagyás, időpontok | 1. (mondatos ág) | tervezett (a 2. lépcső API-ja) | Kamra – mondatos bevitel | – | Tervezett – a 2. lépcső (nov. 6.) |

## US-3 – Receptek és ajánlás

| # | Követelmény | Flow | Végpont | Képernyő | Teszt | Állapot |
|---|---|---|---|---|---|---|
| US-3/1a | 20–40 induló recept strukturált hozzávalókkal | 2. | `GET /recommendations`, `GET /recipes` | „Mit főzzek?”, Receptek | Unit: [SeedDataTests.cs](../../tests/KamraApp.Unit.Tests/SeedDataTests.cs); Int.: [SeedMigrationTests.cs](../../tests/KamraApp.Integration.Tests/SeedMigrationTests.cs), [RecommendationsApiTests.cs](../../tests/KamraApp.Integration.Tests/RecommendationsApiTests.cs) | Megvalósítva (20 recept) |
| US-3/1b | Saját recept kézzel (név, adagszám, hozzávalók, elkészítés), saját hozzávalóval | 4. | `POST /recipes`, `POST /ingredients` | „Új recept”, „Új hozzávaló” | Unit: [OwnRecipeTests.cs](../../tests/KamraApp.Unit.Tests/OwnRecipeTests.cs); Int.: RecipesApiTests (P17), AdversarialRecipesAndCookingTests; Vitest: [RecipesPage.test.tsx](../../src/frontend/src/RecipesPage.test.tsx); E2E: [recipes.spec.ts](../../tests/e2e/recipes.spec.ts) | Megvalósítva |
| US-3/1c | Bármely recept archiválható a saját háztartásban; nem ajánlott, nem főzhető újra; a főzések megmaradnak; más háztartásban változatlan | 4. | `POST /recipes/{id}/archive`; `RECIPE_ARCHIVED` az előnézetnél és az új főzésnél | Receptek, Főzés | Unit: OwnRecipeTests, [CookingUseCaseTests.cs](../../tests/KamraApp.Unit.Tests/CookingUseCaseTests.cs); Int.: RecipesApiTests (P18a, P18b); Vitest: RecipesPage.test.tsx, [CookingPage.test.tsx](../../src/frontend/src/CookingPage.test.tsx); E2E: recipes.spec.ts | Megvalósítva |
| US-3/2 | Elkészíthető recept és a rangsor (hamarosan lejárók, legkorábbi lejárat, név) | 2. | `GET /recommendations` | „Mit főzzek?” | Unit: [RecommenderTests.cs](../../tests/KamraApp.Unit.Tests/RecommenderTests.cs); Int.: RecommendationsApiTests (P7); E2E: [cooking.spec.ts](../../tests/e2e/cooking.spec.ts) | Megvalósítva |
| US-3/3 | Majdnem elkészíthető: hiány „van / kell” bontásban; „Bevásárlólistára” egy kattintással | 2., 3. | `GET /recommendations`, `POST /shopping-list/items` | „Mit főzzek?” | Unit: RecommenderTests (P5); Int.: [ShoppingListApiTests.cs](../../tests/KamraApp.Integration.Tests/ShoppingListApiTests.cs) (P11); Vitest: [RecommendationsPage.test.tsx](../../src/frontend/src/RecommendationsPage.test.tsx) | Megvalósítva |
| US-3/4 | Indoklás minden ajánlás mellett | 2. | `GET /recommendations` | „Mit főzzek?” | Unit: RecommenderTests; Vitest: RecommendationsPage.test.tsx | Megvalósítva |
| US-3/5–6 | Alaphozzávalók nem számítanak; a lejárt tétel készlet, jelölve | 2. | `GET /recommendations` | „Mit főzzek?” | Unit: RecommenderTests | Megvalósítva |
| US-3/7 | Determinisztikus sorrend | 2. | `GET /recommendations` | – | Unit: RecommenderTests | Megvalósítva |

## US-4 – Főzés és készletcsökkenés

| # | Követelmény | Flow | Végpont | Képernyő | Teszt | Állapot |
|---|---|---|---|---|---|---|
| US-4/1 | Adagszám; lineáris skálázás, kerekítés | 2. | `GET /recipes/{id}/cooking-preview?servings=` | Főzés | Unit: [CookingTests.cs](../../tests/KamraApp.Unit.Tests/CookingTests.cs) (P3, P4) | Megvalósítva |
| US-4/2 | Szükséges és szerkeszthető felhasznált mennyiség, hiány egy kattintással a listára; nem több a készletnél | 2. | előnézet, `POST /cookings`, `POST /shopping-list/items` | Főzés | Unit: CookingUseCaseTests; Int.: [CookingApiTests.cs](../../tests/KamraApp.Integration.Tests/CookingApiTests.cs) (P10), ShoppingListApiTests (P13); Vitest: CookingPage.test.tsx; E2E: [shopping.spec.ts](../../tests/e2e/shopping.spec.ts) | Megvalósítva |
| US-4/3 | Csak a felhasznált mennyiség vonódik le, FEFO, a lejárt tétel jelölve | 2. | `POST /cookings` | Főzés | Unit: CookingTests (P2); Int.: CookingApiTests (P2, P6); E2E: cooking.spec.ts | Megvalósítva |
| US-4/4 | Közben fogyott készlet: nem mentődik, újraszámol, kiemeli az eltérést | 2. (edge case) | `POST /cookings` (`INSUFFICIENT_STOCK`, `PANTRY_ITEM_MODIFIED`) | Főzés | Int.: CookingApiTests (P9); Vitest: CookingPage.test.tsx (R1) | Megvalósítva |
| US-4/5 | Idempotens megerősítés, az azonosító a munkamenet-tárolóban | 2. (edge case) | `POST /cookings` (201, ismétlésre 200; `IDEMPOTENCY_CONFLICT`) | Főzés | Int.: CookingApiTests (P8), [AdversarialCookingApiTests.cs](../../tests/KamraApp.Integration.Tests/AdversarialCookingApiTests.cs); E2E: cooking.spec.ts (dupla kattintás) | Megvalósítva |
| US-4/6 | Összegző sor: mi fogyott el, mi lett javaslat; a listán lévő nem lesz javaslat | 2., 3. | `POST /cookings` (`depletedIngredients`, `suggestedIngredients`) | Főzés – összegzés | Int.: ShoppingListApiTests (P13); Vitest: CookingPage.test.tsx; E2E: shopping.spec.ts | Megvalósítva |
| US-4/7 | Minden levonás *elfogyott* okkal a naplóba, a főzés tárolódik | 2. | `POST /cookings` | – | Int.: CookingApiTests, StockMovementLogTests | Megvalósítva |

## US-5 – Bevásárlólista

| # | Követelmény | Flow | Végpont | Képernyő | Teszt | Állapot |
|---|---|---|---|---|---|---|
| US-5/1 | Javaslat elfogyáskor és a minimumszint alá csökkenéskor; *kidobtam* és *hibás rögzítés* nem; a listán lévő hozzávaló nem | 3. | `PUT /pantry-items/{id}`, `POST /cookings`, `PUT /ingredients/{id}/minimum` | Bevásárlólista – Javaslatok, Minimumszintek | Unit: [ShoppingListTests.cs](../../tests/KamraApp.Unit.Tests/ShoppingListTests.cs), [ShoppingListUseCaseTests.cs](../../tests/KamraApp.Unit.Tests/ShoppingListUseCaseTests.cs); Int.: ShoppingListApiTests (P14, P19, P20) | Megvalósítva |
| US-5/2 | Elfogadás és elutasítás a listán; indok, gombmagyarázat, javasolt mennyiség | 3. | `POST /shopping-suggestions/{id}/accept`, `…/reject` | Bevásárlólista – Javaslatok | Int.: ShoppingListApiTests, AdversarialShoppingListApiTests; Vitest: [ShoppingListPage.test.tsx](../../src/frontend/src/ShoppingListPage.test.tsx); E2E: shopping.spec.ts (P13) | Megvalósítva |
| US-5/3 | Elutasított javaslat csak feloldás és újabb csökkenés után jön vissza | 3. | – | – | Unit: ShoppingListTests, [ShoppingListCommandTests.cs](../../tests/KamraApp.Unit.Tests/ShoppingListCommandTests.cs); Int.: ShoppingListApiTests (P15) | Megvalósítva |
| US-5/4 | Kézi bővítés, a receptből és a főzésből; kipipálás, törlés; összeadás; a kipipált tétel új mennyiséggel indul | 3. | `GET /shopping-list`, `POST /shopping-list/items`, `PUT`/`DELETE /shopping-list/items/{id}` | Bevásárlólista – A listád | Unit: ShoppingListTests; Int.: ShoppingListApiTests (P11, P12, P16); Vitest: ShoppingListPage.test.tsx; E2E: shopping.spec.ts | Megvalósítva |

## US-6 – Chat asszisztens

| # | Követelmény | Flow | Végpont | Képernyő | Teszt | Állapot |
|---|---|---|---|---|---|---|
| US-6/1–4 | Négy csak olvasó MCP-tool, nem módosít, csak a saját háztartás, LLM-kiesésnél magyar hiba | – | tervezett | Chat | – | Tervezett – a 3. lépcső (nov. 10.) |

## Nem funkcionális követelmények (a scope_contract 3–4. fejezetéből)

| # | Követelmény | Bizonyíték | Állapot |
|---|---|---|---|
| NF-1 | 360 px-es szélesség vízszintes görgetés nélkül; az e2e asztali Chrome-on és 360 px-en fut | Mind a 9 e2e teszt mindkét nézetben, a görgetés-ellenőrzéssel; képernyőképek: `docs/assets/*-360.png` | Megvalósítva |
| NF-2 | A felület nyelve magyar | Vitest-tesztek a pontos szövegekre; ux_flows | Megvalósítva |
| NF-3 | Indítás `docker compose up`-pal 15 percen belül tiszta gépen | [manual_tests.md](manual_tests.md) MT-02: 1 p 26 mp; [deploy_runbook.md](../05_security_ops/deploy_runbook.md) | Megvalósítva |
| NF-4 | Domain + Application sorlefedettség ≥ 80%, CI-ban mérve | CI `backend` job, a lefedettségi kapu | Megvalósítva |
| NF-5 | Nincs secret a repóban; függőség-sérülékenységi vizsgálat a CI-ban | CI `secrets` (gitleaks), NuGet- és npm-audit ([V-51](../07_ai/verification_log.md)) | Megvalósítva |
| NF-6 | Felhasználói kipróbálás a célcsoporttal | [uat.md](uat.md) | Tervezett – 1. kör okt. 26. – nov. 1. |
