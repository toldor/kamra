# Önértékelés

A leadandó csomag v1.2 4.5. pontja szerinti kötelező önértékelés, a 6.3. pont 100 pontos scorecardja alapján. Állapot: 2026-11-21, a `v0.8.0` kiadás után (`main` @ `1efc36a`). Az első változatot az MI javasolta ([P-31](../07_ai/prompt_log.md)); a pontszámok a fejlesztő értékelését tükrözik, a leadás előtt felülvizsgálva.

## Kapu feltételek

| Kapu | Teljesül? | Evidence |
|---|---|---|
| Futtathatóság: a Quickstart alapján indul | Igen | [README.md](../../README.md) „Gyors indítás”; tiszta környezetben 1 p 26 mp ([manual_tests.md](../04_quality/manual_tests.md) MT-02) |
| CI zöld a `main` ágon | Igen | a `v0.8.0` kiadás `main`-je (`1efc36a`): [run 44632445957](https://github.com/toldor/kamra/actions/runs/44632445957) |
| Nincs titok a repóban | Igen | gitleaks a teljes historyra minden CI-futásban ([ci.yml](../../.github/workflows/ci.yml)); [.env.example](../../.env.example) érték nélkül |
| Legalább 30 automatizált teszt | Igen | 1220 teszt ([test_report.md](../04_quality/test_report.md)) |
| AI-átláthatóság | Igen | [ai_manifest.md](../07_ai/ai_manifest.md), [prompt_log.md](../07_ai/prompt_log.md), [verification_log.md](../07_ai/verification_log.md) |

## Pontok kategóriánként

| Kategória | Önpont | Indoklás | Mit csinálnék még +10 órában? |
|---|---|---|---|
| A) Product & Scope (12) | 11,5 | A vision, a scope contract elfogadási kritériumokkal és DoD-vel, a mérőszámok és az ismert korlátok készek, a hatókör változásai naplózva. A mérőszámok csak szintetikus adaton igazoltak. | A 2 hetes tesztfelhasználói mérés, és a négy mérőszám valódi adaton. |
| B) Capability Breadth (10) | 9,5 | 12 képesség (6 Value, 6 Productization), mind Done, evidence- és tesztlinkkel, a hibautakkal együtt. A képernyőkép-evidence egy része a `v0.8.0` előtti felületet mutatja. | Az evidence-képek frissítése az új felületről, a régi példákat (P2, P7, P13) megtartva. |
| C) Architecture & Decisions (13) | 12,5 | C4 kontextus-, konténer- és komponensábra, 12 ADR alternatívákkal és verifikációval, 7 minőségi attribútum 3 forgatókönyvvel, telepítési nézet. A célkörnyezet (VM, TLS-proxy) csak terv. | A célkörnyezet kipróbálása egy Linux VM-en, és az ADR-ek verifikációs linkjeinek átnézése. |
| D) Engineering Quality (15) | 14 | Rétegszabály-teszt, egységes hibamodell stabil kódokkal, validáció az Application rétegben, induláskor ellenőrzött konfiguráció, lint és formázás a CI-ban, reszponzív és akadálymentes felület. A teljesítmény-alapmérés kész, de szűk keresztmetszetet nem javítottam, mert a cél bőven teljesül ([performance.md](../04_quality/performance.md)). | A párhuzamos terhelés okának mérése (adatbázis-körök kérésenként) és a receptjelöltek gyorsítótárazása mérés előtt és után. |
| E) Testing & Quality Gates (15) | 15 | 1220 teszt értelmes mixben, sok negatív esettel; valódi PostgreSQL a Testcontainersben; végpontok közti tesztek két nézetben; mutációs ellenőrzés, ellenséges biztonsági tesztek, OpenAPI-diff, teljesítménymérés; az instabil tesztek dokumentálva. | Az ismert instabil Vitest-teszt javítása és egy automatizált akadálymentességi ellenőrzés (axe) az e2e-ben. |
| F) DevOps & Operations (15) | 14,5 | Teljes CI (build, formázás, lint, tesztek, lefedettségi kapu, sérülékenységvizsgálat, Compose-stack + e2e), lockfile-ok, konténeres build, deploy runbook mentéssel, frissítéssel és kipróbált visszaállással (MT-04), 3 incidens, strukturált napló, health check és 3 metrika-definíció. Egyetlen, helyi környezet van. | Egy második, távoli környezet (VM) dokumentált telepítése és egy egyszerű riasztás a `/health`-re. |
| G) Security, Privacy, Licensing (10) | 10 | 14 fenyegetés STRIDE szerint, mitigációval és teszttel; adatelkülönítés minden végponton tesztelve; gitleaks, NuGet- és npm-audit; az AI-adatküldés szabálya és a fizetős keret dokumentált. A `picomatch` fejlesztői figyelmeztetése elfogadott kockázat (V-99). | A javított `picomatch` megjelenésekor a teljes npm-ellenőrzés visszaállítása blokkolóra; fióktörlés a felületen. |
| H) AI Engineering Maturity (10) | 10 | Manifest szerepekkel, 35 prompt-bejegyzés kulcslistával, 104 verifikáció (tesztekkel és mérésekkel, PASS és FAIL is), commitonkénti AI-review checklist, tanulságok a tévedésekről. | Egy független (nem Claude-családú) review a CAP-12-re és a dolgozat MI-fejezetére. |
| Bónusz (10) | 10 | Funkciókapcsoló (`Llm:Enabled`); időkorlát és újrapróbálás az LLM-hívásoknál, kiesésnél működő mag; akadálymentesség (billentyűzet, felolvasó, kontraszttesztek, az UAT két javítása); rate limiting háztartásonként és IP-nként; post-mortem ([postmortem.md](../04_quality/postmortem.md)). | – |
| **Összesen** | **97 + 10** | | |

## A leadásig nyitott pontok

- A felhasználói kipróbálás 2. köre és az eredményei ([uat.md](../04_quality/uat.md)).
- A demóforgatókönyv időmért, kézi végigjátszása ([demo_script.md](demo_script.md)).
- Ellenőrzött telepítés egy másik gépen a runbook szerint (az [MT-02](../04_quality/manual_tests.md) a fejlesztő gépén, tiszta WSL-környezetben futott).
- A végleges verzió megjelölése (taggel) a javítások és a dokumentáció lezárása után.
