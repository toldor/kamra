# AI Manifest

A Kamra projekt AI-first módon készül. Ez a dokumentum leírja, **milyen AI eszközöket, mire és milyen korlátokkal** használok.
Élő dokumentum: a fejlesztés során folyamatosan bővül. Részletek: [prompt_log.md](prompt_log.md), [verification_log.md](verification_log.md).

Utolsó frissítés: 2026-09-24

---

## 1. Használt eszközök és verziók

| Eszköz | Típus | Modell / verzió | Szerep |
|---|---|---|---|
| Claude Code | CLI ágens | claude-sonnet-4-6, claude-opus-5-5 | Architect / Tutor, CLI Agent: tervezés, ADR, multi-file generálás, tesztfuttatás, CI |
| Copilot CLI | CLI ágens | [fejlesztő tölti ki] | Claude Code alternatívája |
| GitHub Copilot | IDE asszisztens | [fejlesztő tölti ki] | Pair Programmer: implementáció, boilerplate, unit tesztek |
| Gemini CLI | CLI ágens | [fejlesztő tölti ki] | Reviewer: keresztvalidáció, ellenséges tesztelés |
| Claude / Gemini chat | Chat LLM | [fejlesztő tölti ki] | Scope tervezés, debug, trianguláció |

## 2. Felhasználási területek

| Terület | Elsődleges eszköz | Mire | Példa (prompt log) |
|---|---|---|---|
| Tervezés (scope, architektúra, ADR) | Claude | MVP, non-goals, ADR-alapanyag, trade-off elemzés | [P-01](prompt_log.md) |
| Kód | Copilot, Claude Code | Feature implementáció, boilerplate, scaffold | – |
| Teszt | Copilot Chat, Claude Code | Acceptance criteria → tesztesetek, negatív tesztek | – |
| Review | Gemini CLI | Edge case, security, architekturális konzisztencia | – |
| Debug | Claude chat | Stacktrace-elemzés, root cause hipotézisek, regressziós teszt | – |
| Dokumentáció | Claude Code | README, API leírás, docs skeleton | [P-01](prompt_log.md) |
| CI/CD | Claude Code | GitHub Actions workflow | – |

### Munkamódszer

- **Workflow:** generál → review → teszt → integrál (kézi ellenőrzés + commit). Részletek: [AGENTS.md](../../AGENTS.md).
- **Keresztvalidáció:** aki a kódot írta, nem az reviewzza – Claude generál, Gemini CLI reviewz, a végső elfogadás az enyém.
- **Ellenséges teszt:** kritikus moduloknál egy másik modell próbálja eltörni a kódot.
- **Spec-first (TDD):** komplex moduloknál előbb specifikáció és teszt, utána implementáció.
- **Trianguláció:** fontos architekturális döntésnél két modell véleménye; az ellentmondás az ADR része lesz.
- **Megértés-ellenőrzés:** commit előtt a CLI kikérdez a generált kódról.

## 3. Tiltások – mit nem adok ki az AI-nak

- **Secretek:** API kulcsok, jelszók, valódi connection stringek. A `.env` fájlt az ágensek nem olvashatják be (AGENTS.md 7. pont).
- **Személyes adat (PII):** valós felhasználói adat nem kerül promptba; tesztadat csak szintetikus.
- **Éles adatbázis-tartalom:** promptba csak séma és generált mintaadat kerül.
- [fejlesztő bővíti, ha újabb szabály keletkezik]

Technikai védelem: `.gitignore` (`.env`), `.env.example` érték nélkül, commit előtti diff-átnézés.

## 4. Kritikus döntések (ember döntött, nem az AI)

> 3–5 pont, ahol én döntöttem, és miért. Minden sorhoz ADR vagy prompt log hivatkozás.

| # | Döntés | AI javaslata | Döntésem és miért | Hivatkozás |
|---|---|---|---|---|
| D-1 | [fejlesztő tölti ki] | | | |
| D-2 | | | | |
| D-3 | | | | |

## 5. Kockázatok és kezelésük

| Kockázat | Mi a baj, ha bekövetkezik | Kezelés |
|---|---|---|
| Hallucináció (nem létező API, csomag, metódus) | Nem fordul, vagy rejtett hibás viselkedés | Build + tesztek zöldek kell legyenek; új függőség csak jóváhagyással; dokumentációban csak implementált dolog |
| Hibás security tanács | Auth bypass, injection, adatszivárgás | Minden security állítás a [verification_log](verification_log.md)-ba kerül; keresztvalidáció másik modellel; negatív tesztek |
| Hibás teljesítmény-állítás | Lassú lekérdezés, rossz UX | Mérés vagy PoC a verification logban |
| Licenc | Nem kompatibilis licencű kód/csomag | Függőségek licencének ellenőrzése (dependency scan); részletek: `privacy_licensing.md` |
| Prompt injection (az alkalmazás LLM-funkcióiban) | Az LLM kimenete jogosulatlan műveletet indít | LLM kimenet séma-validáció DB előtt; MCP toolok csak szűk, validált use case-eken át írnak |
| Megértés hiánya | Nem tudom megvédeni a kódot | Kódkikérdezés commit előtt; generált kód refaktorálása |
| Tesztek gyengítése a zöld CI kedvéért | Hamis biztonságérzet | AGENTS.md tiltja; teszt módosítás csak indoklással és jóváhagyással |

## 6. Tanulságok – AI hibák és korlátok

> A fejlesztés során talált AI hibák összefoglalója, hivatkozással a verification log bejegyzésre.

[fejlesztő bővíti a fejlesztés során]
