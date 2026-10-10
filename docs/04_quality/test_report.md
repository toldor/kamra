# Tesztjelentés

Frissítsd minden CI futás után, amelyik releváns változást hoz.
Cél: 30+ automatizált teszt (≥18 unit, ≥6 integrációs, ≥6 e2e/contract), ebből ≥5 negatív eset.

## Környezet

- OS: Windows 11 (fejlesztői gép)
- Runtime/SDK: .NET SDK 10.0.201 (`global.json`), xUnit v3 Microsoft Testing Platform módban
- DB: PostgreSQL 18 (`postgres:18` image Testcontainersszel; a Docker Compose ugyanezt az image-et használja, [ADR-0005](../02_architecture/adr/0005-postgresql-ef-core-migraciok.md)); a teszteknél futó Docker szükséges
- Teszt adat: a migrációval betöltött induló hozzávaló-lista (36 elem, [ingredients.json](../../src/backend/KamraApp.Infrastructure/Persistence/Seed/ingredients.json)); minden más adat szintetikus, tesztenként egyedi felhasználóval és háztartással

## Teszt suite-ek és futtatás

- Összes: `dotnet test` (a repo gyökeréből)
- Unit: `dotnet test --project tests/KamraApp.Unit.Tests`
- Integrációs: `dotnet test --project tests/KamraApp.Integration.Tests`
- Frontend (Vitest): `cd src/frontend && npm ci && npm test`
- E2E (Playwright, futó stack ellen): `cd tests/e2e && npm ci && npx playwright install chromium && npx playwright test` (`E2E_BASE_URL`, alapértelmezetten `http://localhost:5083`). A tesztelt Api-t emelt bejelentkezési limittel kell indítani (`RateLimiting__Auth__PermitLimit=1000`), különben az egymás utáni futások elérik az éles 10 kérés/perc limitet.

## Összesítés

| Kategória | Cél | Jelenlegi | Átmegy |
|---|---|---|---|
| Unit | ≥ 18 | 119 | 119 |
| Integrációs | ≥ 6 | 83 | 83 |
| Frontend (Vitest) | – | 7 | 7 |
| E2E | ≥ 6 | 2 (×2 nézet) | 2 |
| Negatív esetek | ≥ 5 | 117 | 117 |
| **Összesen** | **≥ 30** | **211** | **211** |

## Legutolsó futás eredménye

- Dátum: 2026-10-10
- Eredmény: PASS – .NET 202/202 helyben (az S2 tesztjei háromszor egymás után, flaky-ellenőrzés); Domain + Application sorlefedettség 90,0% (kapu: 80%); Vitest 7/7 és Playwright 4/4 futás a CI-ban
- CI link: [GitHub Actions – run 38076499766](https://github.com/toldor/kamra/actions/runs/38076499766) (2026-10-10, `feature/us-1-pantry`, `a451cf5`): `secrets`, `backend`, `frontend` és `e2e` job zöld

## Tesztelt modulok

| Modul | Típus | Leírás | Állapot |
|---|---|---|---|
| Rétegszabályok ([LayerRulesTests.cs](../../tests/KamraApp.Unit.Tests/LayerRulesTests.cs)) | Unit | A Domain és az Application assembly-hivatkozásai és projektfájl-hivatkozásai az [ADR-0004](../02_architecture/adr/0004-clean-architecture-retegek.md) szerint | ✅ 4/4 |
| Hibakategória → HTTP-státusz ([ErrorCategoryMappingTests.cs](../../tests/KamraApp.Unit.Tests/ErrorCategoryMappingTests.cs)) | Unit | Mind a 8 `ErrorCategory` az [ADR-0007](../02_architecture/adr/0007-rest-api-hibamodell.md) szerinti státuszra képeződik; a kód nélküli keretrendszer-státuszok alapértelmezett kódot kapnak | ✅ 12/12 |
| Hibakezelés, health, correlationId ([ErrorHandlingTests.cs](../../tests/KamraApp.Integration.Tests/ErrorHandlingTests.cs)) | Integrációs | `/health` 200; váratlan hiba 500 belső részlet nélkül (negatív); `AppException` → saját státusz, kód, cím; ismeretlen útvonal 404 ProblemDetails magyar címmel (negatív); rossz HTTP-metódus 405 (negatív); keretrendszer-elutasítás (`BadHttpRequestException`) megtartja a 4xx státuszt, `REQUEST_REJECTED`, belső részlet nélkül (negatív); a `correlationId` egyezik a fejléccel a 409-es és az 500-as úton is, CSP- és `nosniff`-fejléc; a `/health` 200 elérhető adatbázissal | ✅ 7/7 |
| Háztartás létrehozása ([HouseholdTests.cs](../../tests/KamraApp.Unit.Tests/HouseholdTests.cs)) | Unit | GUID v7 azonosító, tulajdonos, létrehozási idő; üres tulajdonos elutasítva (negatív) | ✅ 2/2 |
| Auth-használati esetek ([AuthUseCaseTests.cs](../../tests/KamraApp.Unit.Tests/AuthUseCaseTests.cs)) | Unit | Hibás e-mail, 128-nál hosszabb jelszó, hiányzó törzs elutasítva (negatív); a háztartás tulajdonosa az új felhasználó; hibás belépés → `INVALID_CREDENTIALS`, zárolás → `LOGIN_LOCKED_OUT` (negatív) | ✅ 6/6 |
| Authentikáció ([AuthTests.cs](../../tests/KamraApp.Integration.Tests/AuthTests.cs)) | Integrációs | Az [ADR-0006](../02_architecture/adr/0006-cookie-auth-identity.md) Verification-listája valódi Postgresszel: cookie nélkül és hamisított cookie-val 401; antiforgery token nélkül elutasítva, fiók nem jön létre; rossz jelszó és ismeretlen e-mail azonos válasz; az 5. hibánál zárolás, a helyes jelszó is 429; rate limit 429; 14 karakteres jelszó 400 magyar `errors`-szal; foglalt e-mail (más kis-/nagybetűvel) 409 (mind negatív); a regisztráció háztartást hoz létre és bejelentkeztet; bejelentkezés és kijelentkezés | ✅ 10/10 |
| Ellenséges auth-tesztek ([AdversarialAuthTests.cs](../../tests/KamraApp.Integration.Tests/AdversarialAuthTests.cs)) | Integrációs | Az Antigravity (Gemini 3.1 Pro) által írt támadások: régi cookie kijelentkezés után; más session és bejelentkezés előtti antiforgery tokenje; határértékes jelszavak és e-mailek; felderítés a teljes választörzsből; párhuzamos lockout-megkerülés és kis-/nagybetűs e-mail; párhuzamos regisztráció; csonka JSON (mind negatív). Két valódi hibát találtak ([V-10](../07_ai/verification_log.md), [V-11](../07_ai/verification_log.md)). | ✅ 8/8 |
| Frontend ([App.test.tsx](../../src/frontend/src/App.test.tsx)) | Unit (Vitest) | Az API-kliens elküldi az antiforgery tokent, a ProblemDetails-ből magyar `ApiError` lesz; elavult tokennél egyszer újrapróbál friss tokennel; hálózati hibára magyar üzenet (negatív); hibás belépésnél H4 üzenet, az e-mail megmarad (negatív); a jelszó mezőhibája a mező alatt, `aria-describedby`-jal, és a fókusz a hibás mezőre ugrik (negatív); kijelentkezés már lejárt sessionnel üzenet nélkül a bejelentkezésre visz (negatív); sikeres regisztráció után üres készlet | ✅ 7/7 |
| E2E ([auth.spec.ts](../../tests/e2e/auth.spec.ts)) | E2E (Playwright) | Regisztráció → üres készlet → újratöltés után is bejelentkezve → kijelentkezés → bejelentkezés; 360 px-en nincs vízszintes görgetés; hibás belépés H4 üzenettel (negatív). Asztali Chrome és 360 px-es mobil nézet | ✅ 2/2 (4 futás) |
| Indítás ([StartupTests.cs](../../tests/KamraApp.Integration.Tests/StartupTests.cs)) | Integrációs | Connection string nélkül az alkalmazás nem indul el (fail-fast, QA-7, negatív); elérhetetlen adatbázisnál a `/health` 503 (negatív); az antiforgery token sima HTTP-n is kiadható (V-12) | ✅ 3/3 |
| Mértékegységek ([QuantityTests.cs](../../tests/KamraApp.Unit.Tests/QuantityTests.cs)) | Unit | Átváltás alapegységre (dkg → g, kg → g, dl → ml, l → ml), minden egység pontosan egy dimenzióhoz tartozik, visszaváltás a bevitt egységre a válaszhoz ([ADR-0002](../02_architecture/adr/0002-fix-atvalthato-mertekegysegek.md)) | ✅ 18/18 |
| Lejárat ([ExpiryTests.cs](../../tests/KamraApp.Unit.Tests/ExpiryTests.cs)) | Unit | Becsült lejárat mind a 13 kategóriára a [data_model.md](../03_design/data_model.md) napértékeivel; „Egyéb” kategóriánál nincs becslés (negatív); a „hamarosan lejáró” ablak határai (tegnap: lejárt; ma, holnap, holnapután: igen; +3 nap: nem); a „ma” a budapesti naptári nap nyári és téli időben, évváltáskor is, egy tetszőleges időpont (a bevitel napja) is ([V-23](../07_ai/verification_log.md)) | ✅ 24/24 |
| Készlettétel ([PantryItemTests.cs](../../tests/KamraApp.Unit.Tests/PantryItemTests.cs)) | Unit | Létrehozás alapegységben `Added` mozgással; megadott és becsült lejárat, szerkesztéskor a bevitel napjától; elutasítva (negatív): „Egyéb” lejárat nélkül (létrehozáskor és szerkesztéskor), idegen dimenziójú egység, 0 és negatív mennyiség, 3-nál több tizedes és a `numeric(12,3)` fölötti mennyiség ([V-24](../07_ai/verification_log.md)), ok nélküli csökkentés, csökkenési ok növelésnél, 0 alá csökkentés; a növelés `Corrected`; változatlan mennyiségnél nincs mozgás; a mennyiség = a mozgások összege (CAP-08) | ✅ 20/20 |
| Seed-adat ([SeedDataTests.cs](../../tests/KamraApp.Unit.Tests/SeedDataTests.cs)) | Unit | Az induló hozzávaló-lista egyedi azonosítókkal (GUID v7), kulcsokkal és nevekkel, a három alaphozzávalóval; duplikált `SeedKey`, üres kulcs vagy név és ismeretlen kategória leállítja a betöltést (negatív); névnormalizálás ([ADR-0005](../02_architecture/adr/0005-postgresql-ef-core-migraciok.md)) | ✅ 6/6 |
| Seed-migráció ([SeedMigrationTests.cs](../../tests/KamraApp.Integration.Tests/SeedMigrationTests.cs)) | Integrációs | A migráció után az adatbázisban pontosan a seed-fájl rendszer-hozzávalói vannak ([ADR-0005](../02_architecture/adr/0005-postgresql-ef-core-migraciok.md)) | ✅ 1/1 |
| Készletmozgás-napló ([StockMovementLogTests.cs](../../tests/KamraApp.Integration.Tests/StockMovementLogTests.cs)) | Integrációs | Mozgással rendelkező készlettétel nem törölhető: a napló csak hozzáfűzhető, az adatbázis kényszere védi (negatív) | ✅ 1/1 |
| Készlet-használati esetek ([PantryUseCaseTests.cs](../../tests/KamraApp.Unit.Tests/PantryUseCaseTests.cs)) | Unit | Rögzítés és szerkesztés kézi fake portokkal: minden Domain-szabály előbb 400 `VALIDATION_FAILED` mezőkulccsal (0, negatív, 4 tizedes, 100 000 fölött, ismeretlen vagy számként küldött egység, ismeretlen kategória, idegen dimenzió, „egyéb” lejárat nélkül, ok nélküli csökkentés, csökkenési ok növelésnél, ismeretlen vagy `added` ok, túl hosszú keresés; negatív); nem látható hozzávaló és hiányzó tétel 404 (negatív); **elavult verzió 409 minden más ellenőrzés előtt** (a konkurenciateszt által talált hiba regressziós tesztje, negatív); alapértelmezett kategória és budapesti nap; csökkentés és növelés mozgással; újrabecslés a bevitel napjától ([V-25](../07_ai/verification_log.md)) | ✅ 27/27 |
| Készlet-API ([PantryApiTests.cs](../../tests/KamraApp.Integration.Tests/PantryApiTests.cs)) | Integrációs | Kategóriák, normalizált keresés, rögzítés becsült lejárattal és bevitt egységgel, a három szűrő és a rendezés, lejárt jelölés, csökkentés naplóval (mennyiség = a mozgások összege), 0-ra csökkentés; negatív: **elavult verzió 409, a tétel változatlan**; **két egyidejű szerkesztésből egy nyer**; **más háztartás tétele megkülönböztethetetlen a nem létezőtől** (S-2); 401 minden új végponton; antiforgery nélkül elutasítva; érvénytelen bemenet 400 és nem 500 (`POST` és `PUT`); `%` és `_` szó szerint; túl hosszú keresés; CSP szigorú az API-n, nonce-os a Scalaron; Scalar és OpenAPI csak fejlesztésben ([ADR-0008](../02_architecture/adr/0008-konkurencia-es-idempotencia.md), [V-26](../07_ai/verification_log.md), [V-27](../07_ai/verification_log.md)) | ✅ 31/31 |
| Ellenséges készlet-tesztek ([AdversarialPantryApiTests.cs](../../tests/KamraApp.Integration.Tests/AdversarialPantryApiTests.cs)) | Integrációs | Az Antigravity (Gemini 3.1 Pro) által írt támadások: IDOR, verzió-manipuláció és replay (409), mennyiség-határértékek és szöveges szám, enum számként vagy ismeretlenként, a csökkentési ok kijátszása és dimenzióváltás, törölt tétel, hibás JSON szivárgás nélkül, `Production`-ben rejtett fejlesztői eszközök és szigorú CSP (mind negatív). Hibát nem találtak. | ✅ 22/22 |

## Lefedetlen területek

- A recept-, főzés- és bevásárlólista-végpontok és a teljes felület; a készlet API-ja kész, a felülete az S3-ban készül.

## Ismert hiányosságok

- A Domainben még csak a `Household`, az Applicationben csak a hibamodell és az auth-használati esetek vannak: az assembly-alapú tesztek csak a kódban ténylegesen használt hivatkozást látják, ezért a valódi bizonyító erejük a Domain és az Application kódjával együtt nő. A deklarált, de nem használt hivatkozást a projektfájl-alapú tesztek már most is kiszűrik (kézzel igazolva: egy ideiglenes `FrameworkReference` mindkét projektfájl-tesztet elbuktatta).
- Az [ADR-0006](../02_architecture/adr/0006-cookie-auth-identity.md) Verification-listájából a „más háztartás erőforrása 404” (S-2) pontot a [PantryApiTests.cs](../../tests/KamraApp.Integration.Tests/PantryApiTests.cs) lefedi; a „session megmarad az api konténer újraindítása után” pontot a [V-14](../07_ai/verification_log.md) próbája igazolta (Docker Compose, kézi Playwright-szkript); a deploy runbookba az S6-ban kerül.
- A ux_flows „Lejárt munkamenet” üzenete („Biztonsági okból kiléptettünk…”) az első adatlekérő végponttal (US-1, készlet betöltése) kap valódi kiváltó eseményt; a szöveg már az üzenetkatalógusban van.
- Az üres készlet képernyő csak a „Még üres a kamrád.” mondatot mutatja; a ux_flows szövegének a mondatos bevitelre hívó folytatása a US-2-vel kerül be.
- Az e2e tesztek a CI-ban a Docker Compose stack ellen futnak (helyben is lefuttatva a Compose ellen).
- CI: [.github/workflows/ci.yml](../../.github/workflows/ci.yml) – `secrets` (gitleaks a teljes git-historyra, [V-15](../07_ai/verification_log.md)), `backend` (build, format, OpenAPI-diff, tesztek lefedettséggel, NuGet-sérülékenységek), `frontend` (generált típusok diffje, lint, Vitest, build, `npm audit`), `e2e` (Docker Compose stack + Playwright). Az első sikeres futás linkje a „Legutolsó futás eredménye” szakaszban.
- Branch-védelem: a `develop` és a `main` ágra csak PR-ral, zöld `secrets`, `backend`, `frontend` és `e2e` checkkel lehet mergelni, force push és törlés tiltva ([képernyőkép](../assets/branch-ruleset.png)).
- Lefedettség: a `coverlet.MTP` Cobertura-riportot készít (`dotnet test --coverlet --coverlet-output-format cobertura`), a CI artifactként tölti fel. A unit tesztprojekt futása kapu: a Domain és az Application sorlefedettsége legalább 80% ([test_strategy.md](test_strategy.md), [V-20](../07_ai/verification_log.md)); a többi réteg riportolva, küszöb nélkül.
- Élő AI API-hívás nem futhat CI-ban (`[Trait("Category","LiveAI")]` jelöléssel különítendő el).

## Flaky / instabil tesztek

- Nincs ismert flaky teszt. Az e2e első futtatásakor két teszt a bejelentkezési rate limit (10 kérés/perc/IP) miatt 429-et kapott, mert a futások egy percen belül követték egymást; ez a limit helyes működése, ezért a tesztelt Api emelt limittel fut (lásd fent), az éles alapértelmezés változatlan.
