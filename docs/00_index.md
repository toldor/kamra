# Dokumentáció – Kamra (Smart Pantry & Recipe Manager)

BSc szakdolgozat, SZTE – Docs-as-Code elvű dokumentáció.
Forrás: `Szakdolgozói leadandó csomag v1.2` (2026-02-23).
Minden fájl a kóddal együtt frissül (lásd `AGENTS.md` 9. pont).

Állapotok: **Kész** · *Hiányzik* · (opcionális)

---

## 01_product – Termék és hatókör (12 pont)

| Fájl | Tartalom | Állapot |
|---|---|---|
| [vision.md](01_product/vision.md) | Probléma, persona, értékajánlat, North Star + guardrailek, non-goals, kockázatok | Kész |
| [competitor_analysis.md](01_product/competitor_analysis.md) | Versenytársak funkciónkénti, forrásolt összehasonlítása | Kész |
| [scope_contract.md](01_product/scope_contract.md) | MVP story-k, elfogadási kritériumok, Definition of Done | Kész |
| [capability_map.md](01_product/capability_map.md) | Funkciók állapottáblája (Value + Productization) | Kész |
| [metrics.md](01_product/metrics.md) | North Star + guardrail metrikák, mérési terv, a lekérdezések futtatása és ellenőrzése | Kész |
| [metrics.sql](01_product/metrics.sql) | A négy mérőszám SQL-lekérdezése (CAP-12) | Kész (6 integrációs teszt) |
| [ux_flows.md](01_product/ux_flows.md) | 4 fő user flow, hiba- és üres állapotok, alap a11y | Kész (képernyőképekkel; a mondatos bevitel, a chat és a kinézet bővítése is) |

---

## 02_architecture – Architektúra és döntések (13 pont)

| Fájl | Tartalom | Állapot |
|---|---|---|
| [c4_context_container.md](02_architecture/c4_context_container.md) | C4 Context + Container diagram, deployment view | Kész (az MCP-szerver az API folyamatában) |
| [c4_component.md](02_architecture/c4_component.md) | C4 Component diagram (backend modulhatárok) | Kész |
| [quality_attributes.md](02_architecture/quality_attributes.md) | 7 nemfunkcionális elvárás, 3 quality scenario mérőszámmal | Kész (a QA-5 mérve: [performance.md](04_quality/performance.md)) |
| [adr/0001-template.md](02_architecture/adr/0001-template.md) | ADR sablon (másolható, min. 8 ADR kell) | Kész (sablon) |
| [adr/0002-fix-atvalthato-mertekegysegek.md](02_architecture/adr/0002-fix-atvalthato-mertekegysegek.md) | Fix, átváltható mértékegységek „csomag” nélkül | Accepted |
| [adr/0003-kanonikus-hozzavalo-lista.md](02_architecture/adr/0003-kanonikus-hozzavalo-lista.md) | Kanonikus hozzávaló-lista hierarchia nélkül | Accepted |
| [adr/0004-clean-architecture-retegek.md](02_architecture/adr/0004-clean-architecture-retegek.md) | Clean Architecture rétegek, használati eset osztályok, aggregátumonkénti repository | Accepted |
| [adr/0005-postgresql-ef-core-migraciok.md](02_architecture/adr/0005-postgresql-ef-core-migraciok.md) | PostgreSQL + EF Core, migrator-szolgáltatás, JSON seed, GUID v7 | Accepted |
| [adr/0006-cookie-auth-identity.md](02_architecture/adr/0006-cookie-auth-identity.md) | Cookie-alapú authentikáció ASP.NET Core Identity-vel, azonos origin, antiforgery | Accepted |
| [adr/0007-rest-api-hibamodell.md](02_architecture/adr/0007-rest-api-hibamodell.md) | REST + controllerek, /api/v1, kivétel-alapú ProblemDetails, generált OpenAPI | Accepted |
| [adr/0008-konkurencia-es-idempotencia.md](02_architecture/adr/0008-konkurencia-es-idempotencia.md) | Optimista zárolás xmin-nel, főzés-idempotencia kérésazonosítóval, upsert és egyedi kényszerek | Accepted |
| [adr/0009-llm-szolgaltato.md](02_architecture/adr/0009-llm-szolgaltato.md) | LLM-szolgáltató: Gemini API (gemini-3.1-flash-lite), REST-hívás kötött JSON-sémával, PoC-méréssel | Accepted |
| [adr/0010-mcp-integracio.md](02_architecture/adr/0010-mcp-integracio.md) | MCP-integráció: az MCP-szerver az API folyamatában, memóriabeli szállítással; a Gemini eszközhívási körét az API vezérli | Accepted |
| [adr/0011-serilog-strukturalt-naplozas.md](02_architecture/adr/0011-serilog-strukturalt-naplozas.md) | Serilog JSON-napló, szerver által generált correlationId | Accepted |
| [adr/0012-bongeszos-kereses.md](02_architecture/adr/0012-bongeszos-kereses.md) | Böngészőben szűrő kereső a Receptek és a Bevásárlólista oldalon | Accepted |
| [adr/0013-vizualis-rendszer.md](02_architecture/adr/0013-vizualis-rendszer.md) | Saját vizuális rendszer: tokenek CSS-változókban, saját tárhelyű betűk | Accepted |
| [adr/0014-elo-kitelepites-azure-caddy.md](02_architecture/adr/0014-elo-kitelepites-azure-caddy.md) | Élő kitelepítés: Azure VM, ugyanaz a Docker Compose, Caddy TLS-proxy, meghívókódos regisztráció | Accepted |

---

## 03_design – API, adatmodell, hibakezelés (részben Engineering Quality)

| Fájl | Tartalom | Állapot |
|---|---|---|
| [api.md](03_design/api.md) | REST végpontok, auth, hibakódok, OpenAPI | Kész (mindhárom lépcső) |
| [data_model.md](03_design/data_model.md) | Entitások, kapcsolatok, migrációs stratégia | Kész (a tételjavaslattal) |
| [error_handling.md](03_design/error_handling.md) | Hibakategóriák, RFC 9457, retry, logolás | Kész (az LLM-hibakódokkal) |
| [mcp_tools.md](03_design/mcp_tools.md) | MCP eszközök leírása, input séma, biztonság | Kész (négy eszköz) |

---

## 04_quality – Tesztelés és minőségi kapuk (15 pont)

| Fájl | Tartalom | Állapot |
|---|---|---|
| [test_strategy.md](04_quality/test_strategy.md) | Teszt piramis, mock stratégia, CI quality gate-ek | Kész |
| [test_report.md](04_quality/test_report.md) | Utolsó futás eredménye, lefedettség, ismert hiányok | Kész (1261 teszt, P1–P20, LLM- és chat-mérés, zöld CI) |
| [manual_tests.md](04_quality/manual_tests.md) | Kézi tesztjegyzőkönyv (kiírás 5. pont) | Kész (MT-01–MT-07; az élő példány telepítése, visszaállítása és frissítése: MT-05 – MT-07) |
| [traceability.md](04_quality/traceability.md) | Megfeleltetés: követelmény → flow → végpont és képernyő → teszt | Kész |
| [uat.md](04_quality/uat.md) | Felhasználói kipróbálás terve, két kör | Kész az 1. körre (nov. 1., 4 résztvevő); a 2. kör az élő példányon (nov. 25–27.) |
| [performance.md](04_quality/performance.md) | Teljesítmény-alapmérés, a mért szűk keresztmetszet | Kész (az ajánlás p95 18–22 ms) |
| [postmortem.md](04_quality/postmortem.md) | Post-mortem egy valós hibáról (bónusz) | Kész |

---

## 05_security_ops – Biztonság, üzemeltetés (10 + 15 pont)

| Fájl | Tartalom | Állapot |
|---|---|---|
| [threat_model.md](05_security_ops/threat_model.md) | STRIDE fenyegetések (≥6), mitigáció, residual risk | Kész (17 fenyegetés, az MI-funkcióké és az élő példányé is) |
| [privacy_licensing.md](05_security_ops/privacy_licensing.md) | Adatkategóriák, adatáramlás, AI adatküldési szabály, licencek | Kész |
| [deploy_runbook.md](05_security_ops/deploy_runbook.md) | Deploy lépések, rollback, ≥2 incident forgatókönyv | Kész (3 incidens; tiszta környezetben kipróbálva, MT-02; élő példány az Azure-ban, MT-05, MT-06) |
| [observability.md](05_security_ops/observability.md) | Naplózás, health check, metrikák, debugging guide | Kész (3 naplóalapú metrika; külön metrikagyűjtő szándékosan nincs) |

---

## 06_release – Kiadás és önértékelés

| Fájl | Tartalom | Állapot |
|---|---|---|
| [demo_script.md](06_release/demo_script.md) | 5-7 perces stabil demo forgatókönyv | Kész |
| [self_assessment.md](06_release/self_assessment.md) | Kötelező önértékelés kategóriánként (4.5 pont a PDF-ben) | Kész |
| [changelog.md](06_release/changelog.md) | Release notes, scope változások | Kész (0.1.0 – 0.9.0) |

---

## 07_ai – AI átláthatóság (10 pont, kapu feltétel)

| Fájl | Tartalom | Állapot |
|---|---|---|
| [ai_manifest.md](07_ai/ai_manifest.md) | Használt eszközök, tiltások, kritikus döntések, kockázatok | Kész (D-1–D-3, tanulságok) |
| [prompt_log.md](07_ai/prompt_log.md) | 10-20 kulcsprompt kontextussal és kimenet linkkel | Kész (P-01–P-37; Kulcsbejegyzések tábla validálással) |
| [verification_log.md](07_ai/verification_log.md) | AI állítások ellenőrzési naplója (min. 10 bejegyzés kell) | Kész (V-01–V-108) |
| [review_prompts.md](07_ai/review_prompts.md) | Verziózott review-, ellenséges teszt- és megértés-ellenőrző promptsablonok | Kész |

---

## Kapu feltételek összefoglalója (pontlevonás vagy plafon ha hiányzik)

| Feltétel | Elvárás | Ha hiányzik |
|---|---|---|
| Futtathatóság | README alapján 15 perc alatt elindul | Max. 40 pont |
| AI átláthatóság | ai_manifest + prompt_log + verification_log | Max. 70 pont |
| Secret hygiene | Nincs repo-ban token/jelszó/API kulcs | Leadás visszautasítva vagy -20 pont |
| Automata tesztek | Legalább 30 teszt, CI-ban futtatható | Max. 70 pont |

## Hiányzó fájlok

Nincs: a v1.2 minden kötelező dokumentuma megvan (2026-11-19). Az ADR-ek: 0002–0014 (13 döntés) és a sablon.
