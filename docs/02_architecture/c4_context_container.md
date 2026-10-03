# C4 – Context, Container és Deployment view

A Kamra rendszer környezete, futtatható egységei és telepítése. A diagramok a döntéseket tükrözik: [ADR-0004](adr/0004-clean-architecture-retegek.md) (rétegek, külön MCP-host), [ADR-0005](adr/0005-postgresql-ef-core-migraciok.md) (PostgreSQL, migrator), [ADR-0006](adr/0006-cookie-auth-identity.md) (auth, azonos origin), [ADR-0007](adr/0007-rest-api-hibamodell.md) (REST API). A minőségi elvárások: [quality_attributes.md](quality_attributes.md).

## Jelmagyarázat

| Jelölés | Jelentés |
|---|---|
| Sötétkék doboz | Szereplő (személy) |
| Kék doboz | A Kamra rendszer vagy annak containere |
| Szürke doboz | Külső rendszer |
| Henger | Adattár |
| Keretezett csoport | Határ: auth boundary vagy hálózati határ (a címe megmondja, melyik) |
| Folytonos nyíl | Kapcsolat: mi megy át [protokoll] |
| Szaggatott nyíl / doboz | Tervezett, még ADR-döntésre váró elem |

## 1. Context

```mermaid
flowchart LR
    user["Felhasználó<br/>[Szereplő]<br/>Egy háztartás vezetője: készletet rögzít,<br/>receptet választ, főz, bevásárlólistát kezel"]
    kamra["Kamra<br/>[Szoftverrendszer]<br/>Készlet- és receptkezelés, ajánlás,<br/>bevásárlólista, chat"]
    llm["Külső LLM-szolgáltató<br/>[Külső rendszer]<br/>Tételjavaslatok kinyerése szövegből,<br/>chat-válaszok"]

    user -- "Használja<br/>[HTTPS, böngésző]" --> kamra
    kamra -- "Beírt szöveg, a válaszhoz szükséges készletadat és a tool-leírások; fiókadat soha.<br/>Vissza: strukturált tételjavaslat, chat-válasz vagy toolhívás-kérés<br/>[HTTPS, JSON API]" --> llm

    classDef person fill:#08427b,color:#fff,stroke:#052e56
    classDef system fill:#1168bd,color:#fff,stroke:#0b4884
    classDef external fill:#8a8a8a,color:#fff,stroke:#5e5e5e
    class user person
    class kamra system
    class llm external
```

| Elem | Felelősség |
|---|---|
| Felhasználó | Egy háztartás vezetője (a [vision.md](../01_product/vision.md) mindkét personája); egy fiók = egy háztartás. |
| Kamra | Készlet, receptek és ajánlás, főzés és készletcsökkenés, bevásárlólista, csak olvasó chat. |
| Külső LLM-szolgáltató | Szövegből tételjavaslatot készít, és a chat kérdéseire válaszol. A konkrét szolgáltató külön ADR-ben dől el. |

## 2. Container

```mermaid
flowchart TB
    user["Felhasználó<br/>[Szereplő]"]
    llm["Külső LLM-szolgáltató<br/>[Külső rendszer]"]

    subgraph kamra["Kamra"]
        spa["SPA<br/>[React + TypeScript, böngészőben]<br/>Felhasználói felület"]
        subgraph server["Auth boundary: minden adatkérés hitelesített session-t igényel"]
            api["API<br/>[ASP.NET Core, .NET 10]<br/>REST /api/v1, auth, antiforgery,<br/>a SPA kiszolgálása, LLM-hívások"]
            subgraph internal["Belső hálózat: kívülről nem érhető el"]
                mcp["MCP-szerver<br/>[ASP.NET Core]<br/>4 csak olvasó tool a chathez"]
                mig["Migrator<br/>[EF migration bundle, egyszer fut]<br/>Séma- és seed-migrációk"]
                db[("Adatbázis<br/>[PostgreSQL 18]<br/>Üzleti adatok és Identity-táblák")]
            end
        end
    end

    user -- "Használja [HTTPS]" --> spa
    api -- "Kiszolgálja a statikus fájlokat [HTTPS]" --> spa
    spa -- "JSON-kérések session-cookie-val<br/>és antiforgery fejléccel [HTTPS, /api/v1]" --> api
    api -- "Olvas és ír [EF Core, TCP]" --> db
    api -- "Szöveg, szükséges készletadat, tool-leírások;<br/>vissza: tételjavaslat, válasz, toolhívás-kérés [HTTPS]" --> llm
    api -. "Toolhívás háztartás-kontextussal<br/>[MCP over HTTP] – tervezett" .-> mcp
    mcp -- "Olvas [EF Core, TCP]" --> db
    mig -- "Séma és seed [TCP]" --> db

    classDef person fill:#08427b,color:#fff,stroke:#052e56
    classDef container fill:#438dd5,color:#fff,stroke:#2e6295
    classDef external fill:#8a8a8a,color:#fff,stroke:#5e5e5e
    class user person
    class spa,api,mcp,mig,db container
    class llm external
```

| Container | Technológia | Felelősség | Kódnév |
|---|---|---|---|
| SPA | React + TypeScript (Vite), a böngészőben | Felhasználói felület; minden API-hívás a közös kliensen át | `src/frontend` (`src/api/`) |
| API | ASP.NET Core, .NET 10 | REST `/api/v1` controllerekkel, cookie-alapú auth és antiforgery, a SPA statikus kiszolgálása, LLM-hívások (tételjavaslat, chat) | `KamraApp.Api` |
| MCP-szerver | ASP.NET Core host | Négy csak olvasó tool a chathez, az Application használati eseteken át | `KamraApp.McpServer` |
| Migrator | EF migration bundle | Séma- és seed-migrációk az API és az MCP-szerver indulása előtt; rollback célmigrációval | `KamraApp.Infrastructure` migrációi |
| Adatbázis | PostgreSQL 18 | Üzleti adatok és Identity-táblák | – |

Az API és az MCP-szerver is a saját folyamatában tölti be az Application, a Domain és az Infrastructure réteget ([ADR-0004](adr/0004-clean-architecture-retegek.md)); a rétegek nem külön futtatható egységek, ezért a Container diagramon nem szerepelnek.

| Honnan | Hová | Protokoll | Mi megy át | Hitelesítés |
|---|---|---|---|---|
| Felhasználó (böngésző) | API | HTTPS | A SPA statikus fájljai | – (nyilvános) |
| SPA | API | HTTPS, JSON, `/api/v1` | Készlet-, recept-, bevásárlólista- és chat-kérések | Session-cookie (`HttpOnly`, `Secure`, `SameSite=Strict`) + antiforgery fejléc a módosító kéréseknél |
| API | Adatbázis | TCP (Npgsql) | Lekérdezések és írások, háztartás szerint szűrve | Adatbázis-felhasználó, connection string környezeti változóból |
| API | Külső LLM | HTTPS | A beírt szöveg, a válaszhoz szükséges készletadat és a tool-leírások; fiókadat soha. Vissza: strukturált tételjavaslat, chat-válasz vagy toolhívás-kérés; az LLM nem fér hozzá közvetlenül az adatbázishoz, a toolhívást az API hajtja végre. | API-kulcs környezeti változóból |
| API | MCP-szerver | MCP over HTTP, belső hálózat (tervezett) | Toolhívások; a háztartás-azonosító a sessionből, soha nem az LLM-től | Az MCP-integráció ADR-jének döntése |
| MCP-szerver | Adatbázis | TCP (Npgsql) | Csak olvasó lekérdezések, háztartás szerint szűrve | Adatbázis-felhasználó |
| Migrator | Adatbázis | TCP (Npgsql) | Séma- és seed-migrációk | Migrációs adatbázis-felhasználó |

## 3. Deployment view

```mermaid
flowchart TB
    browser["Böngésző<br/>[Chrome, Edge, Firefox; asztal és mobil]"]
    operator["Fejlesztő / üzemeltető<br/>[Szereplő]"]
    llm["Külső LLM-szolgáltató"]

    subgraph ci["CI: GitHub Actions"]
        build["Build, format, lint, tesztek,<br/>OpenAPI-diff"]
        tc[("Testcontainers<br/>PostgreSQL 18")]
        build --> tc
    end

    subgraph host["Docker-gazdagép: fejlesztői gép, localhost"]
        subgraph compose["Docker Compose"]
            apic["api konténer<br/>KamraApp.Api + SPA (wwwroot)<br/>egyetlen publikált port"]
            subgraph net["Belső Docker-hálózat: kamra-network"]
                mcpc["mcp konténer<br/>KamraApp.McpServer"]
                migc["migrator konténer<br/>efbundle, egyszer fut"]
                dbc["db konténer<br/>postgres:18"]
            end
            pg[("pgdata volume")]
            dp[("dpkeys volume<br/>Data Protection kulcsok")]
        end
    end

    subgraph target["Tervezett célkörnyezet – döntés a deploy runbookban"]
        vm["Linux VM: ugyanaz a Compose<br/>+ TLS-proxy konténer"]
    end

    browser -- "HTTP localhost / HTTPS" --> apic
    operator -- "docker compose up, logok" --> compose
    operator -- "push, pull request" --> ci
    migc -- "migráció, utána kilép" --> dbc
    apic -- "indul, ha a migrator sikeres" --> dbc
    apic -.-> mcpc
    mcpc --> dbc
    dbc --- pg
    apic --- dp
    apic -- "HTTPS" --> llm
    compose -.-> target

    classDef planned stroke-dasharray: 5 5
    class target,vm planned
```

| Környezet | Mi fut | Hogyan indul | Konfiguráció |
|---|---|---|---|
| Fejlesztői | Az API `dotnet run`-nal, a SPA a Vite dev serverrel (az `/api` kéréseket az API-ra proxyzza), az adatbázis Compose-ban | `docker compose up -d db`, `dotnet ef database update`, `dotnet run`, `npm run dev` | `appsettings.Development.json` + környezeti változók |
| Docker Compose (localhost) | api (SPA-val), mcp, migrator, db; volume-ok: `pgdata`, `dpkeys`; kívülről csak az api portja érhető el | `docker compose up --build`; a migrator a sikeres lefutás után engedi indulni az api-t és az mcp-t | `.env` (nincs a repóban) a [.env.example](../../.env.example) alapján; induláskor validálva (QA-7) |
| CI (GitHub Actions) | Build, formázás, lint, tesztek (Testcontainers, `postgres:18`), OpenAPI-diff | Push és pull request | CI secretek, valódi LLM-hívás nélkül |
| Tervezett célkörnyezet | Ugyanaz a Compose egy Linux VM-en, TLS-proxy konténerrel | A deploy runbook szerint | A deploy runbook szerint |

## Ismert korlátok

- **MCP-útvonal:** az API és az MCP-szerver közötti hívás mechanizmusa (és a háztartás-kontextus átadása) az MCP-integráció ADR-jében dől el; ha az a toolokat az API folyamatába helyezi, az MCP-szerver container megszűnik.
- **Nincs felhőkörnyezet:** a rendszer Docker Compose-zal, localhoston fut; a célkörnyezet a deploy runbookban dől el.
- **HTTPS:** a session-cookie `Secure`, ezért nem localhost telepítéshez TLS-végződtetés kell (TLS-proxy vagy a platform szolgáltatása).
- **LLM-szolgáltató:** a konkrét szolgáltató és modell külön ADR-ben dől el; a diagram általános külső rendszerként mutatja.
