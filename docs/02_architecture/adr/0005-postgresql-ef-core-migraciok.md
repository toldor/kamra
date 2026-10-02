# 0005 - PostgreSQL EF Core-ral, külön migrator-szolgáltatással és GUID v7 azonosítókkal

Dátum: 2026-10-02
Státusz: Proposed

## Context

- **Probléma:** el kell dönteni, hogyan éri el a backend az adatbázist, hogyan változik és hogyan görgethető vissza a séma, hogyan kerül be az induló rendszeradat (kategóriák, kb. 150–250 hozzávaló, 20–40 recept), és milyen azonosítót kapnak a rekordok.
- **Kényszerek:** a PostgreSQL adott a témavezetői iránymutatás és a tématerv alapján. A rendszer tiszta gépen `docker compose up`-pal 15 percen belül induljon ([QA-7](../quality_attributes.md)); a deploy runbookban a migráció sorrendje és egy kipróbált rollback szerepeljen. Nincs összefűzött SQL (AGENTS.md 7. pont). A Domain nem függhet az adatelérési technológiától ([ADR-0004](0004-clean-architecture-retegek.md)). Kb. 200 óra fejlesztési idő. A verziók: [EF Core 10](https://learn.microsoft.com/en-us/ef/core/what-is-new/) (LTS, támogatás 2028-11-10-ig), [Npgsql EF Core 10](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL/10.0.0) (PostgreSQL License; [PostgreSQL 18 támogatással](https://www.npgsql.org/efcore/release-notes/10.0.html)), [PostgreSQL 18](https://endoflife.date/postgresql) (támogatás 2030-11-14-ig).
- **Érintett minőségi attribútumok:** QA-7 telepíthetőség, QA-1 adatelkülönítés, QA-5 teljesítmény, QA-6 tesztelhetőség.

## Decision

- Az Infrastructure EF Core 10-zel és az Npgsql 10 providerrel éri el a PostgreSQL 18-at; a migrációkat egy egyszer lefutó migrator-szolgáltatás alkalmazza EF migration bundle-lel, az induló hozzávalók és receptek JSON fájlokból `HasData`-n keresztül a migrációk részei, a kategóriák a Domain-kódban vannak, a technikai azonosító pedig a Domainben generált GUID v7.

## Alternatives

**Adatelérés**

1) **Dapper + kézzel írt SQL, külön migrációs eszközzel (pl. DbUp)** – előny: teljes kontroll az SQL fölött; hátrány: két új függőség, minden lekérdezés és leképezés kézzel, a rendelkezésre álló időben a legdrágább.
2) **EF Core írásra, Dapper olvasásra** – előny: az ajánlás-lekérdezés külön optimalizálható; hátrány: két adatelérési mód és plusz függőség egy még nem mért lekérdezés miatt.

**Migrációk alkalmazása**

3) **Automatikusan az Api indulásakor** – előny: a legkevesebb munka; hátrány: az Api-nak futás közben DDL-jog kell, hibás migrációnál az Api nem indul, a rollback nem különálló lépés, és két host esetén versenyhelyzet lehet.
4) **Idempotens SQL-szkript kézi vagy CI-s futtatással** – előny: reviewzható szkript; hátrány: kézi lépés, ami a 15 perces indítást veszélyezteti.

**Induló adatok**

5) **Saját idempotens seeder** – előny: kisebb migrációk; hátrány: saját kód és teszt kell, és mivel a bundle nem futtat saját kódot, a migrator külön konzolprogram lenne.
6) **Kézzel írt `HasData` C#-ban** – előny: nincs fájlbeolvasás; hátrány: 40 recept és 200 hozzávaló kódban olvashatatlan.
7) **Kategóriák adatbázistáblában** – előny: kód nélkül bővíthető; hátrány: a lista fix és a felhasználó nem bővíti, a becsült lejárat számolása pedig adatbázis nélkül nem tesztelhető.

**Azonosító**

8) **`bigint` identity** – előny: kompakt, olvasható; hátrány: sorszámként kitalálható, és a seed-kulcsok ütközhetnek a sorozattal.
9) **GUID v4** – előny: kitalálhatatlan; hátrány: rosszabb index-locality, a v7-tel szemben nincs előnye.
10) **Belső `bigint` + publikus GUID** – előny: kompakt belső kulcsok és join-ok; hátrány: táblánként két kulcs, és leképezés kell minden végponton és MCP-toolban; egy elfelejtett leképezés kiszivárogtatja a belső kulcsot, ami az S-2 kockázatát növeli.

## Consequences

- **Pozitív:**
  - A sémaváltozások verziózott migrációk. A LINQ-lekérdezések és a `FromSql` interpolált paraméterei paraméterezettek; `FromSqlRaw` összefűzött stringgel nem használható.
  - A `docker compose up` a migrációval együtt egy lépésben indít, az Api és az McpServer csak a sikeres migráció után indul (`depends_on: service_completed_successfully`), és futás közben nem kap DDL-jogot.
  - A rollback a bundle célmigrációval történő futtatása (`efbundle <megmaradó migráció> --connection …`), amely a későbbi migrációk `Down` lépéseit hajtja végre, a rendszeradat-változásokat is beleértve ([Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying)).
  - A becsült lejárat számolása adatbázis nélkül unit-tesztelhető.
  - Az azonosító nem kitalálható, ami csökkenti a támadási felületet; ez nem védelem, azt a háztartás szerinti szűrés és az S-2 teszt adja.
- **Negatív / kockázatok:**
  - Az EF rejtett lekérdezései (N+1) rontják a teljesítményt; ez a legvalószínűbb oka lenne, ha az S-3 célértéke (ajánlás p95 ≤ 300 ms) nem teljesülne.
  - A `Down` lépések adatvesztéssel járhatnak (például oszlop vagy tábla elvetésekor), ezért a rollbacket éles adat előtt ki kell próbálni, és adatvesztő `Down` lépés csak a runbookban jelölve kerülhet be.
  - A rendszeradat változása nagy migrációs fájlt ad.
  - A GUID v7 16 bájtos és logban nehezen olvasható.
  - A .NET `Guid.CreateVersion7()` ugyanazon ezredmásodpercen belül nem garantál sorrendet (az RFC 9562 6.2 monoton módszerét nem valósítja meg), így az index-locality csak várhatóan jobb a v4-nél; a várható írási terhelésnél ez elhanyagolható.
- **Figyelni kell a megvalósítás során:**
  - Az Npgsql főverziója egyezzen az EF Core főverziójával; a projekt alatt .NET 10 LTS-en maradunk (a .NET 11 STS).
  - A PostgreSQL főverziója rögzített, és ugyanaz az image fut a Docker Compose-ban és a Testcontainers-tesztekben.
  - Az azonosítót a Domain entitás generálja a létrehozáskor (`Guid.CreateVersion7()`, .NET 9+ BCL), így mentés előtt ismert; a tételjavaslat jóváhagyásának idempotenciáját viszont a tételjavaslat állapota adja, nem az azonosító.
  - **Seed-folyamat:** a hozzávalók és receptek az Infrastructure rétegben lévő JSON fájlokban vannak; a modellépítés determinisztikusan beolvassa és `HasData`-ként adja át őket; a JSON változása után új migráció készül (`dotnet ef migrations add`), és az alkalmazás csak a migráción keresztül kapja meg a változást.
  - **Azonosító-szabályok:** minden rendszer-rekordnak a JSON-ban rögzített `Id`-je és egyedi, olvasható `SeedKey`-je van; egyik sem változtatható meg a létrehozás után. A seed fájlok egymásra `SeedKey`-jel hivatkoznak, ezt a betöltés `Id`-re oldja fel, és ismeretlen `SeedKey`-nél a betöltés hibával leáll.
  - **Törlés:** rendszer-rekord nem törölhető a seedből, ha felhasználói adat hivatkozhat rá; ilyenkor visszavonásra jelölés kell (a pontos megoldás a [data_model.md](../../03_design/data_model.md)-ben).
  - A Domain az alaphozzávalókat `SeedKey`-jel azonosítja; az oszlopdefiníció a data_model.md-ben szerepel.
  - A migrációk leíró nevet kapnak; adatot törlő vagy átalakító migráció csak jóváhagyással készül (AGENTS.md 11. pont).
  - Lokális fejlesztésben a `dotnet ef database update` használható marad.

## Verification

- **Hogyan ellenőrizzük?**
  - Integrációs tesztek valódi PostgreSQL-lel (Testcontainers, a Compose-zal azonos image), amelyek a migrációkat a tesztadatbázison futtatják.
  - Integrációs teszt: az induló receptkészlet minden hozzávalója szerepel az induló hozzávaló-listán ([ADR-0003](0003-kanonikus-hozzavalo-lista.md)).
  - Unit teszt: a seed-betöltés ismeretlen `SeedKey`-nél hibával leáll.
  - A rollback kipróbálása a deploy runbook szerint, az eredmény a runbookba kerül.
  - Friss klónos indítási próba (QA-7).
  - Tervezési validáció: [P-12](../../07_ai/prompt_log.md); az architektúra-kapu keresztvalidációjának eredménye ide kerül.
- **Evidence link:** a walking skeleton migrációja és az 1. lépcső integrációs tesztjei; a link az implementációval együtt kerül ide.
