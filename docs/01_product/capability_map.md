# Capability Map

A termék képességei és megvalósítottsági állapotuk: mennyi a felhasználói érték (**Value**) és mennyi a termékminőség (**Productization**: biztonság, üzemeltethetőség, hibakezelés, minőségbiztosítás). A story-k és az ütemezés forrása: [scope_contract.md](scope_contract.md).

**Frissítés:** minden lépcső végén. „Done” státusz csak evidence- és tesztlinkkel adható. Evidence: repón belüli relatív link (screenshot vagy GIF a `docs/assets/` alatt) vagy CI-futás link. Teszt: konkrét tesztfájl vagy tesztosztály.

## Képességek

| Capability | Kategória | Evidence (link) | Teszt (link) | Státusz |
|---|---|---|---|---|
| CAP-01 Készlet rögzítése és áttekintése (US-1) | Value | [pantry-360.png](../assets/pantry-360.png); [api.md](../03_design/api.md) (Készlet, US-1); [CI](https://github.com/toldor/kamra/actions/runs/38127353373) | [PantryApiTests.cs](../../tests/KamraApp.Integration.Tests/PantryApiTests.cs), [PantryUseCaseTests.cs](../../tests/KamraApp.Unit.Tests/PantryUseCaseTests.cs), [PantryPage.test.tsx](../../src/frontend/src/PantryPage.test.tsx), [pantry.spec.ts](../../tests/e2e/pantry.spec.ts) | Done |
| CAP-02 Bevásárlás rögzítése egy mondattal (US-2) | Value | – | – | Planned |
| CAP-03 „Mit főzzek?” – ajánlás a készletből (US-3) | Value | [recommendations-360.png](../assets/recommendations-360.png) (P7 a böngészőben); [recipes-360.png](../assets/recipes-360.png); [CI](https://github.com/toldor/kamra/actions/runs/39971394255) | [RecommenderTests.cs](../../tests/KamraApp.Unit.Tests/RecommenderTests.cs), [RecommendationsApiTests.cs](../../tests/KamraApp.Integration.Tests/RecommendationsApiTests.cs), [RecommendationsPage.test.tsx](../../src/frontend/src/RecommendationsPage.test.tsx), [OwnRecipeTests.cs](../../tests/KamraApp.Unit.Tests/OwnRecipeTests.cs), [RecipesApiTests.cs](../../tests/KamraApp.Integration.Tests/RecipesApiTests.cs), [RecipesPage.test.tsx](../../src/frontend/src/RecipesPage.test.tsx), [recipes.spec.ts](../../tests/e2e/recipes.spec.ts) | Done – ajánlás, „Bevásárlólistára”, saját recept, archiválás és saját hozzávaló |
| CAP-04 Főzés és a készlet automatikus frissítése (US-4) | Value | [cooking-360.png](../assets/cooking-360.png), [cooking-done-360.png](../assets/cooking-done-360.png) (P2 a böngészőben); [CI](https://github.com/toldor/kamra/actions/runs/38163897618), [CI](https://github.com/toldor/kamra/actions/runs/39147109504) | [CookingTests.cs](../../tests/KamraApp.Unit.Tests/CookingTests.cs), [CookingApiTests.cs](../../tests/KamraApp.Integration.Tests/CookingApiTests.cs), [AdversarialCookingApiTests.cs](../../tests/KamraApp.Integration.Tests/AdversarialCookingApiTests.cs), [CookingPage.test.tsx](../../src/frontend/src/CookingPage.test.tsx), [cooking.spec.ts](../../tests/e2e/cooking.spec.ts), [shopping.spec.ts](../../tests/e2e/shopping.spec.ts) | Done – előnézet, FEFO-levonás, idempotencia, konkurencia, a hiány listára tétele, a közben fogyott készlet kiemelése és az összegző mondat a javaslatokról |
| CAP-05 Bevásárlólista javaslatokkal (US-5) | Value | [shopping-360.png](../assets/shopping-360.png) (P13 a böngészőben); [api.md](../03_design/api.md) (Bevásárlólista, US-5); [CI](https://github.com/toldor/kamra/actions/runs/39147109504) | [ShoppingListTests.cs](../../tests/KamraApp.Unit.Tests/ShoppingListTests.cs), [ShoppingListUseCaseTests.cs](../../tests/KamraApp.Unit.Tests/ShoppingListUseCaseTests.cs), [ShoppingListCommandTests.cs](../../tests/KamraApp.Unit.Tests/ShoppingListCommandTests.cs), [ShoppingListApiTests.cs](../../tests/KamraApp.Integration.Tests/ShoppingListApiTests.cs), [AdversarialShoppingListApiTests.cs](../../tests/KamraApp.Integration.Tests/AdversarialShoppingListApiTests.cs), [ShoppingListPage.test.tsx](../../src/frontend/src/ShoppingListPage.test.tsx), [shopping.spec.ts](../../tests/e2e/shopping.spec.ts) | Done – lista, javaslatok indokkal és mennyiséggel, minimumszint, a recept és a főzés bekötése, a felülettel együtt |
| CAP-06 Kérdezés a kamráról élő nyelven (US-6) | Value | – | – | Planned |
| CAP-07 Bejelentkezés és háztartásonkénti adatelkülönítés | Productization | [CI](https://github.com/toldor/kamra/actions/runs/37157528207); [e2e](../../tests/e2e/auth.spec.ts) (regisztráció → üres készlet); [V-10](../07_ai/verification_log.md), [V-11](../07_ai/verification_log.md); adatelkülönítés: [CI](https://github.com/toldor/kamra/actions/runs/38076499766) | [AuthTests.cs](../../tests/KamraApp.Integration.Tests/AuthTests.cs), [AdversarialAuthTests.cs](../../tests/KamraApp.Integration.Tests/AdversarialAuthTests.cs), [AuthUseCaseTests.cs](../../tests/KamraApp.Unit.Tests/AuthUseCaseTests.cs), [PantryApiTests.cs](../../tests/KamraApp.Integration.Tests/PantryApiTests.cs) (S-2), [AdversarialPantryApiTests.cs](../../tests/KamraApp.Integration.Tests/AdversarialPantryApiTests.cs) | Done |
| CAP-08 Készletváltozások naplózása okkal | Productization | [data_model.md](../03_design/data_model.md) (StockMovements); [CI](https://github.com/toldor/kamra/actions/runs/38076499766) | [PantryItemTests.cs](../../tests/KamraApp.Unit.Tests/PantryItemTests.cs), [StockMovementLogTests.cs](../../tests/KamraApp.Integration.Tests/StockMovementLogTests.cs), [PantryApiTests.cs](../../tests/KamraApp.Integration.Tests/PantryApiTests.cs) | Done |
| CAP-09 Folyamatos minőségellenőrzés (build, lint, tesztek, lefedettség, függőségvizsgálat) | Productization | [CI #13 – első zöld futás lefedettségi kapuval](https://github.com/toldor/kamra/actions/runs/37986256261); [ci.yml](../../.github/workflows/ci.yml); [branch-védelem](../assets/branch-ruleset.png) | [test_report.md](../04_quality/test_report.md) (61 teszt), [test_strategy.md](../04_quality/test_strategy.md) | Done |
| CAP-10 Üzemeltethetőség: strukturált naplózás és állapotfigyelés | Productization | [observability.md](../05_security_ops/observability.md) (naplóminta, `/health`); [V-14](../07_ai/verification_log.md); [deploy_runbook.md](../05_security_ops/deploy_runbook.md); [manual_tests.md](../04_quality/manual_tests.md) (MT-02, 1 p 26 mp) | [ErrorHandlingTests.cs](../../tests/KamraApp.Integration.Tests/ErrorHandlingTests.cs), [StartupTests.cs](../../tests/KamraApp.Integration.Tests/StartupTests.cs) | Partial – a deploy runbook kész és tiszta környezetben kipróbálva; metrikák még nincsenek |
| CAP-11 Működés AI-kiesés esetén | Productization | – | – | Planned |
| CAP-12 A siker mérése: North Star és guardrail metrikák | Productization | – | – | Planned |

**Státuszok:** `Planned` · `Partial` · `Done`

## Mérés és kockázat

| Capability | Mérés | Kockázat |
|---|---|---|
| CAP-01 | G2 korai megtartás ([metrics.md](metrics.md)) | R2 pontatlan készlet ([vision.md](vision.md)) |
| CAP-02 | G3 bevitel ideje: p50 ≤ 40 mp, p90 ≤ 90 mp | R1 hibás AI-kimenet |
| CAP-03 | North Star: megmentett főzések | R3 hideg indulás, R4 hozzávaló-egyeztetés |
| CAP-04 | North Star; G1 pazarolt arány | R4 hozzávaló-egyeztetés (rossz levonás) |
| CAP-05 | Mérési ötlet: a bevásárlójavaslatok elfogadási aránya | Zajos, ismétlődő javaslatok → elutasítási szabály (US-5) |
| CAP-06 | Mérési ötlet: a chat-kérdések hány százalékára érkezik hibamentes válasz | Prompt injection → csak olvasó MCP toolok ([mcp_tools.md](../03_design/mcp_tools.md)) |
| CAP-07 | Az adatelkülönítési integrációs tesztek 100%-a zöld | Más háztartás adatának elérése azonosító ismeretében |
| CAP-08 | Minden készletváltozás csökkenési okkal naplózott (invariáns teszt) | Napló nélküli módosítás torzítja a G1-et |
| CAP-09 | A `main` ágon a CI-futások zöld aránya; Domain és Application lefedettség ≥ 80% | Instabil (flaky) tesztek |
| CAP-10 | A health check válaszol; minden hiba `correlationId` alapján visszakereshető | Személyes adat, prompt vagy API-kulcs a naplóban |
| CAP-11 | LLM p95 válaszidő ≤ 10 mp; az időtúllépések aránya | Külső API kiesése → kézi form, AI-funkciók kikapcsolhatók |
| CAP-12 | A metrika-lekérdezések a szintetikus seed-adaton egyeznek a kézzel számolt értékekkel | Felhasználó által rögzített adatból fakadó torzítás |

## Roadmap

Céldátumok a [scope_contract.md](scope_contract.md) ütemezése szerint; ha az ütemezés változik, a scope_contract az irányadó.

| Lépcső | Céldátum | Capability-k |
|---|---|---|
| Alapok | okt. 4. | CAP-07, CAP-09 (bővül a teljes fejlesztés alatt) |
| 1. lépcső – determinisztikus mag | okt. 22. (témavezetői: okt. 23.) | CAP-01, CAP-03, CAP-04, CAP-05, CAP-08, CAP-10 |
| 2. lépcső – AI-bevitel | nov. 3. (témavezetői: nov. 6.) | CAP-02, CAP-11 |
| 3. lépcső – chat és mérés | nov. 10. (témavezetői: nov. 20.) | CAP-06, CAP-12 |

- **Csúszás esetén** először a CAP-06 szűkül; az 1. és a 2. lépcső képességei védettek.
- **Stretch** (nem vállalt, csak ha a 3. lépcső a nov. 10-i feature freeze előtt kész és zöld):
  - *AI-receptötlet mentése jóváhagyással:* a hideg indulást az induló receptkészlet már kezeli, ez csak kényelmi bővítés.
  - *Kipipált bevásárlólista-tételből készletbevitel:* a készletbevitel e nélkül is működik; ez a kör zárását kényelmesíti.
- Az AI-os helyettesítés és adagjavaslat a témavezetővel egyeztetett szűkítés szerint továbbfejlesztési lehetőség ([scope_contract.md](scope_contract.md)).

## Ismert hiányosságok

- Az 1. lépcső felhasználói képességei (CAP-01, CAP-03, CAP-04, CAP-05) a `v0.3.0`-ban készek. A CAP-02 és a CAP-06 még nincs implementálva, ezért ezekhez evidence- és tesztlink sincs. A CAP-07, a CAP-08 és a CAP-09 kész, a CAP-10 *Partial*: a hiányzó részt a Státusz oszlop nevezi meg.
