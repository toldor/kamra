# Tesztjelentés

Frissítsd minden CI futás után, amelyik releváns változást hoz.
Cél: 30+ automatizált teszt (≥18 unit, ≥6 integrációs, ≥6 e2e/contract), ebből ≥5 negatív eset.

## Környezet

- OS: Windows 11 (fejlesztői gép)
- Runtime/SDK: .NET SDK 10.0.201 (`global.json`), xUnit v3 Microsoft Testing Platform módban
- DB: PostgreSQL 18 (`postgres:18` image Testcontainersszel; a Docker Compose ugyanezt az image-et kapja a walking skeleton S5 szakaszában, [ADR-0005](../02_architecture/adr/0005-postgresql-ef-core-migraciok.md)); a teszteknél futó Docker szükséges
- Teszt adat: –

## Teszt suite-ek és futtatás

- Összes: `dotnet test` (a repo gyökeréből)
- Unit: `dotnet test --project tests/KamraApp.Unit.Tests`
- Integrációs: `dotnet test --project tests/KamraApp.Integration.Tests`
- Frontend (Vitest): `cd src/frontend && npm ci && npm test`
- E2E (Playwright, futó stack ellen): `cd tests/e2e && npm ci && npx playwright install chromium && npx playwright test` (`E2E_BASE_URL`, alapértelmezetten `http://localhost:5083`). A tesztelt Api-t emelt bejelentkezési limittel kell indítani (`RateLimiting__Auth__PermitLimit=1000`), különben az egymás utáni futások elérik az éles 10 kérés/perc limitet.

## Összesítés

| Kategória | Cél | Jelenlegi | Átmegy |
|---|---|---|---|
| Unit | ≥ 18 | 24 | 24 |
| Integrációs | ≥ 6 | 28 | 28 |
| Frontend (Vitest) | – | 7 | 7 |
| E2E | ≥ 6 | 2 (×2 nézet) | 2 |
| Negatív esetek | ≥ 5 | 31 | 31 |
| **Összesen** | **≥ 30** | **61** | **61** |

## Legutolsó futás eredménye

- Dátum: 2026-10-03
- Eredmény: PASS – .NET 52/52 (háromszor egymás után), Vitest 7/7 (egyszer), Playwright 4/4 futás – asztali Chrome és 360 px (háromszor egymás után, helyi stacken), helyi futás, háromszor egymás után (flaky-ellenőrzés)
- CI link: –

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

## Lefedetlen területek

- Minden üzleti modul – az implementáció még nem kezdődött el.

## Ismert hiányosságok

- A Domainben még csak a `Household`, az Applicationben csak a hibamodell és az auth-használati esetek vannak: az assembly-alapú tesztek csak a kódban ténylegesen használt hivatkozást látják, ezért a valódi bizonyító erejük a Domain és az Application kódjával együtt nő. A deklarált, de nem használt hivatkozást a projektfájl-alapú tesztek már most is kiszűrik (kézzel igazolva: egy ideiglenes `FrameworkReference` mindkét projektfájl-tesztet elbuktatta).
- Az [ADR-0006](../02_architecture/adr/0006-cookie-auth-identity.md) Verification-listájából két pont még nincs lefedve: a „más háztartás erőforrása 404” (S-2) az első háztartáshoz kötött végponttal (US-1) készül, a „session megmarad az api konténer újraindítása után” pontot a [V-14](../07_ai/verification_log.md) próbája igazolta (Docker Compose, kézi Playwright-szkript); a deploy runbookba az S6-ban kerül.
- A ux_flows „Lejárt munkamenet” üzenete („Biztonsági okból kiléptettünk…”) az első adatlekérő végponttal (US-1, készlet betöltése) kap valódi kiváltó eseményt; a szöveg már az üzenetkatalógusban van.
- Az üres készlet képernyő csak a „Még üres a kamrád.” mondatot mutatja; a ux_flows szövegének a mondatos bevitelre hívó folytatása a US-2-vel kerül be.
- Az e2e teszteket az S4-ben helyben futtattuk (Postgres-konténer + `dotnet run` + buildelt SPA); a CI-ban a Docker Compose stack ellen az S5-ben futnak.
- CI: [.github/workflows/ci.yml](../../.github/workflows/ci.yml) – `backend` (build, format, OpenAPI-diff, tesztek lefedettséggel, NuGet-sérülékenységek), `frontend` (generált típusok diffje, lint, Vitest, build, `npm audit`), `e2e` (Docker Compose stack + Playwright). Az első CI-futás linkje a push után kerül ide.
- Lefedettség: a `coverlet.MTP` Cobertura-riportot készít (`dotnet test --coverlet --coverlet-output-format cobertura`), a CI artifactként tölti fel. Küszöbérték (kapu) még nincs: a ≥ 80%-os Domain/Application-cél és a kizárások (generált migrációk, DTO-k) a test_strategy.md-ben dőlnek el.
- Élő AI API-hívás nem futhat CI-ban (`[Trait("Category","LiveAI")]` jelöléssel különítendő el).

## Flaky / instabil tesztek

- Nincs ismert flaky teszt. Az e2e első futtatásakor két teszt a bejelentkezési rate limit (10 kérés/perc/IP) miatt 429-et kapott, mert a futások egy percen belül követték egymást; ez a limit helyes működése, ezért a tesztelt Api emelt limittel fut (lásd fent), az éles alapértelmezés változatlan.
