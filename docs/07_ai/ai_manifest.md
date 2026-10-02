# AI Manifest

A Kamra projekt AI-first módon készül. Ez a dokumentum leírja, **milyen AI eszközöket, mire és milyen korlátokkal** használok.
Élő dokumentum: a fejlesztés során folyamatosan bővül. Részletek: [prompt_log.md](prompt_log.md), [verification_log.md](verification_log.md).

Utolsó frissítés: 2026-10-02

---

## 1. Használt eszközök és verziók

| Eszköz | Típus | Modell / verzió | Szerep |
|---|---|---|---|
| Claude Code | CLI ágens | claude-sonnet-4-6, claude-opus-5-5 | Architect / Tutor, CLI Agent: tervezés, ADR, multi-file generálás, tesztfuttatás, CI |
| Copilot CLI | CLI ágens | Claude Sonnet 4 | Claude Code alternatívája |
| GitHub Copilot | IDE asszisztens | Claude Sonnet 4, gpt-5.6-luna | Pair Programmer: implementáció, boilerplate, unit tesztek; Reviewer: dokumentum-review |
| Antigravity | Agentic IDE | Gemini 3.1 Pro (High) | Reviewer: keresztvalidáció, dokumentációs audit, ellenséges tesztelés |
| Gemini chat | Chat LLM | Gemini 3.1 Pro | Scope tervezés, debug, trianguláció |

## 2. Felhasználási területek

| Terület | Elsődleges eszköz | Mire | Példa (prompt log) |
|---|---|---|---|
| Tervezés (scope, architektúra, ADR) | Claude Code | MVP, non-goals, ADR-alapanyag, trade-off elemzés, kikérdezéses (grilling) tervezés | [P-03–P-05](prompt_log.md) |
| Kód | Copilot, Claude Code | Feature implementáció, boilerplate, scaffold | – |
| Teszt | Copilot Chat, Claude Code | Acceptance criteria → tesztesetek, negatív tesztek | – |
| Review | Antigravity, GitHub Copilot | Edge case, security, architekturális konzisztencia, dokumentációs audit | [P-06, P-07, P-10](prompt_log.md) |
| Debug | Claude Code, Gemini chat | Stacktrace-elemzés, root cause hipotézisek, regressziós teszt | – |
| Dokumentáció | Claude Code | README, API leírás, docs skeleton | [P-01](prompt_log.md) |
| CI/CD | Claude Code | GitHub Actions workflow | – |

### Munkamódszer

- **Workflow:** generál → review → teszt → integrál (kézi ellenőrzés + commit). Részletek: [AGENTS.md](../../AGENTS.md).
- **Keresztvalidáció:** aki a kódot írta, nem az reviewzza – Claude generál, Antigravity (Gemini) reviewz, a végső elfogadás az enyém. A reviewer állításait is ellenőrzöm (lásd [V-04](verification_log.md)).
- **Ellenséges teszt:** kritikus moduloknál egy másik modell próbálja eltörni a kódot.
- **Spec-first (TDD):** komplex moduloknál előbb specifikáció és teszt, utána implementáció.
- **Trianguláció:** fontos architekturális döntésnél két modell véleménye; az ellentmondás az ADR része lesz.
- **Megértés-ellenőrzés:** commit előtt a CLI kikérdez a generált kódról.

## 3. Tiltások – mit nem adok ki az AI-nak

- **Secretek:** API kulcsok, jelszók, valódi connection stringek. A `.env` fájlt az ágensek nem olvashatják be (AGENTS.md 7. pont).
- **Személyes adat (PII):** valós felhasználói adat nem kerül promptba; tesztadat csak szintetikus.
- **Éles adatbázis-tartalom:** promptba csak séma és generált mintaadat kerül.

Technikai védelem: `.gitignore` (`.env`), `.env.example` érték nélkül, commit előtti diff-átnézés.

## 4. Kritikus döntések (ember döntött, nem az AI)

> 3–5 pont, ahol én döntöttem, és miért. Minden sorhoz ADR vagy prompt log hivatkozás.

| # | Döntés | AI javaslata | Döntésem és miért | Hivatkozás |
|---|---|---|---|---|
| D-1 | **North Star: megmentett főzések száma** | „Lejárat előtt felhasznált készlet aránya” (arány) | A darabszámot választottam, mert közvetlenül a termék fő tevékenységét (főzés hamarosan lejáró alapanyagból) méri, és egyszerűen kommunikálható. Az arány gyengeségét, hogy nem mutatja a kárba ment mennyiséget, a G1 guardrail (pazarolt arány) pótolja. | [P-03](prompt_log.md), [metrics.md](../01_product/metrics.md) |
| D-2 | **Bevásárlólista: csak javaslat, a felhasználó dönt** | Az elfogyott hozzávaló automatikusan kerüljön a listára | Automatikus felkerülés helyett bevásárlójavaslat, amit a felhasználó elfogad vagy elutasít, és ugyanez igaz a minimumszintre is. Ugyanazt az elvet követi, mint az AI-bevitel: a rendszer javasol, az ember dönt. Így a lista nem telik meg olyasmivel, amit a felhasználó nem akar megvenni. | [P-05](prompt_log.md), [scope_contract.md](../01_product/scope_contract.md) US-5 |

## 5. Kockázatok és kezelésük

| Kockázat | Mi a baj, ha bekövetkezik | Kezelés |
|---|---|---|
| Hallucináció (nem létező API, csomag, metódus) | Nem fordul, vagy rejtett hibás viselkedés | Build + tesztek zöldek kell legyenek; új függőség csak jóváhagyással; dokumentációban csak implementált dolog |
| Hibás security tanács | Auth bypass, injection, adatszivárgás | Minden security állítás a [verification_log](verification_log.md)-ba kerül; keresztvalidáció másik modellel; negatív tesztek |
| Hibás teljesítmény-állítás | Lassú lekérdezés, rossz UX | Mérés vagy PoC a verification logban |
| Licenc | Nem kompatibilis licencű kód/csomag | Függőségek licencének ellenőrzése (dependency scan); részletek: `privacy_licensing.md` |
| Prompt injection (az alkalmazás LLM-funkcióiban) | Az LLM kimenete jogosulatlan műveletet indít | LLM kimenet séma-validáció DB előtt; az AI kimenete csak felhasználói jóváhagyással kerül az adatbázisba; az MCP toolok csak olvasnak ([mcp_tools.md](../03_design/mcp_tools.md)) |
| Megértés hiánya | Nem tudom megvédeni a kódot | Kódkikérdezés commit előtt; generált kód refaktorálása |
| Tesztek gyengítése a zöld CI kedvéért | Hamis biztonságérzet | AGENTS.md tiltja; teszt módosítás csak indoklással és jóváhagyással |

## 6. Tanulságok – AI hibák és korlátok

> A fejlesztés során talált AI hibák összefoglalója, hivatkozással a verification log bejegyzésre.

- **Az AI túlértékeli a termék egyediségét ([V-01](verification_log.md)).** Az értékajánlathoz javasolt „csak nálunk van” állítás egy forrásellenőrzés után megdőlt: a szabad szöveges, több tételes bevitel más eszközökben is létezik. Tanulság: az AI által javasolt egyediségi és versenytárs-állítást mindig forrással ellenőrzöm, és negatív állítást („más appban nincs”) nem teszek, mert nem igazolható.
- **A forrás pontos olvasása a problémát is szűkítette ([V-02](verification_log.md)).** Az AI által javasolt pazarlási adat az elsődleges forrás (NÉBIH) szerint helyes volt, de a forrás a kidobás három okát sorolja fel, ebből a termék csak kettőt (megfeledkezés, túlvásárlás) céloz. Tanulság: számszerű állítást csak az elsődleges forrásból veszek át, és azt is ellenőrzöm, hogy a forrás pontosan azt támasztja-e alá, amit állítok, nem csak a számot.
- **A reviewer AI nem dönt helyettem ([P-10](prompt_log.md), [V-04](verification_log.md)).** A UX-validálás során a reviewer (Antigravity) „hivatalosan engedélyezett” egy új függőséget (axe-core), és korábban téves állítást tett a modellnevekről. Tanulság: a reviewer AI megállapításai javaslatok; új függőségről és minden döntésről én döntök, és a reviewer állításait is ellenőrzöm.
