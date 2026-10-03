# Kamra – Smart Pantry & Recipe Manager

## A projektről

A **Kamra** egy webalapú háztartási készlet- és receptkezelő rendszer, amelynek egyik célja az otthoni élelmiszer-pazarlás csökkentése.

A rendszer tervezett működése:

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

## Jelenlegi állapot – 2026. október 2.

A repository jelenleg **tervezési és dokumentációs fázisban van**. Az alkalmazás fő funkciói még nincsenek implementálva.

### Elkészült vagy folyamatban lévő részek

- projektvízió és probléma-meghatározás;
- fogalmi szótár és domainnyelv;
- MVP-hatókör és felhasználói történetek;
- elfogadási kritériumok és Definition of Done;
- képességtérkép és fejlesztési roadmap;
- fő felhasználói folyamatok terve;
- versenytárselemzés;
- adatmodell-, API- és hibakezelési vázlat;
- Clean Architecture- és PostgreSQL/EF Core-döntési tervezetek;
- AI-használati, prompt- és ellenőrzési dokumentáció;
- `.env.example` konfigurációs sablon.

### Jelenleg hiányzó fő részek

- működő backend és REST API;
- működő React frontend;
- PostgreSQL-adatbázis és EF Core migrációk;
- regisztráció és bejelentkezés;
- készletkezelés;
- receptkezelés és determinisztikus receptajánlás;
- főzés utáni készletcsökkentés;
- bevásárlójavaslatok és bevásárlólista;
- AI-alapú természetes nyelvű bevitel;
- MCP-alapú, csak olvasási célú chatasszisztens;
- automatizált tesztek és CI quality gate-ek;
- teljes futtatási és telepítési útmutató.

Az aktuális képességállapot részletesen a [capability mapben](https://github.com/toldor/kamra/blob/develop/docs/01_product/capability_map.md) található.

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

## Tervezett technológiai stack

| Terület | Technológia |
|---|---|
| Backend | C# / .NET, ASP.NET Core Web API |
| Architektúra | Clean Architecture |
| Adatbázis | PostgreSQL |
| Adatkezelés | Entity Framework Core, migrációk |
| Frontend | React, TypeScript, Vite |
| AI | ChatCompletion API alkalmazási rétegben definiált interfész mögött |
| Chat-integráció | Külön MCP-szerver, közös Application use case-ekkel |
| Tesztelés | xUnit, FluentAssertions, Testcontainers, Vitest, Playwright |
| Környezet | Docker Compose, GitHub Actions |

## Tervezett architektúra

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

### Jelenlegi állapot

Az alkalmazás futtatható verziója még nincs elkészítve, ezért a projekt jelenlegi állapotában nincs működő `docker compose up`, backend-, frontend- vagy tesztfuttatási útmutató.

A futtatási útmutató akkor kerül véglegesítésre, amikor elkészül az első működő walking skeleton, és a parancsokat tiszta környezetben is ellenőriztem.

### Tervezett előkészítés

Ha a fejlesztési környezet elkészült:

1. a repository gyökerében létre kell hozni a `.env` fájlt a `.env.example` alapján;
2. az érzékeny értékeket csak lokálisan, a `.env` fájlban szabad megadni;
3. a `.env` fájlt nem szabad commitolni;
4. a projekt indítását és az adatbázis-migrációkat a későbbi, ellenőrzött futtatási útmutató szerint kell végrehajtani.

## Fejlesztési roadmap

| Szakasz | Tervezett tartalom | Állapot |
|---|---|---|
| Alapok | Repository, CI, architektúra, autentikáció előkészítése | Folyamatban |
| 1. lépcső | Determinisztikus készlet-, recept-, főzés- és bevásárlólista-funkciók | Tervezett |
| 2. lépcső | AI-alapú egy mondatos készletbevitel | Tervezett |
| 3. lépcső | Olvasási célú chat és mérési funkciók | Tervezett |
| Lezárás | Tesztek, hardening, dokumentáció, demó és szakdolgozati anyag | Tervezett |

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
