# Changelog

A kiadások a [Semantic Versioning](https://semver.org/) szerint számozódnak; a `1.0.0` előtti változatok fejlesztési mérföldkövek. Minden kiadás git taggel jelölt.

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
