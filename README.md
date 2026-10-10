# Kamra – Smart Pantry & Recipe Manager

[![CI](https://github.com/toldor/kamra/actions/workflows/ci.yml/badge.svg)](https://github.com/toldor/kamra/actions/workflows/ci.yml)

## A projektről

A **Kamra** egy webalapú háztartási készlet- és receptkezelő rendszer, amelynek egyik célja az otthoni élelmiszer-pazarlás csökkentése.

A rendszer tervezett teljes működése (hogy ebből mi készült el, azt a „Jelenlegi állapot” szakasz mutatja):

- a felhasználó nyilvántarthatja az otthoni készlettételeit;
- kezelheti a mennyiségeket, mértékegységeket és lejárati dátumokat;
- a hiányzó lejárati dátumot a rendszer kategória alapján megbecsülheti;
- a rendszer megmutathatja, hogy a rendelkezésre álló készletből milyen receptek készíthetők;
- a hamarosan lejáró hozzávalókat használó receptek előrébb kerülhetnek az ajánlásban;
- főzés után a felhasznált mennyiségek automatikusan levonhatók a készletből;
- elfogyott vagy alacsony készletű hozzávalókhoz bevásárlójavaslat készülhet;
- a későbbi fejlesztési lépcsőkben természetes nyelvű készletbevitel és olvasási célú chatasszisztens is megjelenhet.

## Szakdolgozati kontextus

**Szakdolgozat címe:** Intelligens kamra- és receptkezelő rendszer

**Készítő:** Toldi Dorottya  
**Képzés:** Gazdaságinformatikus BSc  
**Intézmény:** Szegedi Tudományegyetem

## Probléma és cél

Az otthoni élelmiszerkészlet követése gyakran pontatlan: a felhasználók nem mindig tudják, mi van otthon, mi jár le hamarosan, illetve mit lehetne elkészíteni a rendelkezésre álló hozzávalókból. Ez felesleges vásárláshoz és élelmiszer-pazarláshoz vezethet.

A projekt célja egy olyan mérnökileg ellenőrizhető rendszer megtervezése és megvalósítása, amely:

1. átláthatóvá teszi a háztartási készletet;
2. támogatja a lejárathoz közeli tételek felhasználását;
3. determinisztikus szabályokkal ajánl recepteket;
4. főzés után következetesen frissíti a készletet;
5. az AI-t csak ellenőrzött, validált és felhasználói jóváhagyással működő folyamatokban használja.

## Jelenlegi állapot – 2026. október 8.

Elkészült a **walking skeleton**: a teljes rendszer végponttól végpontig fut egy vékony szeleten, üzleti funkció nélkül.

- regisztráció, bejelentkezés, kijelentkezés (cookie-alapú session ASP.NET Core Identity-vel, antiforgery, fiókzárolás, rate limit);
- üres készlet-képernyő a React SPA-ban (asztali és 360 px-es mobil nézet);
- PostgreSQL EF Core migrációkkal, külön migrator-szolgáltatással;
- egységes hibamodell (RFC 7807 ProblemDetails, magyar üzenetek), strukturált JSON-napló `correlationId`-vel, `/health`;
- Docker Compose stack, GitHub Actions CI (build, formázás, lint, unit-, integrációs és e2e tesztek, OpenAPI-szerződés, sérülékenység- és secret-szkennelés).

Még **nincs kész**: készletkezelés, receptek és ajánlás, főzés, bevásárlólista, AI-alapú bevitel, chatasszisztens. Az aktuális képességállapot: [capability map](docs/01_product/capability_map.md); a tesztek állapota: [test report](docs/04_quality/test_report.md).

Következő mérföldkő: **okt. 16.** – készletkezelés, valamint ajánlás és főzés 3–5 recepttel. A teljes ütemezés: [scope_contract.md](docs/01_product/scope_contract.md) (3. Korlátok / Idő).

## Tervezett MVP-lépcsők

### 1. Determinisztikus mag

- készlettételek rögzítése, szerkesztése, törlése és áttekintése;
- lejárati dátumok és becsült lejáratok kezelése;
- receptek és hozzávalók kezelése;
- receptillesztés a jelenlegi készlethez;
- hamarosan lejáró tételeket előnyben részesítő ajánlási sorrend;
- főzés és FEFO-alapú készletcsökkentés;
- bevásárlójavaslatok és bevásárlólista;
- készletmozgások ok szerinti naplózása.

### 2. AI-alapú egy mondatos bevitel

A felhasználó természetes nyelvű mondatot írhat be, amelyből az AI strukturált **tételjavaslatokat** készít. A javaslatok nem kerülnek automatikusan az adatbázisba: a felhasználó azokat ellenőrizheti, módosíthatja és jóváhagyhatja.

### 3. Olvasási célú chatasszisztens

A chatasszisztens a tervek szerint csak olvasási célú MCP-eszközöket használhat:

- készlet lekérdezése;
- hamarosan lejáró tételek lekérdezése;
- receptajánlás lekérdezése;
- bevásárlólista megtekintése.

A chat nem módosíthat közvetlenül adatot.

## Technológiai stack

| Terület | Technológia |
|---|---|
| Backend | C# / .NET, ASP.NET Core Web API |
| Architektúra | Clean Architecture |
| Adatbázis | PostgreSQL |
| Adatkezelés | Entity Framework Core, migrációk |
| Frontend | React, TypeScript, Vite |
| AI (tervezett, 2. lépcső) | ChatCompletion API alkalmazási rétegben definiált interfész mögött |
| Chat-integráció (tervezett, 3. lépcső) | MCP-eszközök közös Application use case-ekkel; a felépítésről az ADR-0010 dönt |
| Tesztelés | xUnit v3, FluentAssertions 7, Testcontainers, Vitest, Playwright |
| Környezet | Docker Compose, GitHub Actions |

## Architektúra

Megvalósult (v0.1.0): React-frontend, Web API, Application, Domain, Infrastructure, PostgreSQL. Tervezett: LLM-adapter (2. lépcső) és MCP-szerver (3. lépcső).

```text
React + TypeScript frontend
             |
             v
ASP.NET Core Web API
             |
             v
Application use cases
       /             \
      v               v
Domain        Infrastructure
              |       |
              v       v
          PostgreSQL  LLM adapter

MCP server
     |
     v
Application use cases
```

Az API és az MCP-szerver ugyanazokat az Application-rétegbeli use case-eket használja. Az MCP-eszközök nem tartalmazhatnak külön üzleti logikát, és csak a bejelentkezett felhasználó háztartásának adataihoz férhetnek hozzá.

## Dokumentáció

A teljes dokumentáció a repository `docs` könyvtárában található.

- [Dokumentációs index](https://github.com/toldor/kamra/blob/develop/docs/00_index.md)
- [Projektvízió](https://github.com/toldor/kamra/blob/develop/docs/01_product/vision.md)
- [Hatókör és MVP](https://github.com/toldor/kamra/blob/develop/docs/01_product/scope_contract.md)
- [Képességtérkép](https://github.com/toldor/kamra/blob/develop/docs/01_product/capability_map.md)
- [Felhasználói folyamatok](https://github.com/toldor/kamra/blob/develop/docs/01_product/ux_flows.md)
- [Versenytárselemzés](https://github.com/toldor/kamra/blob/develop/docs/01_product/competitor_analysis.md)
- [Minőségi attribútumok](https://github.com/toldor/kamra/blob/develop/docs/02_architecture/quality_attributes.md)
- [Adatmodell](https://github.com/toldor/kamra/blob/develop/docs/03_design/data_model.md)
- [API-terv](https://github.com/toldor/kamra/blob/develop/docs/03_design/api.md)
- [Hibakezelés](https://github.com/toldor/kamra/blob/develop/docs/03_design/error_handling.md)
- [MCP-eszközök terve](https://github.com/toldor/kamra/blob/develop/docs/03_design/mcp_tools.md)
- [AI-manifeszt](https://github.com/toldor/kamra/blob/develop/docs/07_ai/ai_manifest.md)
- [Promptnapló](https://github.com/toldor/kamra/blob/develop/docs/07_ai/prompt_log.md)
- [Ellenőrzési napló](https://github.com/toldor/kamra/blob/develop/docs/07_ai/verification_log.md)

## Futtatás

### Gyors indítás (Docker Compose)

Előfeltétel: [Docker Desktop](https://www.docker.com/products/docker-desktop/) (vagy Docker Engine + Compose v2), futó állapotban. Más nem kell.

```bash
git clone https://github.com/toldor/kamra.git
cd kamra
cp .env.example .env        # Windows PowerShell: Copy-Item .env.example .env
docker compose up --build
```

Ezután nyisd meg: **http://localhost:8080** → *Regisztrálj* → e-mail-cím és legalább 15 karakteres jelszó → az üres készlet-képernyő jelenik meg.

- Az első indítás (image-ek letöltése és buildelése) friss gépen mért ideje: lásd [V-16](docs/07_ai/verification_log.md) (kb. 2 perc a `/health`-ig, cache nélküli builddel, az alap-image-ek letöltése nélkül); a további indítások gyorsabbak.
- A `docker compose up` sorrendben indítja a szolgáltatásokat: `db` (PostgreSQL 18) → `migrator` (EF Core migrációk, egyszer fut le) → `api` (REST API és a React SPA ugyanarról a címről).
- Állapot: `curl http://localhost:8080/health` → `Healthy`. Napló: `docker compose logs api`.
- Leállítás: `docker compose down` (az adatok megmaradnak); minden adat törlése: `docker compose down -v`.
- A `.env` csak a gépeden létezik, soha nem kerül a repóba. Helyi futtatáshoz a `.env.example` helykitöltő értékei elegendők; más környezetben cseréld le a jelszót.

### Környezeti változók

| Változó | Jelentés | Alapérték (`.env.example`) |
|---|---|---|
| `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD` | Az adatbázis neve, felhasználója és jelszava (a Compose ebből állítja össze a connection stringet) | `pantry`, `pantry_user`, `change_me` |
| `POSTGRES_PORT` | Az adatbázis portja a gépen | `5432` |
| `API_PORT` | Az alkalmazás portja a gépen | `8080` |
| `ConnectionStrings__Default` | Csak a Compose nélküli, helyi `dotnet run`-hoz | `Host=localhost;…` |
| `DataProtection__KeysPath` | A cookie-kulcsok helye (Compose-ban a `dpkeys` volume, így újraindítás után is bejelentkezve maradsz) | Compose-ban `/keys` |
| `RateLimiting__Auth__PermitLimit`, `RateLimiting__Auth__WindowSeconds` | Bejelentkezés és regisztráció: kérések száma IP-címenként egy időablakban | `10`, `60` |

Hiányzó kötelező beállítással az alkalmazás induláskor hibával leáll (fail-fast).

### Fejlesztés és tesztek

| Feladat | Parancs |
|---|---|
| Backend build (figyelmeztetés = hiba) | `dotnet build -warnaserror` |
| Formázás ellenőrzése | `dotnet format --verify-no-changes` |
| Backend tesztek (unit + integrációs, Dockerrel futó PostgreSQL-lel) | `dotnet test` |
| Lefedettség (Cobertura, `TestResults/`) | `dotnet test --coverlet --coverlet-output-format cobertura` |
| Csak az adatbázis indítása helyi fejlesztéshez | `docker compose up -d db` |
| Migrációk alkalmazása helyben | `dotnet ef database update -p src/backend/KamraApp.Infrastructure -s src/backend/KamraApp.Api` |
| Backend helyben | `dotnet run --project src/backend/KamraApp.Api` (http://localhost:5083) |
| API-leírás fejlesztésben (Scalar, csak `Development`) | http://localhost:5083/scalar – módosító kéréshez előbb kérj tokent a `GET /api/v1/auth/antiforgery`-vel, és add meg `X-XSRF-TOKEN` fejlécként |
| Frontend fejlesztői szerver (az `/api`-t a backendre proxyzza) | `cd src/frontend && npm ci && npm run dev` |
| Frontend lint, teszt, build | `cd src/frontend && npm run lint && npm test && npm run build` |
| Frontend API-típusok újragenerálása az `openapi.json`-ból | `cd src/frontend && npm run gen:api` |
| E2E (futó stack ellen, például Compose: `E2E_BASE_URL=http://localhost:8080`) | `cd tests/e2e && npm ci && npx playwright install chromium && npx playwright test` |

Az e2e tesztekhez a tesztelt Api-t emelt bejelentkezési limittel érdemes indítani (`RateLimiting__Auth__PermitLimit=1000`), különben a gyorsan ismételt futások elérik az alapértelmezett 10 kérés/perc limitet.

### Gyakori hibák

| Tünet | Megoldás |
|---|---|
| `Set POSTGRES_PASSWORD in .env` | Hiányzik a `.env`: `cp .env.example .env` |
| `port is already allocated` (5432 vagy 8080) | Más program használja a portot: állítsd át a `.env`-ben a `POSTGRES_PORT`-ot vagy az `API_PORT`-ot |
| `Cannot connect to the Docker daemon` | Indítsd el a Docker Desktopot, és várd meg, amíg fut |
| A böngésző nem tart meg bejelentkezést, ha nem `localhost`-on nyitod meg | A biztonságos (`Secure`) cookie-t a böngészők HTTP-n csak `localhost`-ra engedik; más gépnévhez vagy IP-címhez HTTPS kell |
| Az integrációs tesztek nem indulnak (`Docker is either not running…`) | A tesztek Testcontainersszel PostgreSQL-konténert indítanak: futó Docker kell |

## Fejlesztési roadmap

| Szakasz | Tervezett tartalom | Állapot |
|---|---|---|
| Alapok | Repository, CI, architektúra, walking skeleton (autentikáció, Docker Compose) | Kész (2026-10-04) |
| 1. lépcső | Determinisztikus készlet-, recept-, főzés- és bevásárlólista-funkciók | Folyamatban (cél: okt. 23.) |
| 2. lépcső | AI-alapú egy mondatos készletbevitel | Tervezett (cél: nov. 6.) |
| 3. lépcső | Olvasási célú chat és mérési funkciók | Tervezett (cél: nov. 20.) |
| Lezárás | Tesztek, hardening, dokumentáció, demó és szakdolgozati anyag | Tervezett (cél: dec. 4.) |

## Biztonsági és minőségi alapelvek

- A felhasználói adatokat kezelő végpontoknak tényleges autentikációt kell használniuk.
- A háztartások adatai egymástól el vannak különítve.
- Minden bemenetet validálni kell, mielőtt a domain- vagy adatbázisrétegbe kerül.
- Az AI-kimenetet séma és DTO-validáció után lehet csak felhasználni.
- Az AI nem adhat jogosultságot, és nem hajthat végre ellenőrizetlen írási műveletet.
- Tesztkörnyezetben nem fut valódi LLM-hívás; az LLM-et mockolni kell.
- Naplóba nem kerülhet személyes adat, prompt vagy API-kulcs.
- Titkok, valódi jelszavak és API-kulcsok nem kerülhetnek a repositoryba.

## Repository

https://github.com/toldor/kamra

## Licenc

A licenc a projekt jelenlegi állapotában még nincs véglegesítve.
