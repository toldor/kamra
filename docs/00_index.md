# Dokumentáció – Kamra (Smart Pantry & Recipe Manager)

BSc szakdolgozat, SZTE – Docs-as-Code elvű dokumentáció.
Forrás: `Szakdolgozói leadandó csomag v1.2` (2026-02-23).
Minden fájl a kóddal együtt frissül (lásd `AGENTS.md` 9. pont).

Állapotok: **Kész** · *Hiányzik* · (opcionális)

---

## 01_product – Termék és hatókör (12 pont)

| Fájl | Tartalom | Állapot |
|---|---|---|
| *[vision.md](01_product/vision.md)* | Persona, értékajánlat, non-goals, kockázatok | **Hiányzik** |
| [scope_contract.md](01_product/scope_contract.md) | MVP story-k, elfogadási kritériumok, Definition of Done | Kész |
| [capability_map.md](01_product/capability_map.md) | Funkciók állapottáblája (Value + Productization) | Kész |
| *[metrics.md](01_product/metrics.md)* | North Star + guardrail metrikák, mérési terv | **Hiányzik** |

---

## 02_architecture – Architektúra és döntések (13 pont)

| Fájl | Tartalom | Állapot |
|---|---|---|
| *[c4_context_container.md](02_architecture/c4_context_container.md)* | C4 Context + Container diagram | **Hiányzik** |
| *[quality_attributes.md](02_architecture/quality_attributes.md)* | 5-8 nemfunkcionális elvárás, ≥2 quality scenario | **Hiányzik** |
| [adr/0001-template.md](02_architecture/adr/0001-template.md) | ADR sablon (másolható, min. 5–8 ADR kell) | Kész (sablon) |

---

## 03_design – API, adatmodell, hibakezelés (részben Engineering Quality)

| Fájl | Tartalom | Állapot |
|---|---|---|
| [api.md](03_design/api.md) | REST végpontok, auth, hibakódok, OpenAPI | Kész (skeleton) |
| [data_model.md](03_design/data_model.md) | Entitások, kapcsolatok, migrációs stratégia | Kész (skeleton) |
| [error_handling.md](03_design/error_handling.md) | Hibakategóriák, RFC 7807, retry, logolás | Kész (skeleton) |
| [mcp_tools.md](03_design/mcp_tools.md) | MCP eszközök leírása, input séma, biztonság | Kész (skeleton) |

---

## 04_quality – Tesztelés és minőségi kapuk (15 pont)

| Fájl | Tartalom | Állapot |
|---|---|---|
| *[test_strategy.md](04_quality/test_strategy.md)* | Teszt piramis, mock stratégia, CI quality gate-ek | **Hiányzik** |
| [test_report.md](04_quality/test_report.md) | Utolsó futás eredménye, lefedettség, ismert hiányok | Kész (skeleton) |

---

## 05_security_ops – Biztonság, üzemeltetés (10 + 15 pont)

| Fájl | Tartalom | Állapot |
|---|---|---|
| *[threat_model.md](05_security_ops/threat_model.md)* | STRIDE fenyegetések (≥6), mitigáció, residual risk | **Hiányzik** |
| *[privacy_licensing.md](05_security_ops/privacy_licensing.md)* | Adatkategóriák, adatáramlás, AI adatküldési szabály, licencek | **Hiányzik** |
| *[deploy_runbook.md](05_security_ops/deploy_runbook.md)* | Deploy lépések, rollback, ≥2 incident forgatókönyv | **Hiányzik** |
| [observability.md](05_security_ops/observability.md) | Naplózás, health check, metrikák, debugging guide | Kész (skeleton) |

---

## 06_release – Kiadás és önértékelés

| Fájl | Tartalom | Állapot |
|---|---|---|
| *[demo_script.md](06_release/demo_script.md)* | 5-7 perces stabil demo forgatókönyv | **Hiányzik** |
| *[self_assessment.md](06_release/self_assessment.md)* | Kötelező önértékelés kategóriánként (4.5 pont a PDF-ben) | **Hiányzik** |
| *[changelog.md](06_release/changelog.md)* | Release notes, scope változások | **Hiányzik** |

---

## 07_ai – AI átláthatóság (10 pont, kapu feltétel)

| Fájl | Tartalom | Állapot |
|---|---|---|
| *[ai_manifest.md](07_ai/ai_manifest.md)* | Használt eszközök, tiltások, kritikus döntések, kockázatok | **Hiányzik** |
| [prompt_log.md](07_ai/prompt_log.md) | 10-20 kulcsprompt kontextussal és kimenet linkkel | Kész (P-01 bejegyzéssel) |
| [verification_log.md](07_ai/verification_log.md) | AI állítások ellenőrzési naplója (min. 10 bejegyzés kell) | Kész (sablon) |

---

## Kapu feltételek összefoglalója (pontlevonás vagy plafon ha hiányzik)

| Feltétel | Elvárás | Ha hiányzik |
|---|---|---|
| Futtathatóság | README alapján 15 perc alatt elindul | Max. 40 pont |
| AI átláthatóság | ai_manifest + prompt_log + verification_log | Max. 70 pont |
| Secret hygiene | Nincs repo-ban token/jelszó/API kulcs | Leadás visszautasítva vagy -20 pont |
| Automata tesztek | Legalább 30 teszt, CI-ban futtatható | Max. 70 pont |

## Hiányzó fájlok összesítve (teendők)

- `docs/01_product/vision.md`
- `docs/01_product/metrics.md`
- `docs/02_architecture/c4_context_container.md`
- `docs/02_architecture/quality_attributes.md`
- `docs/02_architecture/adr/0002-*.md` … (min. 5–8 ADR kell összesen)
- `docs/04_quality/test_strategy.md`
- `docs/05_security_ops/threat_model.md`
- `docs/05_security_ops/privacy_licensing.md`
- `docs/05_security_ops/deploy_runbook.md`
- `docs/06_release/demo_script.md`
- `docs/06_release/self_assessment.md`
- `docs/06_release/changelog.md`
- `docs/07_ai/ai_manifest.md`
