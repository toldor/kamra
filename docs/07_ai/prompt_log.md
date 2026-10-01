# AI Prompt Napló

Minden érdemi AI-munkamenet végén javasolj egy bejegyzést (lásd `AGENTS.md` 10. pont).
A **„Mit változtattam / döntésem"** mezőt a fejlesztő tölti ki – az AI nem találja ki.

## Formátum

```
### P-XX – [rövid cím]
- **Dátum:** ÉÉÉÉ-HH-NN
- **Cél:** ...
- **Eszköz:** Claude Code / Gemini CLI / stb.
- **Prompt összefoglaló:** ...
- **AI javaslat összefoglaló:** ...
- **Érintett fájlok:** ...
- **Mit változtattam / döntésem:** [fejlesztő tölti ki]
```

---

### P-01 – Projekt alapdokumentáció létrehozása
- **Dátum:** 2026-09-19
- **Cél:** AGENTS.md, CLAUDE.md és a docs/ könyvtárstruktúra skeleton doksikkal
- **Eszköz:** Claude Code (claude-sonnet-4-6)
- **Prompt összefoglaló:** Hozz létre agents.md-t és claude.md-t; majd hozz létre a docs mappába alap doksikat, az AGENTS.md hivatkozzon rájuk.
- **AI javaslat összefoglaló:** Létrehozta a teljes docs/ struktúrát (00_index, 01_product, 02_architecture/adr, 03_design, 04_quality, 05_security_ops, 07_ai), frissítette az AGENTS.md 9. és 10. szekcióját markdown linkekkel.
- **Érintett fájlok:** `agents.md`, `CLAUDE.md`, `docs/` (11 fájl)
- **Mit változtattam / döntésem:** Módosítottam a felépítésen, fájlok tartalmain.

### P-02 – AI manifest váz
- **Dátum:** 2026-09-24
- **Cél:** Az ai_manifest.md kezdeti váza a v1.2 követelmények (3.J) alapján, a későbbi bővítéshez.
- **Eszköz:** Claude Code (claude-opus-5-5)
- **Prompt összefoglaló:** Készítsd el az ai_manifest.md kezdeti vázát a követelmény-dokumentum és a saját AI-használati segédletem (mindentud.md) alapján.
- **AI javaslat összefoglaló:** 6 szekciós váz: eszközök és verziók, felhasználási területek és munkamódszer, tiltások, kritikus döntések (üres, a fejlesztő tölti ki), kockázatok és kezelésük, tanulságok. A 00_index.md-ben az ai_manifest „Kész (skeleton)” lett.
- **Érintett fájlok:** `docs/07_ai/ai_manifest.md`, `docs/00_index.md`
- **Mit változtattam / döntésem:** Saját inputom alapján készíttettem el a doksit, átnéztem, módosítottam és jóváhagytam.

### P-03 – Vision kidolgozása interjúval, versenytárs-elemzés
- **Dátum:** 2026-09-24 – 2026-09-26
- **Cél:** A vision.md megírása szekciónként, kikérdezéses (grilling) formában; a versenytárs-táblázat elkészítése csak igazolt állításokkal.
- **Eszköz:** Claude Code (claude-opus-5-5), `/grill-with-docs` skill, webkeresés
- **Prompt összefoglaló:** Kérdezz ki a vision.md-hez szekciónként (probléma, persona, értékajánlat, siker definíció, non-goals, kockázatok), egyszerre egy kérdéssel; minden szekció után mutasd meg a szöveget, és csak jóváhagyás után írd be. Alap: tématerv és a témavezetői e-mail.
- **AI javaslat összefoglaló:** 17 kérdés ajánlott válaszokkal. Forrásolt pazarlási adatok (NÉBIH 2025, Eurostat 2023). Két persona, North Star és 3 guardrail, 5 non-goal, 4 kockázat. A glosszáriumba (CONTEXT.md) felvett fogalmak: Háztartás, Tételjavaslat, Készletbevitel, Hamarosan lejáró, Megmentett főzés, Ajánlás, AI-receptötlet. A javasolt egyedi értékeket az ellenőrzés megcáfolta (V-01), ezért kikerültek. Funkciónkénti versenytárs-táblázat készült; a scope_contract és a capability_map az új fogalmakhoz igazodott.
- **Érintett fájlok:** `docs/01_product/vision.md`, `docs/01_product/competitor_analysis.md`, `docs/01_product/scope_contract.md`, `docs/01_product/capability_map.md`, `CONTEXT.md`, `docs/07_ai/verification_log.md`, `docs/00_index.md`
- **Mit változtattam / döntésem:** Personák kidolgozása; alapvető probléma kidolgozása; sok oda-vissza dobálgatás és kikérdezés volt, mire összeállt, ezért folyamatosan változtattam a tartalmon, ahogy a vision.md alakult. A javaslattól eltérő döntéseim:
  - **Persona:** az elsődleges persona teljesen egyedül él (nem párban), a neve Tomi. A másodlagos persona neve Betti, és a frusztrációját lecseréltem: nehéz összeegyeztetni, mi van otthon, és mi lesz mindenkinek jó, ezért nehéz jó receptet találni. Később kivettem a leírásából a Non-goals szekcióval ismétlődő részt.
  - **Probléma:** kiegészíttettem azzal, hogy sokan nehezen döntik el, mit főzzenek abból, ami otthon van; forrás nélkül hagytam.
  - **Értékajánlat:** ellenőriztettem a versenytársakkal való összehasonlítást; mivel nem volt 100%-osan igaz (V-01), kivetettem a „meglévő alkalmazásokhoz képest” részt.
  - **North Star:** a javasolt „lejárat előtt felhasznált készlet aránya” helyett a „hetente megfőzött receptek, amelyek legalább egy hamarosan lejáró tételt felhasználnak” metrikát választottam.
  - **Guardrailek:** a G2 a „4. héten is aktív” helyett „legalább 3 készletbevitel a regisztrációt követő 14 napban” lett, bármilyen készletbevitel számít; a G3 a „tételjavaslatok javítási aránya” helyett a bevitel ideje lett.
  - **Hamarosan lejáró:** 3 helyett 2 nap.
  - **Non-goals:** a javasolt tételek (chat nem módosít, vonalkód, értesítések) helyett a natív mobilalkalmazást és a bolti árakat/online rendelést vettem fel.
  - **Kockázatok:** a javasolt R6 (LLM-függőség, prompt injection) helyett a scope/idő kockázatot választottam, majd később ezt is kivettem. A receptforrásról döntöttem: a felhasználó kézzel is vihet fel receptet, és az AI-tól kért receptötlet elmenthető a receptek közé.
  - **Versenytárs-táblázat:** csak igazolt állítások kerülhettek bele; az egyedi érték hiányát tudott hiányosságként rögzíttettem.

### P-04 – Metrikák mérési terve interjúval
- **Dátum:** 2026-09-26
- **Cél:** A metrics.md megírása a v1.2 sablon (7.6: típus, név, definíció, hogyan mérném) szerint, reális mérési tervvel.
- **Eszköz:** Claude Code (claude-opus-5-5), `/grill-with-docs` skill
- **Prompt összefoglaló:** Kérdezz ki a metrics.md-hez (a vision.md siker definíciójára építve), egyszerre egy kérdéssel, jóváhagyás után írd be.
- **AI javaslat összefoglaló:** 10 kérdés ajánlott válaszokkal: adatforrás (saját domain-adat, nincs külső analitika), North Star vetítése (aktív háztartásonként), célértékek (NS ≥ 1/hét, G1 ≤ 25% 4 hetes ablakkal, G2 ≥ 50%, G3 p50 ≤ 30 mp / p95 ≤ 90 mp / LLM p95 ≤ 5 mp), G1 egységfüggetlen számítása, csökkenési okok (elfogyott / kidobtam / hibás rögzítés), pazarlás definíciója lejárat után, mérési terv (kötelező szintetikus ellenőrzés és tervezett tesztfelhasználói mérés). A percentiliseket (p50/p95) kérésre elmagyarázta. A vision.md, CONTEXT.md, scope_contract.md és capability_map.md az új döntésekhez igazodott.
- **Érintett fájlok:** `docs/01_product/metrics.md`, `docs/01_product/vision.md`, `docs/01_product/scope_contract.md`, `docs/01_product/capability_map.md`, `CONTEXT.md`, `docs/00_index.md`
- **Mit változtattam / döntésem:** Az adatforrást, a North Star vetítését és célértékét, a G2 célértékét, a csökkenési okokat és a mérési tervet a javaslat szerint elfogadtam. A G1 ablakát 4 hétről 2 hétre rövidítettem. A G3 célértékeit átírtam: teljes idő p50 ≤ 40 mp és p90 ≤ 90 mp (p95 helyett), LLM válaszidő p95 ≤ 10 mp. Mielőtt döntöttem, elmagyaráztattam a p50/p95 percentiliseket.

### P-05 – Scope contract kidolgozása interjúval
- **Dátum:** 2026-09-27
- **Cél:** A scope_contract.md átírása a v1.2 követelmények szerint: 3–6 MVP story elfogadási kritériumokkal, 2–4 stretch, korlátok, Definition of Done.
- **Eszköz:** Claude Code (claude-opus-5-5), `/grill-with-docs` skill
- **Prompt összefoglaló:** Kérdezz ki a scope_contract.md-hez szekciónként, egyszerre egy kérdéssel; minden szekció után mutasd meg a szöveget, és csak jóváhagyás után írd be. Alap: tématerv, témavezetői e-mail, vision.md, metrics.md.
- **AI javaslat összefoglaló:** 22 kérdés ajánlott válaszokkal. 6 MVP story (US-1–US-6) három lépcsőben (determinisztikus mag → AI-bevitel → chat), tesztelhető elfogadási kritériumokkal: fix kategórialista becsült lejárattal, átváltható mértékegységek („csomag” nélkül), LLM-kiesés kezelése kézi formra váltással, relatív dátum szabálykészlet, kétcsoportos determinisztikus ajánlás, alaphozzávalók (víz, só, bors), adagskálázás, FEFO levonás negatív készlet nélkül, csak olvasó chat. Javasolt 4 stretch tétel, ütemezés feature freeze-zel, szolgáltató-független LLM-korlátok, Definition of Done (≥ 30 teszt, Domain/Application ≥ 80% lefedettség). A CONTEXT.md bővült: Kategória, Becsült lejárat, Mértékegység, Hozzávaló, Készlettétel, Minimumszint, Bevásárlójavaslat, Alaphozzávaló. A kikérdezés két döntéséből ADR készült (ADR-0002, ADR-0003).
- **Érintett fájlok:** `docs/01_product/scope_contract.md`, `docs/01_product/capability_map.md`, `docs/01_product/vision.md`, `CONTEXT.md`, `docs/02_architecture/adr/0002-fix-atvalthato-mertekegysegek.md`, `docs/02_architecture/adr/0003-kanonikus-hozzavalo-lista.md`, `docs/00_index.md`
- **Mit változtattam / döntésem:** A story-szerkezetet, a kategóriákat, a mértékegységeket, a relatív dátumokat, az ajánlás és a levonás szabályait, a chat-toolokat, az authentikációt, az ütemezést és a DoD-t a javaslat szerint elfogadtam. A javaslattól eltérő döntéseim:
  - **LLM-időtúllépés:** 10 helyett 15 mp.
  - **Bevásárlólista:** az elfogyott hozzávaló nem automatikusan kerül fel, hanem bevásárlójavaslatként, amit a bevásárlólista felületén el lehet fogadni vagy utasítani; kiegészítettem opcionális minimumszinttel, ami szintén csak javaslatot vált ki.
  - **Jelszó-visszaállítás és e-mail-megerősítés:** nem korlátként, hanem a „Hatókörön kívül” listában szerepel.
  - **Stretch:** a javasolt négyből csak hármat tartottam meg (AI-receptötlet mentése, AI-os helyettesítés és adagjavaslat, kipipált tételből készletbevitel); a chat javaslatot kezdeményező toolja hatókörön kívülre került.
  - **Böngészők:** a Safarit kivettem a támogatottak közül.
  - **Időkeret:** megadtam a leadási határidőt (dec. 5.) és a heti kb. 20 órás kapacitást; a havi LLM-költségkeretnek a javasolt 10 000 Ft-ot fogadtam el.

### P-06 – Scope Contract és Definition of Done konzulensi felülvizsgálata
- **Dátum:** 2026-09-27
- **Cél:** A Claude Code által készített tervezési javaslatok (külső LLM API korlátok és Definition of Done) szigorú felülvizsgálata a hivatalos tanszéki követelmények (v1.2 leadandó csomag és v1.0 MI-eszköztár) és az AGENTS.md szerint.
- **Eszköz:** Gemini CLI / Antigravity (Gemini 2.5 Pro) – konzulensi és szenior szoftverarchitekt szerepkörben
- **Prompt összefoglaló:** A két egyetemi követelménydokumentum beolvasása után felülvizsgáltattam a Claude Code javaslatát a külső LLM API kezelésére (Q19: A vs. B opció), a tesztlefedettségi küszöbre (Q22: A, B, C opció), valamint a 4. Definition of Done fejezet tervezetére.
- **AI javaslat összefoglaló:**
  - Q19: Jóváhagyta az „A” opciót (szolgáltató-független `IIngredientParser` interfész, a konkrét modellválasztás külön ADR témája). Felhívta a figyelmet a hiányzó resilience és security korlátokra: kötelező DTO séma-validáció az Application rétegben, max. 15 mp timeout és 1 automatikus újrapróbálkozás, valamint a konfigurációs feature flag (`Llm:Enabled`) költségtúllépés/kiesés esetére.
  - Q22: Támogatta a „B” opciót (Domain és Application rétegre vonatkozó ≥80% sorlefedettség a CI-ban), pontosítva a tiszta adathordozó DTO-k és generált kódok kizárásával, megelőzve az antipattern „test stuffing”-ot.
  - DoD felülvizsgálat: Kiszűrt két kritikus pontlevonási kockázatot a v1.2 scorecard alapján:
    1. A tervezetben lévő 5 ADR helyett minimum 8 ADR szükséges a maximális pontszámhoz (C kategória).
    2. A verification log arányainál 2 teszt / 1 mérés helyett minimum 3 tesztet eredményező és minimum 2 mérésen/PoC-n alapuló bejegyzés a kötelező elvárás (H kategória).
  - Figyelmeztetett a `capability_map.md` kötelező 5 oszlopos struktúrájának és a Productization kategóriájú sorok pótlásának szükségességére.
- **Érintett fájlok:** `docs/01_product/scope_contract.md`, `docs/01_product/capability_map.md`, `docs/07_ai/prompt_log.md`
- **Mit változtattam / döntésem:** A felülvizsgálat javaslatait mérlegeltem: néhányat elfogadtam, a többit megfontolásra félretettem, és a scope_contract, illetve a capability_map véglegesítésénél döntök róluk.

### P-07 – Dokumentációs audit (keresztvalidáció) és a javítások feldolgozása
- **Dátum:** 2026-09-28
- **Cél:** Az eddigi tervezési dokumentumok formai és tartalmi megfelelésének független auditja a v1.2 leadandó csomag és a v1.0 MI-eszköztár alapján, majd a talált hibák szűrése és javítása.
- **Eszköz:** Antigravity (Gemini 3.1 Pro (High)) – audit; Claude Code (claude-opus-5-5) – a megállapítások ellenőrzése és a javítások
- **Prompt összefoglaló:** (1) Antigravity: szigorú bírálóként ellenőrizd a docs/ tervezési dokumentumait a két követelménydokumentum alapján (kötelező fejezetek, számszerű elvárások, evidence, konzisztencia, tipikus hibák, TODO és törött link), fájl:sor és követelmény-idézet hivatkozással, fájlmódosítás nélkül. (2) Claude Code: a jelentés alapján gyűjtsd össze, mit kell ténylegesen módosítani a meglévő dokumentumokban.
- **AI javaslat összefoglaló:** Az audit 17 megállapítást, 13 hiányzó dokumentumot és egy becsült pontmérleget adott. A Claude Code a v1.2 forrásban és a repóban ellenőrizte az állításokat, és négy csoportra bontotta őket: javítandó a saját dokumentumokban (index, capability map, verification log, ai_manifest), javítandó a korábbi skeletonokban (error_handling, mcp_tools, data_model), elutasítandó (a „fiktív modellnevek” állítás téves – V-04; a „család” szóhasználat nem sérti a glosszáriumot), és döntést igénylő (600 órás elvárt ráfordítás vs. a hátralévő 200 óra). A javítások után: törött link nélküli index a hiányzó ux_flows, c4_component és performance sorokkal; V-02–V-04 verifikációs bejegyzések; 4 csak olvasó MCP tool; az INSUFFICIENT_STOCK hibakód törlése; a data model fogalmi javítása; az ai_manifest eszközverziói.
- **Érintett fájlok:** `docs/00_index.md`, `docs/01_product/capability_map.md`, `docs/03_design/error_handling.md`, `docs/03_design/mcp_tools.md`, `docs/03_design/data_model.md`, `docs/07_ai/verification_log.md`, `docs/07_ai/ai_manifest.md`, `docs/07_ai/prompt_log.md`
- **Mit változtattam / döntésem:** A szűrt javítási listát jóváhagytam. A 600 óra kontra 200 óra kérdésben úgy döntöttem, hogy a scope_contractban maradjon a tényleges, hátralévő 200 órás kapacitás. Megadtam az ai_manifest eszközverzióit (Antigravity a Gemini CLI helyett). A kritikus döntések (D-1, D-2) és a tanulságok megfogalmazásához ajánlást kértem, a többit később töltöm ki.

### P-08 – Capability map kidolgozása interjúval
- **Dátum:** 2026-09-29
- **Cél:** A capability_map.md átdolgozása a v1.2 3.C fejezete és a B) Capability Breadth scorecard szerint, a kiemelkedő szint elemeivel (mérés, kockázat, roadmap).
- **Eszköz:** Claude Code (claude-opus-5-5), `/grill-with-docs` skill
- **Prompt összefoglaló:** Kérdezz ki a capability_map.md-hez szekciónként, egyszerre egy kérdéssel; a szekció végén mutasd meg a szöveget, és csak jóváhagyás után írd be.
- **AI javaslat összefoglaló:** 3 kérdés ajánlott válaszokkal: (1) a 17 funkció szintű sor helyett 12 képesség képességnyelven (6 Value, egy-egy MVP story-nként, és 6 Productization: auth és adatelkülönítés, készletnapló, CI, üzemeltethetőség, AI-kiesés kezelése, metrikák), új CAP-01–CAP-12 azonosítókkal; (2) a kötelező 5 oszlopos tábla változatlan, alatta külön „Mérés és kockázat” tábla hivatkozásokkal a metrics.md-re és a vision kockázataira; (3) Roadmap a scope_contract ütemezéséből (lépcső, céldátum, csúszási szabály) és a stretch tételek „miért nem MVP” indoklással.
- **Érintett fájlok:** `docs/01_product/capability_map.md`, `docs/07_ai/prompt_log.md`
- **Mit változtattam / döntésem:** Mindhárom javaslatot elfogadtam, és a teljes tervezetet változtatás nélkül jóváhagytam.

### P-09 – UX flow-k kidolgozása interjúval
- **Dátum:** 2026-09-30
- **Cél:** A ux_flows.md megírása a v1.2 3.D fejezete szerint: 2–3 fő flow, hibahelyzetek, üres állapotok, alap akadálymentesség, a kiemelkedő szinthez edge case-ekkel és mikrocopyval.
- **Eszköz:** Claude Code (claude-opus-5-5), `/grill-with-docs` skill
- **Prompt összefoglaló:** Kérdezz ki a ux_flows.md-hez, egyszerre egy kérdéssel; a végén mutasd meg a szöveget, és csak jóváhagyás után írd be.
- **AI javaslat összefoglaló:** 7 kérdés ajánlott válaszokkal: 3 fő flow Tomi személyével (bevitel, „Mit főzzek?” és főzés, bevásárlójavaslat), a készlet mint kezdőképernyő az üres állapotban a mondatos bevitelre terelve, duplikált készlet és dupla jóváhagyás kezelése (külön készlettétel, „Már van otthon” jelzés, idempotens jóváhagyás), három „Mit főzzek?” üres állapot mikrocopyval, értesítés a bevásárlójavaslatokról (visszajelzés és navigációs jelvény), 4 kötelező hibahelyzet (AI-kiesés, hiányzó egység, hálózati hiba, hibás bejelentkezés) és 2 további edge case, akadálymentességi minimum Lighthouse-ellenőrzéssel.
- **Érintett fájlok:** `docs/01_product/ux_flows.md`, `docs/00_index.md`, `docs/07_ai/prompt_log.md`
- **Mit változtattam / döntésem:** A flow-kat, a kezdőképernyőt, az edge case-eket, a hibahelyzeteket és az akadálymentességi minimumot a javaslat szerint elfogadtam. A javaslattól eltérő döntéseim:
  - **Üres állapot szövegei:** teljesen üres készletnél a gomb „Bevásárlás rögzítése” helyett „Készlet rögzítése”; a majdnem elkészíthető állapot szövege „…egyik recepthez…” helyett „…egyik receptedhez…”.
  - **Bevásárlójavaslat értesítés:** a javasolt visszajelzés és navigációs jelvény helyett csak a keletkezés pillanatában megjelenő visszajelzés (jelvény nélkül).

### P-10 – UX flow-k validálása keresztvalidációval
- **Dátum:** 2026-09-30
- **Cél:** A ux_flows.md független validálása a v1.2 3.D fejezete szerint, majd a reviewer megjegyzéseinek szűrése.
- **Eszköz:** Antigravity (Gemini 3.1 Pro (High)) – validálás; Claude Code (claude-opus-5-5) – a megjegyzések értékelése és a feljegyzések
- **Prompt összefoglaló:** (1) Antigravity: validáld a ux_flows.md-t a követelmények szerint. (2) Claude Code: nézd át a reviewer megjegyzéseit, gyűjtsd össze, mit módosítanál vagy jegyeznél fel, de jóváhagyásig ne módosíts.
- **AI javaslat összefoglaló:** A reviewer a dokumentumot kiemelkedő szintűnek értékelte, és három megjegyzést tett: (1) „hivatalosan engedélyezte” az `@axe-core/playwright` függőséget; (2) egyidejű készletlevonásnál optimista zárolást javasolt; (3) a dupla jóváhagyáshoz backend-idempotenciát kért. A Claude Code értékelése: (1) a függőség jóváhagyása a fejlesztő döntése, nem a revieweré, ADR nem kell hozzá; (2) valós versenyhelyzet (elveszett levonás), adatmodell-döntésként feljegyzendő; (3) a ux_flows.md már tartalmazza (a tételjavaslat állapota idempotencia-kulcs), csak az ismételt jóváhagyásra adott API-válasz nyitott.
- **Érintett fájlok:** `docs/03_design/data_model.md`, `docs/03_design/error_handling.md`, `docs/07_ai/ai_manifest.md`, `docs/07_ai/prompt_log.md`, `docs/00_index.md`
- **Mit változtattam / döntésem:** Az axe-core függőséget nem vezettem be, az akadálymentesség ellenőrzése a Lighthouse-auditon és a kézi bejáráson marad. Az egyidejű levonás és az ismételt jóváhagyás kérdését feljegyeztettem az adatmodell és a hibakezelés ismert hiányosságai közé, későbbi tervezésre, és a tanulságot felvetettem az AI manifestbe.
