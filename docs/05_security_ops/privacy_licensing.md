# Adatkezelés és licencek

## Adatkategóriák

| Adat | Személyes? | Miért kell | Hol |
|---|---|---|---|
| E-mail-cím | Igen | Bejelentkezés, egy fiók = egy háztartás | `AspNetUsers` |
| Jelszó | Igen (titok) | Bejelentkezés; csak az ASP.NET Core Identity hash-e tárolódik, a jelszó soha | `AspNetUsers` |
| A háztartás adatai: készlet, receptek, főzések, bevásárlólista, minimumszintek | Közvetve (étkezési szokások) | A szolgáltatás maga | az üzleti táblák, háztartás-azonosítóval |
| Tételjavaslatok: a mondat tételhez tartozó részei, a javasolt értékek, a beküldés és a döntés ideje | Közvetve (vásárlási szokások) | A mondatos bevitel és a G3 metrika; a teljes mondat nem tárolódik | `ItemProposals` |
| Napló | Nem: kérésazonosító, útvonal, státusz, időtartam, hibakód | Hibakeresés | a konténer kimenete |
| Cookie-kulcsok | Nem (titok) | A bejelentkezési süti titkosítása | `dpkeys` volume |

A tesztekben és a dokumentációban csak kitalált adat szerepel (`@example.com`).

## Adatáramlás

Böngésző → API (`/api/v1`, session-cookie) → PostgreSQL ugyanazon a gépen. A mondatos bevitel (US-2, csak bekapcsolt `Llm:Enabled` mellett) a beírt mondatot és a háztartás által látható hozzávalók nevét elküldi a Google Gemini API-nak ([ADR-0009](../02_architecture/adr/0009-llm-szolgaltato.md)); készlet- és fiókadatot nem. A chat (US-6, ugyanazzal a kapcsolóval) a kérdést, a legfeljebb 10 korábbi üzenetet és az MCP-eszközök eredményét küldi: a saját háztartás készletét, ajánlását és bevásárlólistáját (nevek, mennyiségek, lejáratok), azonosító és fiókadat nélkül ([ADR-0010](../02_architecture/adr/0010-mcp-integracio.md)); a beszélgetést a szerver nem tárolja. Más harmadik félhez semmi nem megy. A felület sem tölt be külső erőforrást: a betűk is az alkalmazás saját címéről jönnek ([ADR-0013](../02_architecture/adr/0013-vizualis-rendszer.md)).

## Megőrzés és törlés

- Az adat a fiók törléséig megmarad. A felületen fióktörlés nincs (Ismert korlátok); kérésre az üzemeltető törli a felhasználót; ez láncban törli a háztartását és annak minden adatát (a háztartás a felhasználóra, a háztartáshoz kötött táblák a háztartásra hivatkoznak `ON DELETE CASCADE`-del, [data_model.md](../03_design/data_model.md)).
- A napló a konténer kimenete: a konténer újralétrehozásával törlődik.
- Az UAT után a próba-adatbázist a fejlesztő törli ([uat.md](../04_quality/uat.md)).

## Hozzáférés

- Egy felhasználó csak a saját háztartása adatát látja (QA-1, [threat_model.md](threat_model.md) T-4). Szerepkörök és admin felület nincsenek.
- Az üzemeltető (a telepítést futtató személy) az adatbázishoz a gépen fér hozzá.

## MI és adatok

Szolgáltató: Google Gemini API (`gemini-3.1-flash-lite`, [ADR-0009](../02_architecture/adr/0009-llm-szolgaltato.md)).

- **Mi megy ki:** a mondat (legfeljebb 500 karakter) és a háztartás által látható hozzávalók neve. A chatnél a kérdés, az előzmény és a saját háztartás adatai az eszközeredményekből. E-mail, jelszó és azonosító egyiknél sem.
- **Napló:** a mondat, a kérdés, a válasz és az eszközeredmény nem kerül a naplóba ([observability.md](observability.md)).
- **Feltételek** ([Gemini API Additional Terms](https://ai.google.dev/gemini-api/terms)): az EGT-ben elérhetővé tett alkalmazás csak a fizetős szolgáltatást használhatja. A fizetős szolgáltatásnál a Google a promptokat és a válaszokat nem használja a termékei fejlesztésére, adatfeldolgozási kiegészítés (DPA) szerint kezeli, és csak visszaélés-felderítésre naplózza, korlátozott ideig.
- **Fizetős keret (bekapcsolva 2026-11-08):** Tier 1, előre fizetett 5 dollár, automatikus újratöltés nélkül, havi költéskorlát 5 dollárral. A kérések a fizetős ár szerint díjkötelesek, és csak pozitív egyenleg mellett futnak ([Billing](https://aistudio.google.com/docs/billing), [Pricing](https://aistudio.google.com/docs/pricing), 2026-11-08). Ha az egyenleg elfogy, a Gemini HTTP 402-t ad; az alkalmazás ezt újrapróbálás nélkül `LLM_UNAVAILABLE`-ként adja vissza, a kézi űrlap működik ([V-76](../07_ai/verification_log.md)).
- A CI-ban és a tesztekben valódi LLM-hívás nincs (AGENTS.md 7.): a tesztek hamis adaptert, az e2e egy LLM-csonkot használ.

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
| ModelContextProtocol.Core (+ Microsoft.Extensions.AI.Abstractions, Microsoft.Extensions.Logging.Abstractions) | 2.3.1 (10.11.0, 10.0.13) | Apache-2.0 (MIT, MIT) | backend (MCP, [ADR-0010](../02_architecture/adr/0010-mcp-integracio.md)) |
| react, react-dom | 19.3.0 | MIT | frontend |
| vite, @vitejs/plugin-react, vitest, jsdom, @testing-library/react, @testing-library/dom, openapi-typescript, oxlint, @types/* | lásd `package-lock.json` | MIT | frontend (fejlesztés) |
| typescript | 5.9.3 | Apache-2.0 | frontend, e2e |
| @playwright/test | 1.63.0 | Apache-2.0 | e2e |
| Zilla Slab 700, IBM Plex Sans (betűtípus, woff2, latin + latin-ext) | Google Fonts, 2026-10-31 (Zilla Slab v12, IBM Plex Sans v23) | SIL OFL 1.1 (`src/frontend/public/fonts/OFL-*.txt`) | frontend, saját tárhelyről |

Mind megengedő licenc, egymással és az MIT-tel kompatibilis. A US-2 nem hozott új függőséget: a Gemini-hívás `HttpClient`-tel megy, az e2e LLM-csonkja csak beépített Node-modulokat használ; a US-6 egyet: a hivatalos MCP C# SDK-t (Apache-2.0), a két MIT-licences átmeneti függőségével. A Docker-alapképek: `postgres:18` (PostgreSQL License), `mcr.microsoft.com/dotnet/*` (MIT), `node:24-alpine` (MIT).

**Adat és tartalom:** az induló receptek saját szövegűek ([V-43](../07_ai/verification_log.md)); a kategóriák eltarthatósági értékei a USDA FoodKeeper adatain alapulnak (CC0, [data_model.md](../03_design/data_model.md)).
