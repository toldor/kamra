# Changelog

A kiadások a [Semantic Versioning](https://semver.org/) szerint számozódnak; a `1.0.0` előtti változatok fejlesztési mérföldkövek. Minden kiadás git taggel jelölt.

## [0.4.0] – 2026-11-01 – Új felület

A fejlesztő által választott „Piaci tábla” design (a „Kamra stílusirányok” vászon C2 és C3 táblái) az egész alkalmazásban. A működés és az API nem változott. Részletek: [ADR-0013](../02_architecture/adr/0013-vizualis-rendszer.md), [P-24](../07_ai/prompt_log.md), [V-63 – V-65](../07_ai/verification_log.md).

### Hozzáadva
- Saját vizuális rendszer: színek, betűk és térközök CSS-változóként; saját tárhelyű betűk (Zilla Slab, IBM Plex Sans, OFL 1.1) a magyar ő és ű betűvel.
- Asztali elrendezés: fejsáv a menüvel, két oszlop (a szűrők és az űrlapok mellett a lista); 760 px alatt egymás alatt.
- Kontrasztteszt a színekre; e2e a betűkre, az elrendezésre és két javított hibára (hosszú nevű minimumszint, a fejsáv hover-kontrasztja).

### Módosítva
- A Készlet oldal címe „Készlet” (a „Kamra” a fejsáv logója), a lista szakasza „Tételek”, a kereső az „Új tétel” fölött.
- A Receptek oldalon a gomb felirata „Megnyitás”.

## [0.3.1] – 2026-10-31 – Az UAT előtti javítások

A fejlesztő UAT előtti kipróbálásából: érthetőbb feliratok és kereső. A backend és az API nem változott. Részletek: [ADR-0012](../02_architecture/adr/0012-bongeszos-kereses.md), [P-23](../07_ai/prompt_log.md), [V-61](../07_ai/verification_log.md), [V-62](../07_ai/verification_log.md).

### Hozzáadva
- Kereső a Receptek oldalon (a recept nevében és hozzávalóiban) és a Bevásárlólista oldalon (a tétel nevében), a készletkereső szabályával.
- A Készlet oldalon tájékoztató: a minimumszint a Bevásárlólista oldalon állítható be, linkkel.

### Módosítva
- „Megfőztem” → „Recept megtekintése”; „Bevásárlólistára” → „Hiány a bevásárlólistára”; a főzés oldalon „Levonás (lejárat – mennyiség): …”.

## [0.3.0] – 2026-10-24 – Bevásárlólista, saját recept és az első dokumentált telepítés

Az 1. lépcső okt. 22-i mérföldköve: a készlettől a főzésen át a bevásárlólistáig végig működő folyamat, saját recepttel, 20 induló recepttel, és egy tiszta környezetben kipróbált telepítési leírás. Részletek és bizonyítékok: [test_report.md](../04_quality/test_report.md) (P11–P20), [manual_tests.md](../04_quality/manual_tests.md), [P-16 – P-20](../07_ai/prompt_log.md).

### Hozzáadva
- Bevásárlólista (US-5): kézi felvétel mennyiséggel vagy anélkül, összevonás azonos hozzávalónál, kipipálás, törlés (P12, P16).
- Bevásárlójavaslat indokkal és mennyiséggel: elfogyott tételből és a minimumszint alá csökkent készletből; „Nem kell” elutasítás, amelyet új tétel felold (P14, P15, P19); egyidejű fogyásnál pontosan egy javaslat ([V-35](../07_ai/verification_log.md), P20).
- A recept és a főzés bekötése: „Bevásárlólistára” a hiányból az ajánlásban és a főzés előnézetében (P11, P13); a közben fogyott készlet kiemelése; összegző mondat a főzés után keletkezett javaslatokról.
- Saját recept (US-3): létrehozás a háztartás saját hozzávalójával is, piszkozattal; bármely recept archiválása a háztartásban (kikerül az ajánlásból, új főzése `RECIPE_ARCHIVED`) (P17, P18).
- Saját hozzávaló (`POST /ingredients`), névütközés-ellenőrzéssel ([ADR-0003](../02_architecture/adr/0003-kanonikus-hozzavalo-lista.md)).
- Bővített induló adatok: 20 saját szövegű recept és 157 hozzávaló ([V-43](../07_ai/verification_log.md)).
- Telepítési és üzemeltetési leírás ([deploy_runbook.md](../05_security_ops/deploy_runbook.md)): mentés, frissítés, visszaállás, három incidens; tiszta környezetben kipróbálva, a klóntól a `/health`-ig 1 perc 26 másodperc ([V-52](../07_ai/verification_log.md), MT-02).
- Minőségi és biztonsági dokumentáció: kézi tesztjegyzőkönyv, UAT-terv, megfeleltetési táblázat, fenyegetésmodell, adatkezelés és licencek ([V-53](../07_ai/verification_log.md)), C4 komponensnézet.
- Ellenséges integrációs tesztek a bevásárlólistához, a saját recepthez és az archiváláshoz.

### Módosítva
- Az adatbázis portja csak a gépről érhető el (`127.0.0.1`).
- A projekt licence MIT ([LICENSE](../../LICENSE)).
- Vite 8.3.5 biztonsági frissítés ([V-51](../07_ai/verification_log.md)).
- A hibamodell hivatkozása RFC 9457, amely az RFC 7807-et felváltotta; a formátum nem változott ([V-56](../07_ai/verification_log.md)).

### Ismert korlátok
- A kipipált bevásárlólista-tételből nincs automatikus készletbevitel.
- Metrikák nincsenek (CAP-10 *Partial*); a felhasználói kipróbálás (UAT) okt. 26-án indul, a visszaállás próbája (MT-04) a kiadás után.
- Ha egy recept archiválása és első főzése pontosan egyszerre fut, a főzés még átmehet ([api.md](../03_design/api.md)).
- Az ajánlás lapozás nélkül adja vissza az összes illeszkedő receptet.

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
