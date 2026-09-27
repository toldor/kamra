# Capability Map

Az alkalmazás képességei és megvalósítottsági állapotuk: mennyi a felhasználói érték (**Value**) és mennyi a termékminőség (**Productization**: minőség, üzemeltetés, biztonság).
Frissítsd minden lépcső végén; „Done” státusz csak evidence- és tesztlinkkel adható. A story-k és a stretch tételek: [scope_contract.md](scope_contract.md).

| Capability | Kategória | Evidence (link) | Teszt (link) | Státusz |
|---|---|---|---|---|
| F-01 Készlettétel hozzáadása, szerkesztése, törlése (form) – US-1 | Value | – | – | Planned |
| F-02 Természetes nyelvű gyorsbevitel tételjavaslatokkal – US-2 | Value | – | – | Planned |
| F-03 Lejárati dátum becslés kategória alapján – US-1 | Value | – | – | Planned |
| F-04 Ajánlás készletből, lejárat-prioritással – US-3 | Value | – | – | Planned |
| F-06 Automatikus készletcsökkentés főzés után – US-4 | Value | – | – | Planned |
| F-07 Bevásárlólista bevásárlójavaslatokkal és minimumszinttel – US-5 | Value | – | – | Planned |
| F-08 Chat asszisztens MCP-n keresztül (csak olvasó) – US-6 | Value | – | – | Planned |
| F-09 Receptkezelés: induló receptkészlet + kézi felvitel – US-3 | Value | – | – | Planned |
| F-11 Kézi készletcsökkentés csökkenési okkal + készletmozgás-napló – US-1, US-4 | Productization | – | – | Planned |
| F-12 Metrika-lekérdezések (North Star + guardrailek) – [metrics.md](metrics.md) | Productization | – | – | Planned |
| F-14 Regisztráció, bejelentkezés, háztartásonkénti adatelkülönítés – keresztmetszeti | Productization | – | – | Planned |
| F-15 CI pipeline: build, formázás, lint, tesztek, lefedettség, függőség-vizsgálat – DoD | Productization | – | – | Planned |
| F-16 Strukturált naplózás és health check – [observability.md](../05_security_ops/observability.md) | Productization | – | – | Planned |
| F-17 LLM-kiesés kezelése: időtúllépés, újrapróbálás, kézi form, AI-funkciók kikapcsolhatósága – US-2 | Productization | – | – | Planned |
| F-05 AI-vezérelt adag- és helyettesítési javaslat – stretch | Value | – | – | Planned |
| F-10 AI-receptötlet mentése jóváhagyással – stretch | Value | – | – | Planned |
| F-13 Kipipált bevásárlólista-tételből készletbevitel – stretch | Value | – | – | Planned |

**Státuszok:** `Planned` · `Partial` · `Done`

## Ismert hiányosságok

- Egyik képesség sincs még implementálva, ezért evidence- és tesztlink sincs.
