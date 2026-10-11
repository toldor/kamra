# Changelog

A kiadások a [Semantic Versioning](https://semver.org/) szerint számozódnak; a `1.0.0` előtti változatok fejlesztési mérföldkövek. Minden kiadás git taggel jelölt.

## [0.2.0] – 2026-10-11 – Készlet, ajánlás és főzés

Az 1. lépcső okt. 16-i mérföldköve: működő készletkezelés és ajánlás–főzés folyamat öt saját recepttel, kézzel ellenőrzött mennyiségekkel. Részletek és bizonyítékok: [test_report.md](../04_quality/test_report.md) (P1–P10), [P-15](../07_ai/prompt_log.md).

### Hozzáadva
- Készlet (US-1): rögzítés, szerkesztés, csökkentés okkal, törlés; becsült lejárat 14 kategóriából ([V-18](../07_ai/verification_log.md)); keresés, kategória- és „hamarosan lejáró” szűrő; alapegységű tárolás ([ADR-0002](../02_architecture/adr/0002-fix-atvalthato-mertekegysegek.md)); csak hozzáfűzhető készletmozgás-napló; optimista zárolás `xmin`-nel ([ADR-0008](../02_architecture/adr/0008-konkurencia-es-idempotencia.md)).
- Induló hozzávaló-lista (36) és 5 saját szövegű recept seed-adatként ([ADR-0003](../02_architecture/adr/0003-kanonikus-hozzavalo-lista.md), [ADR-0005](../02_architecture/adr/0005-postgresql-ef-core-migraciok.md)).
- „Mit főzzek?” (US-3): elkészíthető és majdnem elkészíthető receptek indoklással és „van / kell” hiánnyal; determinisztikus rangsor magyar névsorral.
- Főzés (US-4 alapút): előnézet adagszám-skálázással és FEFO-felosztással; szerkeszthető felhasznált mennyiség, élő hiány; idempotens megerősítés kérésazonosítóval, `INSUFFICIENT_STOCK`, konkurens főzések kezelése; összegzés az elfogyott hozzávalókkal.
- Felület: navigáció, hash-alapú útvonalak, fókuszkezelés, 360 px-es nézet.
- Scalar API-felület fejlesztői környezetben ([V-27](../07_ai/verification_log.md)).
- Ellenséges integrációs tesztek a készlethez és a főzéshez; kézzel számolt ellenőrző példák (P1–P10).

### Ismert korlátok
- Okt. 23-ig: bevásárlólista és bevásárlójavaslat (US-5); a főzésnél a hiány bevásárlólistára tétele és a közben változott készlet kiemelése; saját recept és archiválás; saját hozzávaló.
- Az ajánlás lapozás nélkül adja vissza az összes illeszkedő receptet.
- A „ma” a budapesti naptári nap ([V-23](../07_ai/verification_log.md)).

## [0.1.0] – 2026-10-04 – Walking skeleton

Végponttól végpontig futó rendszer üzleti funkció nélkül. Részletek és bizonyítékok: [test_report.md](../04_quality/test_report.md), [P-13](../07_ai/prompt_log.md).

### Hozzáadva
- Clean Architecture rétegek (Domain, Application, Infrastructure, Api) rétegszabály-tesztekkel ([ADR-0004](../02_architecture/adr/0004-clean-architecture-retegek.md)).
- PostgreSQL 18 EF Core 10-zel, első migráció (`InitialIdentityAndHousehold`), külön migrator-szolgáltatás ([ADR-0005](../02_architecture/adr/0005-postgresql-ef-core-migraciok.md)).
- Regisztráció, bejelentkezés, kijelentkezés cookie-alapú sessionnel (ASP.NET Core Identity): antiforgery, fiókzárolás, IP-alapú rate limit, kijelentkezéskor minden session visszavonása ([ADR-0006](../02_architecture/adr/0006-cookie-auth-identity.md)).
- REST API `/api/v1` alatt, RFC 7807 ProblemDetails stabil hibakódokkal és magyar üzenetekkel, build közben generált OpenAPI-leírás ([ADR-0007](../02_architecture/adr/0007-rest-api-hibamodell.md)).
- Strukturált JSON-naplózás Seriloggal, szerver által generált `correlationId`, `/health` adatbázis-ellenőrzéssel ([ADR-0011](../02_architecture/adr/0011-serilog-strukturalt-naplozas.md)).
- React 19 + TypeScript SPA: bejelentkezés, regisztráció, üres készlet; az Api szolgálja ki ugyanarról az originről.
- Docker Compose stack (`db`, `migrator`, `api`), Data Protection kulcsok volume-on.
- GitHub Actions CI: secret-szkennelés, build, formázás, lint, unit-, integrációs (Testcontainers) és e2e (Playwright) tesztek, OpenAPI-szerződés, függőség-sérülékenység; dependabot; branch-védelem a `develop` és `main` ágon.

### Ismert korlátok
- Nincs még üzleti funkció (készlet, receptek, főzés, bevásárlólista, AI-bevitel, chat).
- A háztartások közötti adatelkülönítés tesztje (S-2) az első háztartáshoz kötött végponttal (US-1) készül.
- A Data Protection kulcsok titkosítatlanul vannak a volume-on ([V-14](../07_ai/verification_log.md)).
