# Adatkezelés és licencek

## Adatkategóriák

| Adat | Személyes? | Miért kell | Hol |
|---|---|---|---|
| E-mail-cím | Igen | Bejelentkezés, egy fiók = egy háztartás | `AspNetUsers` |
| Jelszó | Igen (titok) | Bejelentkezés; csak az ASP.NET Core Identity hash-e tárolódik, a jelszó soha | `AspNetUsers` |
| A háztartás adatai: készlet, receptek, főzések, bevásárlólista, minimumszintek | Közvetve (étkezési szokások) | A szolgáltatás maga | az üzleti táblák, háztartás-azonosítóval |
| Napló | Nem: kérésazonosító, útvonal, státusz, időtartam, hibakód | Hibakeresés | a konténer kimenete |
| Cookie-kulcsok | Nem (titok) | A bejelentkezési süti titkosítása | `dpkeys` volume |

A tesztekben és a dokumentációban csak kitalált adat szerepel (`@example.com`).

## Adatáramlás

Böngésző → API (`/api/v1`, session-cookie) → PostgreSQL ugyanazon a gépen. Harmadik félhez jelenleg semmi nem megy. A tervezett MI-funkciók (US-2, US-6) a beírt szöveget és a válaszhoz szükséges készletadatot küldik egy LLM-szolgáltatónak, fiókadatot (e-mail, jelszó) soha ([scope_contract.md](../01_product/scope_contract.md), 3. Korlátok); a szolgáltató és az adatküldés részletei a megfelelő ADR-ben.

## Megőrzés és törlés

- Az adat a fiók törléséig megmarad. A felületen fióktörlés nincs (Ismert korlátok); kérésre az üzemeltető törli a felhasználót; ez láncban törli a háztartását és annak minden adatát (a háztartás a felhasználóra, a háztartáshoz kötött táblák a háztartásra hivatkoznak `ON DELETE CASCADE`-del, [data_model.md](../03_design/data_model.md)).
- A napló a konténer kimenete: a konténer újralétrehozásával törlődik.
- Az UAT után a próba-adatbázist a fejlesztő törli ([uat.md](../04_quality/uat.md)).

## Hozzáférés

- Egy felhasználó csak a saját háztartása adatát látja (QA-1, [threat_model.md](threat_model.md) T-4). Szerepkörök és admin felület nincsenek.
- Az üzemeltető (a telepítést futtató személy) az adatbázishoz a gépen fér hozzá.

## MI és adatok

Jelenleg nincs MI-hívás. A szabály a tervezett funkciókra: személyes adat (e-mail, jelszó) az LLM-hez nem kerül; a CI-ban és a tesztekben valódi LLM-hívás nincs (AGENTS.md 7.).

## Licencek

A projekt licence: MIT ([LICENSE](../../LICENSE)) (D2).

**Közvetlen függőségek** (a csomagok saját metaadatából, 2026-10-23, [V-53](../07_ai/verification_log.md)):

| Csomag | Verzió | Licenc | Hol |
|---|---|---|---|
| Microsoft.AspNetCore.Identity.EntityFrameworkCore, .OpenApi, .Mvc.Testing; Microsoft.EntityFrameworkCore.Design; Microsoft.Extensions.ApiDescription.Server | 10.0.12 | MIT | backend, tesztek |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.3 | PostgreSQL License | backend |
| Serilog.AspNetCore | 10.0.0 | Apache-2.0 | backend |
| Scalar.AspNetCore | 2.17.14 | MIT | backend (csak fejlesztésben) |
| xunit.v3 | 4.0.1 | Apache-2.0 | tesztek |
| FluentAssertions | 7.2.2 | Apache-2.0 (a 8-as verziótól kereskedelmi licenc, ezért a 7.x-en marad) | tesztek |
| Testcontainers.PostgreSql | 4.15.0 | MIT | tesztek |
| coverlet.MTP | 10.1.0 | MIT | tesztek |
| react, react-dom | 19.3.0 | MIT | frontend |
| vite, @vitejs/plugin-react, vitest, jsdom, @testing-library/react, @testing-library/dom, openapi-typescript, oxlint, @types/* | lásd `package-lock.json` | MIT | frontend (fejlesztés) |
| typescript | 5.9.3 | Apache-2.0 | frontend, e2e |
| @playwright/test | 1.63.0 | Apache-2.0 | e2e |

Mind megengedő licenc, egymással és az MIT-tel kompatibilis. A Docker-alapképek: `postgres:18` (PostgreSQL License), `mcr.microsoft.com/dotnet/*` (MIT), `node:24-alpine` (MIT).

**Adat és tartalom:** az induló receptek saját szövegűek ([V-43](../07_ai/verification_log.md)); a kategóriák eltarthatósági értékei a USDA FoodKeeper adatain alapulnak (CC0, [data_model.md](../03_design/data_model.md)).
