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
- **Eszköz:** Antigravity (Gemini 3.1 Pro (High)) – konzulensi és szenior szoftverarchitekt szerepkörben
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

### P-11 – Tervezési dokumentumok konzisztencia-ellenőrzése és lezárása
- **Dátum:** 2026-10-01
- **Cél:** Commit és branch-váltás előtt ellenőrizni, hogy a kikérdezések minden döntése és ötlete bekerült-e a dokumentumokba, és a dokumentumok konzisztensek-e egymással; a következő (architektúra) fázis átadásának előkészítése.
- **Eszköz:** Claude Code (claude-opus-5-5)
- **Prompt összefoglaló:** Nézd meg, hogy a létrehozott md-kbe minden döntésünket és ötletünket feljegyezted-e, és megfelelően dokumentáltál-e mindent; amit nem, azt gyűjtsd össze, és csak jóváhagyás után módosíts. Utána: mi hiányzik még ahhoz, hogy új chatben folytatható legyen az architektúra.
- **AI javaslat összefoglaló:** Hat csoportba szedett lista: (A) egy később hozott döntésnek ellentmondó szövegek (vision „Zárt kör”, AGENTS.md áttekintés és MCP-szabály, api.md fogalmai); (B) a ux_flows döntései közül a scope_contract elfogadási kritériumaiból hiányzók (idempotens jóváhagyás, „Már van otthon” jelzés, újraszámolás megerősítéskor, összegző sor); (C) kétértelmű definíciók (hamarosan lejáró = ma, holnap vagy holnapután; készletbevitel = sikeresen mentett beküldés; törlés és csökkentés csökkenési okkal); (D) hiányzó glosszárium-fogalmak (Készletmozgás-napló, Recept, Főzés, Elkészíthető, Majdnem elkészíthető); (E) rögzítetlen ötletek „Későbbi továbbfejlesztési lehetőségek” szakaszként; (F) apróságok. Az átadáshoz: a develop branch felvétele az AGENTS.md-be, ADR-jelöltlista a 00_indexbe, a munkamódszer és a nyitott ötletek a Claude Code memóriájába.
- **Érintett fájlok:** `AGENTS.md`, `CONTEXT.md`, `docs/00_index.md`, `docs/01_product/vision.md`, `docs/01_product/metrics.md`, `docs/01_product/scope_contract.md`, `docs/03_design/api.md`, `docs/03_design/data_model.md`, `docs/03_design/mcp_tools.md`, `docs/07_ai/ai_manifest.md`, `docs/07_ai/prompt_log.md`
- **Mit változtattam / döntésem:** Minden javasolt módosítást jóváhagytam, a kétértelmű definícióknál (C1–C3) a javasolt megoldást választottam. A P-06 bejegyzés eszközét a valóban használt modellre javíttattam (Antigravity, Gemini 3.1 Pro (High)). Az architektúra-fázist új chatben, a docs/architecture-01 branchen folytatom.

### P-12 – Architektúra A kapu: minőségi attribútumok, ADR-0004–0007, C4 context/container
- **Dátum:** 2026-10-01 – 2026-10-03
- **Cél:** Az architektúra- és tervezési fázis tervének elkészítése, majd az első (A) kapu dokumentumainak kidolgozása kikérdezéssel, keresztvalidációval és vak triangulációval: quality_attributes.md, ADR-0004 (rétegek), ADR-0005 (adatbázis és migráció), ADR-0006 (authentikáció), ADR-0007 (API és hibamodell), c4_context_container.md.
- **Eszköz:** Claude Code (claude-opus-5-5), `/grill-with-docs` skill, webkeresés – tervezés, kikérdezés, a reviewk szétválogatása; GitHub Copilot (gpt-5.6-luna) – review a quality_attributes.md-ről, az azonosító-döntésről és az ADR-0005 tervezetéről; Antigravity (Gemini 3.1 Pro (High)) – vak trianguláció az ADR-0006-hoz, az ADR-0007-hez és a C4-hez, valamint a kapu konzisztencia-reviewja.
- **Prompt összefoglaló:** (1) Készíts tervet az architektúra- és tervezési fázisra, a más modellekkel való validálásra, arra, hogy mi kell a kódolás előtt és mellett, és a döntések dokumentálására; kiindulás a saját dokumentum-sorrend tervem, de el is térhetsz tőle. (2) Kérdezz ki a `/grill-with-docs` skillel dokumentumonként, egyszerre egy kérdéssel; a szöveget jóváhagyás után írd be. (3) Nézd át a másik modellek megjegyzéseit, és csak a valóban szükségeseket építsd be. (4) Írj a Gemininek adható visszaellenőrző promptokat.
- **AI javaslat összefoglaló:**
  - **Terv:** három kapu (A: a walking skeleton előtt; B: az első feature előtt; C: az AI-lépcsők előtt), ADR-ek a döntés esedékességekor, kapunkénti csomagos review és vak trianguláció a nagy kockázatú döntéseknél, kapunként egy prompt log bejegyzés; eltérések a sorrend-dokumentumtól (nem a teljes 2. szakasz a kód előtt, nincs külön known_limitations.md).
  - **quality_attributes.md:** 8 kérdés; 7 attribútum, 3 scenario mérőszámmal (S-1 LLM-kiesés ≤ 31 mp, S-2 más háztartás adata 404, S-3 ajánlás p95 ≤ 300 ms 10 párhuzamos felhasználóval), reflexiós rétegszabály-teszt.
  - **ADR-0004:** 6 kérdés; Clean Architecture, használati esetenként egy osztály, aggregátumonkénti repository, explicit `HouseholdId`, DataAnnotations, gazdag entitások; a MediatR és az AutoMapper licencváltozás miatt kihagyva.
  - **ADR-0005:** 4 kérdés; EF Core 10 + Npgsql 10 + PostgreSQL 18, migrator-szolgáltatás migration bundle-lel, kategóriák a Domain-kódban, JSON → `HasData` seed `SeedKey`-jel, GUID v7.
  - **ADR-0006:** 8 kérdés; cookie-alapú session ASP.NET Core Identity-vel port mögött, azonos origin, `SameSite=Strict` + antiforgery, NIST SP 800-63B-4 szerinti jelszó (15–128 karakter), lockout, IP-alapú rate limit, `HouseholdId` claim, a háztartás-azonosító soha nem az LLM-től.
  - **ADR-0007:** 9 kérdés; REST + controllerek globális antiforgery-szűrővel, `/api/v1`, `AppException` + `IExceptionHandler`, magyar `title` visszaeséssel és frontend-katalógussal, állapotfüggő 200/409 ismételt jóváhagyásnál, 502/503 LLM-hibakódok, build közben generált OpenAPI CI-diffel és generált frontend-típusokkal.
  - **C4:** 5 kérdés; Mermaid flowchart C4-stílusban (a Mermaid C4-szintaxisa kísérleti), 5 container, az API mint MCP-kliens (tervezett), deployment view fejlesztői, Compose és CI környezettel, tervezett VM-célkörnyezettel.
  - **A reviewk szétválogatása:** Copilot 1 (V-05): 2 valós, 2 részben valós, 1 téves; Copilot 2 (azonosító): 5/5 elfogadva, a `SeedKey` szűkítve, és a GUID v7 ezredmásodpercen belüli sorrendjére vonatkozó pontatlanság javítva; Copilot 3 (ADR-0005): 1 téves (csonkolt másolat), 5 elfogadva vagy részben elfogadva. Gemini vak trianguláció: ADR-0006 – egyezés a mechanizmusban, eltérés a fiókzárolásban és a felhasználókezelés mélységében, elavult NIST-állítás (V-06); ADR-0007 – egyezés, kivéve a Minimal API-t (a `RequireAntiforgery()` JSON-kérést nem utasít el, forráskóddal ellenőrizve) és a Result-mintát; C4 – egyezés, a Migrator kimaradt nála, a „PII nélkül” címke elutasítva. Kapu-review: ellentmondás nélkül, 3 bizonyíthatósági kiegészítés (session-megmaradás újraindítás után, `errors`-tartalom, fail-fast konfiguráció teszt).
  - A licenc-, verzió- és API-állításokat (MediatR, EF Core, Npgsql, PostgreSQL, `Guid.CreateVersion7`, Identity-alapértelmezések, NIST, antiforgery, OpenAPI) forrással ellenőrizte.
- **Érintett fájlok:** `docs/02_architecture/quality_attributes.md`, `docs/02_architecture/c4_context_container.md`, `docs/02_architecture/adr/0004-clean-architecture-retegek.md`, `docs/02_architecture/adr/0005-postgresql-ef-core-migraciok.md`, `docs/02_architecture/adr/0006-cookie-auth-identity.md`, `docs/02_architecture/adr/0007-rest-api-hibamodell.md`, `docs/01_product/scope_contract.md`, `docs/03_design/error_handling.md`, `docs/03_design/api.md`, `docs/07_ai/verification_log.md`, `docs/07_ai/ai_manifest.md`, `docs/00_index.md`, `AGENTS.md`; repón kívül: `kamra_architektura_terv.md`
- **Mit változtattam / döntésem:** A tervet jóváhagytam, és elfogadtam, hogy az 1. lépcső 3–4 napot csúszik. A kikérdezések javaslatait elfogadtam, a reviewk szétválogatását jóváhagytam, és az A kapu ADR-jeit Accepted státuszba tettem. A javaslattól eltérő döntéseim és saját kezdeményezéseim:
  - **Kikérdezési forma:** a sima kérdések helyett a `/grill-with-docs` skill formátumát kértem.
  - **Scenariók:** a javasolt három helyett először csak kettőt (S-1, S-2) választottam az időkeret miatt; miután kiderült, hogy a teljesítménymérés a D kategória miatt amúgy is elkészül, az S-3-at visszavettem.
  - **Rákérdeztem**, mielőtt döntöttem: mire való a MediatR, illetve rendben vannak-e az EF Core és az Npgsql licencei.
  - **Keresztvalidáció:** a három Copilot-reviewt saját kezdeményezésemre kértem; a vak triangulációt a tervben szereplő ADR-0006 mellett az ADR-0007-re és a C4-re is kiterjesztettem.
  - **Verification log:** a MediatR-licencre (javasolt V-06) és a Gemini Minimal API-állítására (javasolt V-07) javasolt bejegyzést nem vettem fel.
  - **Fiókzárolás:** a Gemini DoS-érve után is a lockout megtartása mellett döntöttem, a kockázatot maradó kockázatként rögzítettem.

### P-13 – Walking skeleton: implementációs és folyamatterv, megvalósítás szakaszonkénti reviewval
- **Dátum:** 2026-10-03 – 2026-10-04
- **Cél:** A walking skeleton megtervezése és megvalósítása (végponttól végpontig futó rendszer üzleti funkció nélkül: rétegek, hibamodell, naplózás, adatbázis, cookie-alapú auth, React SPA, Docker Compose, CI) az [ADR-0004–0007](../02_architecture/adr/) alapján, szakaszonkénti review-kapukkal, a v1.2 bizonyíték-elvárásainak megfelelően.
- **Eszköz:** Claude Code (claude-opus-5-5) – tervezés, implementáció, tesztek, elő-review (`/code-review`, `/security-review` skillek), a reviewk szétválogatása, mérések; Antigravity (Gemini 3.1 Pro (High)) – szakaszonkénti független kódreview (S1–S4) és ellenséges tesztek az auth-modulra; gitleaks – secret-szkennelés.
- **Prompt összefoglaló:** (1) Walking skeleton implementációs terv a kamra_architektura_terv.md 8. szakasza és az ADR-0004–0007 alapján. (2) Terv arra, hogyan zajlik a fejlesztés: szakaszok, review és ellenőrzés konkrét eszközökkel és promptokkal, a v1.2-nek megfelelően; mindig mondd meg, mi következik és mit csináljak. (3) Szakaszonként: mini-spec → jóváhagyás → implementáció → kapuk → elő-review → Gemini-review → szétválogatás → javítás jóváhagyás után. (4) Push előtt ellenőrizd, van-e olyan információ, amit nem szabad pusholni.
- **AI javaslat összefoglaló:**
  - **Implementációs terv:** 8 lépés (solution, Api-alapok, adatbázis, auth, frontend, Docker Compose, CI, dokumentáció); négy döntési kérdés: Q1 FluentAssertions 7.x rögzítése (a 8.x kereskedelmi licencű, V-07), Q2 Playwright füstteszt már a skeletonban, Q3 zárolt fiókra külön 429 `LOGIN_LOCKED_OUT`, Q4 naplózás – az AI eredeti ajánlása a beépített `AddJsonConsole` volt (függőség nélkül), egy második modell reviewja után a lapos JSON miatt a Serilogra váltott (ADR-0011).
  - **Folyamatterv:** szerepek (Claude Code implementál és nem commitol; Antigravity független reviewer és ellenséges tesztelő; a fejlesztő dönt és commitol), hat szakasz (S1–S6) review-kapukkal, commitonkénti AI-review checklist, verziózott promptsablonok ([review_prompts.md](review_prompts.md)), bizonyíték-térkép a v1.2-höz, kiegészítésként dependabot és `v0.1.0` tag.
  - **Megvalósítás és review-eredmények:**
    - S1 (solution, rétegszabály-tesztek): a Gemini egy valós .NET 10-beállítást „hallucináltnak” nevezett (V-08, FAIL); az xUnit v3 4.x csak a Microsoft Testing Platformon fut, ezért a VSTest-csomagok kikerültek.
    - S2 (hibamodell, Serilog, `/health`): a tesztek megtalálták, hogy a kivételkezelő törli a válaszfejléceket (a `correlationId` `OnStarting`-ben); a keretrendszer-hibák magyar címet és kódot kapnak egy helyen.
    - S3 (adatbázis, auth, spec-first: 16 piros teszt → zöld): a biztonsági review nem talált sérülékenységet, a Gemini kódreviewja sem; az ellenséges tesztek két valódi hibát találtak: párhuzamos kérésekkel megkerülhető fiókzárolás (V-10) és kijelentkezés után érvényes cookie (V-11).
    - S4 (OpenAPI, React SPA, Playwright): az e2e megmutatta, hogy az AI egy korábbi javaslata (antiforgery cookie `Secure = Always`) HTTP-n megakadályozza a bejelentkezést (V-12, FAIL); a 360 px-es ellenőrzés egy 7 px-es túlcsordulást talált; a `Secure` session-cookie `http://localhost`-on működik (V-13).
    - S5 (Docker Compose, CI): a session túléli az api újraindítását (V-14); az első CI-futás zöld.
    - S6: időmért indítás friss klónból 122 mp (V-16), secret-szkennelés három rétegben (V-15), dokumentáció.
    - PR-review: a GitHub Copilot automatikus reviewja kvótahiány miatt elmaradt, a kézzel kért „lite” review öt megállapításából kettő valós hiba volt (a folyamatszintű bejelentkezési zár minden fiókot sorba állított → fiókonként „csíkozott” zár; az eldobott security-stamp eredmény → a kijelentkezés hibát jelez, ha nem sikerül), egy dokumentációs elmaradás (observability), egy már elfogadott kockázat (kivétel a naplóban, ADR-0011), egy téves (V-17). Az Antigravity S6 konzisztencia-reviewja két ellentmondást talált (meg nem valósított LLM/MCP-változók a `.env.example`-ben, elavult mondat a capability mapben).
  - A licenc-, verzió- és API-állításokat (FluentAssertions, xUnit/MTP, Npgsql `Include Error Detail`, GitHub Actions-verziók, `coverlet.MTP`) forrással vagy kísérlettel ellenőrizte.
- **Érintett fájlok:** a teljes `src/` és `tests/` (új), `Dockerfile`, `docker-compose.yml`, `.dockerignore`, `.github/workflows/ci.yml`, `.github/dependabot.yml`, `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, `.gitattributes`, `.gitignore`, `.env.example`, `dotnet-tools.json`, `KamraApp.slnx`, `README.md`, `AGENTS.md`, `docs/02_architecture/adr/0004`–`0007` (evidence), `0006` (dátumozott kiegészítés), `0011` (új), `docs/01_product/capability_map.md`, `docs/01_product/ux_flows.md` (H5), `docs/03_design/api.md`, `data_model.md`, `error_handling.md`, `docs/04_quality/test_report.md`, `docs/05_security_ops/observability.md`, `docs/06_release/changelog.md`, `docs/07_ai/ai_manifest.md`, `review_prompts.md`, `verification_log.md` (V-07–V-16), `docs/00_index.md`, `docs/assets/`; repón kívül: `kamra_walking_skeleton_terv.md`, `kamra_architektura_terv.md`
- **Mit változtattam / döntésem:** A terveket jóváhagytam; a Q1–Q3-nál az ajánlást fogadtam el. A javaslattól eltérő döntéseim és saját kezdeményezéseim:
  - **Q4 naplózás:** egy második modell (Antigravity) reviewja után a Serilogot választottam az AI eredeti, függőség nélküli javaslata helyett; rákérdeztem, hogy az AI továbbra is a beépítettet ajánlaná-e.
  - **Munkamód:** kértem, hogy minden lépésnél mondja meg, mi következik és mit kell tennem; a commitokba nem kérek `Co-Authored-By` trailert; a megértés-ellenőrző kérdéseket kihagytam.
  - **Review:** a reviewer által létrehozott fájlokat (`diff.txt`, `diff.patch`) töröltem, a próbafájl létrehozását megtagadtam; a modellválasztásnál a Gemini 3.1 Pro mellett maradtam.
  - **K1–K3 (ellenséges teszt):** a lockout-megkerülést javíttattam, a felderítési tesztet a kérésazonosítók kihagyásával fogadtam el, és az ADR-0006 elfogadott kockázata helyett a kijelentkezéskori teljes session-visszavonást választottam (ai_manifest D-3).
  - **Lockout-teszt:** jóváhagytam, hogy a teszt az Identity tényleges viselkedéséhez (az 5. hibánál zárol) igazodjon.
  - **S4:** utólag jóváhagytam az antiforgery `SecurePolicy` visszaállítását és a build közbeni OpenAPI-generálás helykitöltő connection stringjét; elfogadtam a Gemini kritikáját, hogy szándékos kijelentkezésnél ne jelenjen meg „kiléptettünk” üzenet.
  - **PR-review:** a Copilot-kvóta elfogyása után „lite” reviewt kértem; a kivétel-naplózást dokumentált kockázatként hagytam, a többi valós megállapítás javítását jóváhagytam; a javítások a PR mergelése előtt ugyanazon a branchen készültek.
  - **Push előtt** secret-ellenőrzést kértem; a secret-szkennelést három rétegben választottam (kézi gitleaks-futás, gitleaks a CI-ban, GitHub Secret Protection és Push protection, amelyet magam kapcsoltam be); beállítottam a `develop` és `main` branch-védelmét.

### P-14 – B kapu felülvizsgálata a témavezetői levél után; B0 (ütemezés) és B1 (1. lépcső tervezése)
- **Dátum:** 2026-10-08 – 2026-10-09
- **Cél:** Az architektúra-terv B kapujának ellenőrzése a témavezető 2026. okt. 4-i levele alapján (logikai sorrend, hiányok), majd a B0 (ütemezés, a dolgozat fejezetvázlata, válaszlevél, README) és a B1 (az 1. lépcső elfogadási kritériumainak pontosítása, adatmodell, konkurencia-ADR, végpontok, hibakódok, tesztstratégia) elkészítése kikérdezéssel, vak triangulációval és kapu-reviewval.
- **Eszköz:** Claude Code (claude-opus-5-5) – felülvizsgálat, kikérdezés, webes forráskeresés, a FoodKeeper-adatok szkriptes feldolgozása, PoC-mérés (coverlet-küszöb), a reviewk szétválogatása; Antigravity (Gemini 3.1 Pro (High)) – vak trianguláció az ADR-0008-hoz és a B1 kapu-reviewja.
- **Prompt összefoglaló:** (1) Nézd meg, valóban jó-e a B kapura adott ajánlás; ha kiegészítenéd, tedd meg; figyelj a fejlesztés és tervezés logikai sorrendjére, és hogy nem hagytunk-e ki valamit. (2) B0 és B1 kikérdezéssel, egy kérdés és ajánlás egyszerre, jóváhagyás után beírva. (3) ADR-0008: vak validációs prompt a Gemininek, majd összevetés. (4) Kapu-review prompt és szétválogatás.
- **AI javaslat összefoglaló:**
  - **Felülvizsgálat:** a B kapu iránya helyes, de a témavezetői levél után három okból módosítandó: idő (okt. 16 / okt. 23-i mérföldkövek), tartalmi hiány (ismételt főzési kérés, részleges felhasználás a receptigénytől elkülönítve, hiány mennyiséggel, tárolás alapegységben), folyamat-hiány (okt. 9-i leadandó, dolgozatírás, elavult `main`); a kapu B0/B1/B2-re bontva, a B0 és a B1 külön branchen (`docs/architecture-02-b0`).
  - **B0:** feature freeze nov. 20., saját befejezési cél dec. 4., döntési pont okt. 23.; fejezetvázlat a repón kívül; a válaszlevélben a tématervben szereplő MI-alapú helyettesítés és adagjavaslat stretch-státuszának kifejezett egyeztetése; README és V-15 javítása; `develop` → `main`.
  - **B1 – elfogadási kritériumok:** kétmezős megerősítés (szükséges / felhasznált), elégtelen készletnél elutasítás csendes levágás helyett; idempotens főzés kérésazonosítóval; hiány mennyiséggel és „van / kell” bontással; verzióellenőrzés a kézi módosításnál.
  - **B1 – adatmodell:** fix dimenzió hozzávalónként, alapegységű tárolás `numeric(12,3)`-mal és a bevitt egységgel; Domain-enum kategóriák; 14 kategória a USDA FoodKeeper-adatokból, a jellemző termékek minimumával (V-18); aktuális mennyiség a tételen + csak hozzáfűzhető napló invariánssal; 0-ra csökkent sor megtartása; saját recept létrehozással és archiválással; főzés-sorok szükséges és felhasznált mennyiséggel; bevásárlójavaslat állapottáblával, minimumszint külön táblában.
  - **ADR-0008 (az AI eredeti javaslata a vak összevetés előtt):** optimista zárolás `xmin`-nel a kézi módosításnál, **pesszimista zárolás (`SELECT … FOR UPDATE`) a főzésnél**, kérésazonosító egyedi kényszerrel, feltételes állapotváltás, upsert. Az összevetés után az AI a főzésnél a Gemini optimista zárolását ajánlotta, a Gemini `Version`-oszlop-javaslatát elutasította (V-19), és pótolta a mindkettejük által kihagyott esetet (egyidejű duplikátumnál a verzióütközés megelőzheti az egyedikulcs-sértést).
  - **API és tesztstratégia:** törlés `PUT`-tal (0-ra csökkentés okkal), főzés-előnézet, kategória-végpont; a lefedettségi kapu a unit tesztprojekten, a contract-diff nem számít e2e-nek; az AI eredetileg saját Cobertura-szkriptet tervezett, mert a coverlet dokumentációja szerint a küszöb nem támogatott – a mérés ezt cáfolta (V-20).
  - **Kapu-review szétválogatása:** 1 valós (bevásárlójavaslat állapotváltása), 1 részben valós (a megerősítés hozzávalónkénti jellege nem volt kimondva; a „vak szerver” állítás téves, V-21), 1 részben valós (recept-archiválás: a végpont scope-többlet, a `RetiredAt` az ADR-0005 miatt kell) – az AI javaslata az archiválási végpont elhagyása volt.
- **Érintett fájlok:** `README.md`, `CONTEXT.md`, `docs/00_index.md`, `docs/01_product/scope_contract.md`, `capability_map.md`, `ux_flows.md`, `docs/02_architecture/adr/0008-konkurencia-es-idempotencia.md` (új), `docs/03_design/data_model.md`, `api.md`, `error_handling.md`, `docs/04_quality/test_strategy.md` (új), `test_report.md`, `docs/07_ai/verification_log.md` (V-18–V-21, V-15 javítása), `.github/workflows/ci.yml`; repón kívül: `kamra_architektura_terv.md` (9. fejezet), `dolgozat_fejezetvazlat.md`, `valasz_bilicki_02.md`
- **Mit változtattam / döntésem:** A felülvizsgálatot és a B0/B1/B2 bontást jóváhagytam, a témavezetői ütemezést és a heti 20 órát elfogadtam; a kikérdezés kérdéseinél (B0: Q1–Q3, B1: Q1–Q14) az ajánlást fogadtam el. A javaslattól eltérő döntéseim:
  - **Branch:** a B0-t és a B1-et nem új ágon, hanem a `docs/architecture-02` ágon készítettem; a B2-nek nyitok új ágat.
  - **ADR-0008:** a vak trianguláció után a főzésnél az AI eredeti pesszimista zárolása helyett a Gemini optimista zárolását fogadtam el.
  - **Recept-archiválás (kapu-review):** az AI a végpont elhagyását javasolta; én megtartottam, és a US-3-at kiegészítettem vele.
  - **Ellenőrzés:** a `secrets` check kötelező voltát magam ellenőriztem a GitHub rulesetben; a Gemini-reviewk után ellenőriztem, hogy nem hozott létre fájlt.
