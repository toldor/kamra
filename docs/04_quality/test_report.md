# Tesztjelentés

Frissítsd minden CI futás után, amelyik releváns változást hoz.
Cél: 30+ automatizált teszt (≥18 unit, ≥6 integrációs, ≥6 e2e/contract), ebből ≥5 negatív eset.

## Környezet

- OS: Windows 11 (fejlesztői gép)
- Runtime/SDK: .NET SDK 10.0.201 (`global.json`), xUnit v3 Microsoft Testing Platform módban
- DB: – (még nincs adatbázis-teszt; az integrációs tesztek `WebApplicationFactory`-val, adatbázis nélkül futnak)
- Teszt adat: –

## Teszt suite-ek és futtatás

- Összes: `dotnet test` (a repo gyökeréből)
- Unit: `dotnet test --project tests/KamraApp.Unit.Tests`
- Integrációs: `dotnet test --project tests/KamraApp.Integration.Tests`

## Összesítés

| Kategória | Cél | Jelenlegi | Átmegy |
|---|---|---|---|
| Unit | ≥ 18 | 16 | 16 |
| Integrációs | ≥ 6 | 7 | 7 |
| E2E | ≥ 6 | 0 | – |
| Negatív esetek | ≥ 5 | 4 | 4 |
| **Összesen** | **≥ 30** | **23** | **23** |

## Legutolsó futás eredménye

- Dátum: 2026-10-03
- Eredmény: PASS (23/23), helyi futás, háromszor egymás után (flaky-ellenőrzés)
- CI link: –

## Tesztelt modulok

| Modul | Típus | Leírás | Állapot |
|---|---|---|---|
| Rétegszabályok ([LayerRulesTests.cs](../../tests/KamraApp.Unit.Tests/LayerRulesTests.cs)) | Unit | A Domain és az Application assembly-hivatkozásai és projektfájl-hivatkozásai az [ADR-0004](../02_architecture/adr/0004-clean-architecture-retegek.md) szerint | ✅ 4/4 |
| Hibakategória → HTTP-státusz ([ErrorCategoryMappingTests.cs](../../tests/KamraApp.Unit.Tests/ErrorCategoryMappingTests.cs)) | Unit | Mind a 8 `ErrorCategory` az [ADR-0007](../02_architecture/adr/0007-rest-api-hibamodell.md) szerinti státuszra képeződik; a kód nélküli keretrendszer-státuszok alapértelmezett kódot kapnak | ✅ 12/12 |
| Hibakezelés, health, correlationId ([ErrorHandlingTests.cs](../../tests/KamraApp.Integration.Tests/ErrorHandlingTests.cs)) | Integrációs | `/health` 200; váratlan hiba 500 belső részlet nélkül (negatív); `AppException` → saját státusz, kód, cím; ismeretlen útvonal 404 ProblemDetails magyar címmel (negatív); rossz HTTP-metódus 405 (negatív); keretrendszer-elutasítás (`BadHttpRequestException`) megtartja a 4xx státuszt, `REQUEST_REJECTED`, belső részlet nélkül (negatív); a `correlationId` egyezik a fejléccel a 409-es és az 500-as úton is, CSP- és `nosniff`-fejléc | ✅ 7/7 |

## Lefedetlen területek

- Minden üzleti modul – az implementáció még nem kezdődött el.

## Ismert hiányosságok

- A Domain réteg még üres (az Applicationben csak az `AppException` van): az assembly-alapú tesztek csak a kódban ténylegesen használt hivatkozást látják, ezért a valódi bizonyító erejük a Domain és az Application kódjával együtt nő. A deklarált, de nem használt hivatkozást a projektfájl-alapú tesztek már most is kiszűrik (kézzel igazolva: egy ideiglenes `FrameworkReference` mindkét projektfájl-tesztet elbuktatta).
- Még nincs CI: a tesztek egyelőre csak helyben futnak; a CI a walking skeleton S5 szakaszában készül.
- Még nincs lefedettségmérés: a lefedettség-gyűjtő eszköz a CI-val együtt kerül be.
- Élő AI API-hívás nem futhat CI-ban (`[Trait("Category","LiveAI")]` jelöléssel különítendő el).

## Flaky / instabil tesztek

- Nincs ismert flaky teszt.
