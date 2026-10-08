# Tesztstratégia

A cél nem a teljes lefedettség, hanem a legfontosabb kockázatok regresszióvédelme: a készlet helyessége (nem negatív, nem vész el változás, nem von le kétszer), a háztartások adatelkülönítése és a determinisztikus szabályok. A tesztek állapota és eredménye: [test_report.md](test_report.md).

## Tesztpiramis

| Szint | Eszköz | Mit véd | Cél (DoD) |
|---|---|---|---|
| Unit | xUnit v3, FluentAssertions 7 | Domain- és Application-logika tisztán, adatbázis nélkül | ≥ 18 |
| Integrációs | xUnit v3 + Testcontainers (`postgres:18`), `WebApplicationFactory` | API + valódi PostgreSQL együtt: auth, adatelkülönítés, konkurencia, idempotencia, migrációk | ≥ 6 |
| E2E | Playwright (asztali Chrome + 360 px mobil) a Docker Compose stack ellen | A kritikus felhasználói utak a böngészőből | ≥ 6 |

Legalább 5 negatív teszt (érvénytelen bemenet, jogosultság, ütközés, hiányzó adat, külső hiba). A CI-ban nincs valódi LLM-hívás.

## Mit tesztelünk unit szinten, és mit integrációval?

- **Unit (tiszta logika):** mértékegység-átváltás és dimenzióellenőrzés (ADR-0002); becsült lejárat kategóriából; „hamarosan lejáró”; receptillesztés, rangsor, indoklás és hiányszámítás (US-3); adagskálázás és kerekítés, FEFO-felosztás, elégtelen készlet (US-4); bevásárlójavaslat-szabályok (US-5); relatív dátumok (US-2); a használati esetek validációja és hibaágai mockolt portokkal; rétegszabályok (ADR-0004).
- **Integrációs (DB + API):** minden végpont boldog útja és fő hibakódjai (ADR-0007); **adatelkülönítés két próba-háztartással:** a háztartáshoz kötött végpontok más háztartás erőforrására 404-et adnak (QA-1, paraméterezett teszt); **konkurencia és idempotencia** ([ADR-0008](../02_architecture/adr/0008-konkurencia-es-idempotencia.md) Verification: elavult verzió → 409, két egyidejű főzés, ismételt és egyidejű `requestId`, egyidejű bevásárlólista-felvétel); a napló-invariáns (`Quantity` = a `Delta`-k összege); a seed-adat konzisztenciája (ADR-0003, ADR-0005).

## Kritikus e2e utak

1. Regisztráció → bejelentkezés → üres készlet (megvan).
2. Készlettétel rögzítése → megjelenik, a becsült lejárat jelölve (US-1).
3. Ajánlás → főzés megerősítése → helyes maradék a készletben (US-3, US-4) – a fő flow.
4. Elfogyott hozzávaló → bevásárlójavaslat → elfogadás → a listán (US-5).
5. A 2. és a 3. lépcsőben: egy mondatos bevitel mockolt LLM-mel, chat-kérdés.

## Mock- és stub-stratégia

- **LLM:** az Application rétegben definiált interfész mögött; unit és integrációs tesztben fake implementáció, e2e-ben determinisztikus fake szolgáltató. Kiesés és sémahiba szimulálva (QA-3).
- **Adatbázis:** nincs in-memory provider; az integrációs teszt valódi PostgreSQL-t használ (Testcontainers), mert a konkurencia, az egyedi kényszerek és a migrációk csak így ellenőrizhetők.
- **Idő:** `TimeProvider` (a „hamarosan lejáró” és a becsült lejárat rögzített nappal tesztelhető).
- **Párhuzamosság:** az egyidejű kérések `Task.WhenAll`-lal futnak; a „másik fül” módosítását közvetlen SQL szimulálja.

## CI-kapuk

A `develop` és a `main` ágra csak zöld CI-vel lehet mergelni ([ci.yml](../../.github/workflows/ci.yml), branch-védelem):

- `secrets`: gitleaks a teljes historyra.
- `backend`: build (figyelmeztetés = hiba), `dotnet format`, OpenAPI contract-diff, unit- és integrációs tesztek, **lefedettségi kapu: Domain + Application sorlefedettség ≥ 80%** a unit tesztprojekten (`coverlet.MTP`: `--coverlet-include "[KamraApp.Domain]*" --coverlet-include "[KamraApp.Application]*" --coverlet-threshold 80 --coverlet-threshold-type line`, [V-20](../07_ai/verification_log.md)), NuGet-sérülékenységi vizsgálat.
- `frontend`: generált típusok diffje, lint, Vitest, build, `npm audit`.
- `e2e`: Docker Compose stack + Playwright.

Az OpenAPI contract-diff kapu, nem teszteset: a teszt-minimumokba nem számít bele. A többi réteg lefedettsége riportolva, küszöb nélkül.

## Futtatás

| Mit | Parancs |
|---|---|
| Minden .NET-teszt (Docker kell) | `dotnet test` |
| Unit | `dotnet test --project tests/KamraApp.Unit.Tests` |
| Integrációs | `dotnet test --project tests/KamraApp.Integration.Tests` |
| Frontend | `cd src/frontend && npm test` |
| E2E | `cd tests/e2e && npx playwright test` (futó stack ellen, lásd [test_report.md](test_report.md)) |

## Ismert korlátok

- Az LLM valódi pontosságát nem az automatizált tesztek, hanem a 2. lépcső PoC-mérése méri (ADR-0009).
- A lefedettségi kapu csak a unit tesztprojekt futásán mér; az integrációs tesztek által bejárt sorok nem számítanak bele.
