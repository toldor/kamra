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
- **Eszköz:** Claude Code (claude-opus-5-5) – felülvizsgálat, kikérdezés, webes forráskeresés, a FoodKeeper-adatok szkriptes feldolgozása, PoC-mérések (coverlet-küszöb, PostgreSQL null-aritmetika), a reviewk szétválogatása; Antigravity (Gemini 3.1 Pro (High)) – vak trianguláció az ADR-0008-hoz és a B1 kapu-reviewja; GitHub Copilot – PR-review.
- **Prompt összefoglaló:** (1) Nézd meg, valóban jó-e a B kapura adott ajánlás; ha kiegészítenéd, tedd meg; figyelj a fejlesztés és tervezés logikai sorrendjére, és hogy nem hagytunk-e ki valamit. (2) B0 és B1 kikérdezéssel, egy kérdés és ajánlás egyszerre, jóváhagyás után beírva. (3) ADR-0008: vak validációs prompt a Gemininek, majd összevetés. (4) Kapu-review prompt és szétválogatás.
- **AI javaslat összefoglaló:**
  - **Felülvizsgálat:** a B kapu iránya helyes, de a témavezetői levél után három okból módosítandó: idő (okt. 16 / okt. 23-i mérföldkövek), tartalmi hiány (ismételt főzési kérés, részleges felhasználás a receptigénytől elkülönítve, hiány mennyiséggel, tárolás alapegységben), folyamat-hiány (okt. 9-i leadandó, dolgozatírás, elavult `main`); a kapu B0/B1/B2-re bontva, a B0 és a B1 külön branchen (`docs/architecture-02-b0`).
  - **B0:** feature freeze nov. 20., saját befejezési cél dec. 4., döntési pont okt. 23.; fejezetvázlat a repón kívül; a válaszlevélben a tématervben szereplő MI-alapú helyettesítés és adagjavaslat stretch-státuszának kifejezett egyeztetése; README és V-15 javítása; `develop` → `main`.
  - **B1 – elfogadási kritériumok:** kétmezős megerősítés (szükséges / felhasznált), elégtelen készletnél elutasítás csendes levágás helyett; idempotens főzés kérésazonosítóval; hiány mennyiséggel és „van / kell” bontással; verzióellenőrzés a kézi módosításnál.
  - **B1 – adatmodell:** fix dimenzió hozzávalónként, alapegységű tárolás `numeric(12,3)`-mal és a bevitt egységgel; Domain-enum kategóriák; 14 kategória a USDA FoodKeeper-adatokból, a jellemző termékek minimumával (V-18); aktuális mennyiség a tételen + csak hozzáfűzhető napló invariánssal; 0-ra csökkent sor megtartása; saját recept létrehozással és archiválással; főzés-sorok szükséges és felhasznált mennyiséggel; bevásárlójavaslat állapottáblával, minimumszint külön táblában.
  - **ADR-0008 (az AI eredeti javaslata a vak összevetés előtt):** optimista zárolás `xmin`-nel a kézi módosításnál, **pesszimista zárolás (`SELECT … FOR UPDATE`) a főzésnél**, kérésazonosító egyedi kényszerrel, feltételes állapotváltás, upsert. Az összevetés után az AI a főzésnél a Gemini optimista zárolását ajánlotta, a Gemini `Version`-oszlop-javaslatát elutasította (V-19), és pótolta a mindkettejük által kihagyott esetet (egyidejű duplikátumnál a verzióütközés megelőzheti az egyedikulcs-sértést).
  - **API és tesztstratégia:** törlés `PUT`-tal (0-ra csökkentés okkal), főzés-előnézet, kategória-végpont; a lefedettségi kapu a unit tesztprojekten, a contract-diff nem számít e2e-nek; az AI eredetileg saját Cobertura-szkriptet tervezett, mert a coverlet dokumentációja szerint a küszöb nem támogatott – a mérés ezt cáfolta (V-20).
  - **Kapu-review szétválogatása:** 1 valós (bevásárlójavaslat állapotváltása), 1 részben valós (a megerősítés hozzávalónkénti jellege nem volt kimondva; a „vak szerver” állítás téves, V-21), 1 részben valós (recept-archiválás: a végpont scope-többlet, a `RetiredAt` az ADR-0005 miatt kell) – az AI javaslata az archiválási végpont elhagyása volt.
  - **PR-review (GitHub Copilot) szétválogatása:** 5 megállapítás, mind valós: a kérésazonosító frissítéskor is megmaradjon; az ismétlés-ellenőrzés a teljes tartalmat (mennyiségeket is) vesse össze; a null-mennyiség elvész az összeadó upsertben (V-22, mérés); negatív hiány, ha a felhasznált több a szükségesnél – az AI javaslata: a többlet engedett, a hiány nem negatív; a lejárat utólagos szerkesztése átírja a North Start – az AI javaslata: mozgáskori lejárat a naplóban (`ExpiryDateAtMovement`).
- **Érintett fájlok:** `README.md`, `CONTEXT.md`, `docs/00_index.md`, `docs/01_product/scope_contract.md`, `capability_map.md`, `ux_flows.md`, `docs/02_architecture/adr/0008-konkurencia-es-idempotencia.md` (új), `docs/03_design/data_model.md`, `api.md`, `error_handling.md`, `docs/04_quality/test_strategy.md` (új), `test_report.md`, `docs/07_ai/verification_log.md` (V-18–V-22, V-15 javítása), `.github/workflows/ci.yml`; repón kívül: `kamra_architektura_terv.md` (9. fejezet), `dolgozat_fejezetvazlat.md`, `valasz_bilicki_02.md`
- **Mit változtattam / döntésem:** A felülvizsgálatot és a B0/B1/B2 bontást jóváhagytam, a témavezetői ütemezést és a heti 20 órát elfogadtam; a kikérdezés kérdéseinél (B0: Q1–Q3, B1: Q1–Q14) az ajánlást fogadtam el. A javaslattól eltérő döntéseim:
  - **Branch:** a B0-t és a B1-et nem új ágon, hanem a `docs/architecture-02` ágon készítettem; a B2-nek nyitok új ágat.
  - **ADR-0008:** a vak trianguláció után a főzésnél az AI eredeti pesszimista zárolása helyett a Gemini optimista zárolását fogadtam el.
  - **Recept-archiválás (kapu-review):** az AI a végpont elhagyását javasolta; én megtartottam, és a US-3-at kiegészítettem vele.
  - **PR-review:** a Copilot-reviewt magam kértem; mind az öt javítást az AI ajánlása szerint fogadtam el.
  - **Ellenőrzés:** a `secrets` check kötelező voltát magam ellenőriztem a GitHub rulesetben; a Gemini-reviewk után ellenőriztem, hogy nem hozott létre fájlt.

### P-15 – 1. lépcső, okt. 16-i mérföldkő: készlet (US-1), ajánlás (US-3), főzés (US-4 alapút)
- **Dátum:** 2026-10-10 – 2026-10-11
- **Cél:** Az okt. 16-i mérföldkő megvalósítása függőleges szeletekben (S0–S6): működő készletkezelés és bemutatható ajánlás–főzés folyamat öt saját szövegű recepttel, kézzel számolt ellenőrző példákkal (P1–P10). Szakaszonként: mini-spec → jóváhagyás → piros tesztek → implementáció → elő-review → független review (és kritikus modulnál ellenséges teszt) → doksi.
- **Eszköz:** Claude Code (claude-opus-5-5) – terv, mini-specek, tesztek és implementáció, elő-review (`/code-review` két külön subagenttel: kódolási szabályok és spec; `/security-review`), a reviewk szétválogatása, mérések (V-23–V-32); Antigravity (Gemini 3.1 Pro) – szakasz-review S1–S5, ellenséges tesztek (készlet, főzés), PR-review (a Copilot helyett).
- **Prompt összefoglaló:** (1) Implementációs terv az okt. 16-i mérföldkőre szakaszonként: rétegenkénti fájlok, AC- és ADR-lefedés, spec-first tesztek (negatív esetek, két próba-háztartás, ADR-0008 konkurencia), migrációk és seed, doksi, review-kapuk, commitpontok, időbecslés; a nyitott kérdések egyenként, ajánlással. (2) Szakaszonként: „mini-spec” → „mehet” → „zöld mehet” → `/code-review develop` → Gemini-review és ellenséges tesztek → „javítások mehet” → „doksi mehet”. (3) Saját architektúra-javaslataim: Scalar, külön frontend-konténer.
- **AI javaslat összefoglaló:**
  - **Terv (Q1–Q11):** rétegenkénti bontás (Q1); 5 recept; P1–P10 a test_reportban; „ma” = Europe/Budapest; a lejárt tétel a készlet része (Q5); a hibás esetek backendje okt. 16-ra, minimális felülettel (Q6); hash-alapú útválasztás új függőség nélkül; `test:` → `feat:` → `docs:` commitrend; `v0.2.0` a `develop` merge-commitján.
  - **S1 (Domain, seed, migráció):** alapegységű tárolás, csak hozzáfűzhető napló, `xmin`; az elő-review után: a napló `NO ACTION`-nel hivatkozik a tételre, újrabecslés a bevitel napjától, a decimális skála őre ([V-24](verification_log.md)), az üres seed-mezők ellenőrzése.
  - **S2 (készlet API):** a verzióellenőrzés minden más ellenőrzés előtt (a konkurenciateszt mutatta meg); OpenAPI enum- és számkezelés ([V-26](verification_log.md)); Scalar nonce-os CSP-vel ([V-27](verification_log.md)); a review után új okválasztó üzenet és a kérésenként egyedi `traceId` kihagyása az izolációs teszt összevetéséből.
  - **S3 (készlet felület):** a debounce csak a keresőmezőn; az elő-review után 13 javítás (adatvesztés üres mennyiségnél, 409 után friss adat, a11y); a Gemini-review alapján a fókusz visszaadása a dialógus bezárásakor ([V-28](verification_log.md)).
  - **S4 (ajánlás):** a rangsor kulcsai, D1–D4; az elő-review után E1 (a lejárt tétel nem javít a rangsoron) és E2 (üres készletnél nincs ajánlás); magyar kolláció a konténerben ([V-29](verification_log.md)); a Gemini-review alapján a rangsorteszt elkülönítő erejének helyreállítása ([V-30](verification_log.md)).
  - **S5 (főzés):** S5-D1–D5 (összegzés a US-5 nélkül, a felhasznált mennyiség a recept egységében, összegzés ugyanazon az oldalon, csupa 0 mennyiségű főzés engedett, adagszám 1–100); a piros fázis után F1–F6 (`Depleted` oszlop, ismétlés-ellenőrzés a recept betöltése előtt, bármely ütközés után előbb a `requestId` keresése, levonás-sor az alapértelmezett mennyiséghez, nincs aktuális navigációs link); az elő-review után D-a–D-d (pontosság alapegységben, [V-32](verification_log.md); főzés-specifikus ütközés-szöveg; az archivált recept tiltása halasztva; soronkénti mezőhiba); a Gemini-review után E1–E3 (üres `requestId` elutasítva, [V-31](verification_log.md); adagszám-korlát a Domainben; a repository-szintű teszt megjegyzése).
  - **Az AI saját hibái:** két tesztsegéd-hiba (a Layout üres hibarégiója; az e2e űrlapvárakozás), amelyeket az implementáció futtatása fedett fel; a P1-teszt kimaradt a piros fázisból (a doksi írásakor pótolva); kimaradt a helyi `dotnet format` ellenőrzés, a CI piros lett; az S4 E1-tesztje és egy bizonyítási kísérlet (`git stash`) első körben nem különített el.
- **Érintett fájlok:** `src/backend` (Domain: `Quantities`, `Pantry`, `Ingredients`, `Recipes`, `Recommendations`, `Cookings`, `Collation.cs`; Application: `Pantry`, `Ingredients`, `Recommendations`, `Cookings`; Infrastructure: repositoryk, `KamraDbContext`, seed, migrációk `AddIngredientsAndPantryItems`, `AddRecipes`, `AddCookings`; Api: controllerek, `Program.cs`, `AppExceptionHandler.cs`, `openapi.json`); `src/frontend/src` (PantryPage, PantryItemForm, PantryItemEditor, Layout, useHashRoute, RecommendationsPage, CookingPage, API-kliens); `tests` (unit, integrációs – köztük `AdversarialPantryApiTests.cs`, `AdversarialCookingApiTests.cs` –, Vitest, e2e `pantry.spec.ts`, `cooking.spec.ts`); `docs` (capability_map, scope_contract, ux_flows, ADR-0002/0003/0005/0007/0008, data_model, api.md, error_handling, test_report, test_strategy, review_prompts, verification_log V-23–V-32, changelog, ai_manifest, README, `docs/assets` screenshotok); repón kívül: `kamra_1_lepcso_terv.md`
- **Mit változtattam / döntésem:** A tervet és a szakaszok mini-specjeit jóváhagytam; a döntési pontokon (Q2–Q11, D1–D4, E1–E2, S5-D1–D5, F1–F6, D-a–D-d, E1–E3) és a reviewk javítási javaslatainál az ajánlást fogadtam el, a felhasználónak szóló szövegeket egyenként hagytam jóvá. A javaslattól eltérő vagy saját kezdeményezésű döntéseim:
  - **Q1 – bontás:** a javasolt réteg-bontás helyett függőleges szeleteket választottam, hogy csúszásnál is legyen bemutatható, kész funkció.
  - **Q7 – Scalar és Q8 – külön frontend-konténer:** saját javaslataim; a Scalar csak fejlesztői környezetben, a külön konténer okt. 23. után, ADR-0012-vel.
  - **Kézzel számolt példák:** a P1–P10 elvárt értékeit implementálás előtt magam is kiszámoltam, és az AI-val is újraszámoltattam.
  - **Bizonyítékok:** jeleztem, hogy a képernyőképet a repóba kell tenni; kértem, hogy a V-30 tanulsága dokumentálva legyen.
  - **Ellenőrzés:** a CI-futásokat magam követtem, az S5-ben a piros backend-jobot én jeleztem; a Gemini-reviewk után ellenőriztem a munkakönyvtárat – az S2-ben és az S3-ban a tiltás ellenére létrehozott fájlokat (`scratch.diff`, `diff.txt`) töröltem.
  - **Teszt-módosítások:** az AI által kért tesztpontosításokat (S2: `traceId`; S3: gombnév, `waitFor`; S5: hibaüzenet- és űrlapvárakozás, a D-a miatti tesztérték) egyenként jóváhagytam.
  - **PR-review:** a Copilot-kreditkeretem elfogyott, ezért a PR-review-t az Antigravity (Gemini 3.1 Pro) végezte a teljes ág-diffre. Négy megállapításából kettő valós (PR-1 üzenet, PR-3 szóközök), egy részben valós (PR-2 rendezés, [V-33](verification_log.md)), egy téves ([V-34](verification_log.md)); a javításokat az AI ajánlása szerint fogadtam el.

### P-16 – Az okt. 22-i mérföldkő tervezése; T0 (scope és US-5 terv), T1 (bevásárlólista backendje), T4 (a lista bővítése)
- **Dátum:** 2026-10-17
- **Cél:** Az okt. 22-i mérföldkő (kurzus-kiírás: a fő folyamatok végig működnek, első dokumentált telepítés, a dolgozat 1–4. fejezete) implementációs és dokumentációs terve kérdésenként; a témavezető 4. levelének beépítése; a terv első szakaszai: a vállalás szűkítése és új ütemezése (T0), a bevásárlólista, a bevásárlójavaslatok és a minimumszint backendje (T1), az induló recept- és hozzávalólista bővítése (T4).
- **Eszköz:** Claude Code (claude-opus-5-5) – kikérdezés, terv, mini-specek, piros tesztek és implementáció, elő-review (`/code-review` két külön subagenttel: kódolási szabályok és spec; `/security-review`), szétválogatás, doksiszövegek; Antigravity (**Claude Sonnet 4.6**, nem Gemini 3.1 Pro) – szakasz-review és ellenséges tesztek a T1-re.
- **Prompt összefoglaló:** (1) Az átadó dokumentum 10 nyitott kérdése egyenként, opciókkal és indokolt ajánlással, majd a terv a `kamra_1_lepcso_terv.md` mintájára (`kamra_okt22_terv.md`). (2) A témavezető 4. levele után a terv és az okt. 16-i beszámoló módosítása. (3) A T0 mini-spec (M1–M15) és a doksiszövegek jóváhagyásra (`kamra_t0_szovegek.md`). (4) A T1 technikai mini-spec (D1–D7), piros tesztek, implementáció, elő-review, biztonsági review, külső review és ellenséges tesztek szétválogatása, docs (`kamra_t1_docs.md`). (5) A 15 recept és 121 hozzávaló tervezete átnézésre (`kamra_seed_atnezes.md`), majd beépítése (T4).
- **AI javaslat összefoglaló:**
  - **Tervezés (O1–O11, az eredeti ajánlások):** O1 a teljes dolgozat határideje a témavezető nov. 20-a, megerősítéssel a levélben; O2 két ág egymás után (`feature/us-5-shopping-list`, `docs/b2-deploy`); O3 a vállalás alsó határa (20 recept, kb. 150 hozzávaló), P11–P18; O4 teljes minimumszint minimális felülettel (P19); O5 saját recept szerkesztés nélkül, „Új hozzávaló” a választóból; O6 új WSL2-disztró okt. 22-re, másik gép dec. 4-re; O7 LaTeX a repóban (a fejlesztő kérdésére a Google Docs, a Claude Docs és a Word is mérlegelve, végül a Word ajánlva, követett változtatásokkal); O8 két UAT-kör, 3 résztvevő; O9 review a kockázat szerint; O10 egyetlen közös levél okt. 22-én; O11 minden nov. 13. előtt, előre rögzített vágási szabállyal.
  - **A 4. levél után:** belső céldátum (nov. 13.) a témavezetői nov. 20. mellett; 4 fős kipróbálás; az 5. fejezet első része okt. 22-re (O12); új mini-spec pontok: M12 (miért került a javaslat a listára), M13 (mennyi), M14 (megszakadt művelet, mobil).
  - **T0 mini-spec (M1–M15):** csak az *elfogyott* ok vált ki javaslatot (M6); minimumszint beállításakor azonnali javaslat (M1); feloldás a feltétel fölé kerüléskor (M3); **az egyidejű csökkentések write skew-ja (M4), amelyet a tervezés közben az AI vett észre, hozzávalónkénti advisory lockkal**; M2-ként először az összeadást javasolta kipipált sornál is (a fejlesztő kérdésére hibásnak bizonyult, javítva: újraindulás); M10-ként az induló recept archiválásának tiltását (a fejlesztő döntése: archiválható); M15 (nincs javaslat, ha a hozzávaló pipa nélkül a listán van) a P-példák összeállításakor derült ki.
  - **T1:** D1 egyetlen mechanizmus (a zár) a nyers SQL upsert és a feltételes UPDATE helyett; D2–D7; a piros fázisban a migráció kimarad (D7). Implementáció közben: a nullázható `Unit?` elrontotta a közös OpenAPI-sémát → `{ value, unit }` (V-37); az ismételt elfogadás nem volt bájtra azonos (V-36). Az elő-review után D-R1 (elfogadás 204), D-R2 (a recept neve a javaslatban), D-R3 (a PUT megtartja a mennyiséget), az M15 a Domainbe, közös segédfüggvények, a tranzakció lezárása az ismétlés ágán, „javaslat a főzéssel egy tranzakcióban” (`xmin`) teszt. A `/security-review` nem talált sebezhetőséget.
  - **Külső review (Claude Sonnet 4.6):** 18 megállapításból egy valós (nyitott minimumszint-javaslat törölt minimum mellett mennyiség nélkül), a „Kritikus” SQL-injekció és több további állítás téves (V-38, V-39); 8 ellenséges tesztből 7 érvényes és zöld, 1 érvénytelen (V-40).
  - **T4:** 15 recept és 121 hozzávaló saját szöveggel; tervezési szempont, hogy egyik új recept se legyen „majdnem elkészíthető” a P1–P10 tesztkészletekhez képest; beépítés szkripttel a jóváhagyott táblázatból; additív `ExpandSeedData` migráció.
  - **Az AI saját hibái:** a piros fázisban kimaradtak a use-case unit tesztek, és az Application-lefedettség 68,8%-ra esett (20 teszt az implementáció után, mutációval ellenőrizve); a `dotnet format` egy nem forduló fájlon 4 tesztet törölt, a tesztszámból derült ki (V-41); egy saját tesztben kétszer olvasta ki ugyanannak a válasznak a törzsét; egy mutáció (M8) rosszul volt megfogalmazva (rögzített idő mellett ekvivalens maradt), újrafuttatva; a seed-szkript első futása átformázta az `ingredients.json`-t (a diffből derült ki, visszaállítva); az első M2-ajánlás hibás volt.
- **Érintett fájlok:** `src/backend` (Domain: `ShoppingList`, `Ingredients/IngredientMinimum`, `Quantities/Unit`, `Cookings/Cooking`; Application: `ShoppingList`, `Ingredients/SetIngredientMinimum*`, `ListIngredients`, `Pantry/AddPantryItem`, `UpdatePantryItem`, `Cookings/ConfirmCooking`, `Common/IIngredientLocks`; Infrastructure: `ShoppingListRepository`, `IngredientLocks`, `TransactionalSave`, `KamraDbContext`, migrációk `AddShoppingList`, `ExpandSeedData`, `Seed/*.json`; Api: `ShoppingList/*`, `IngredientsController`), `tests` (`ShoppingListTests`, `ShoppingListUseCaseTests`, `ShoppingListCommandTests`, `ShoppingFakes`, `ShoppingListApiTests`, `AdversarialShoppingListApiTests`, `SeedDataTests`, a meglévő fake-ek), `src/frontend/src/api/schema.d.ts`, `docs` (scope_contract, CONTEXT, AGENTS, README, capability_map, ux_flows, ADR-0008, data_model, api, error_handling, test_report, review_prompts, ai_manifest, verification_log V-35–V-43); a repón kívül: `kamra_okt22_terv.md`, `valasz_bilicki_03.md`, `kamra_t0_szovegek.md`, `kamra_t1_docs.md`, `kamra_seed_atnezes.md`, `kamra_gemini_t1.md`, `dolgozat_fejezetvazlat.md`.
- **Mit változtattam / döntésem:** A tervet, a mini-speceket és minden doksiszöveget jóváhagyás után engedtem be; az ajánlásoktól ezekben tértem el:
  - **O1 – határidő:** a kurzus-kiírás dátumait választottam a javasolt nov. 20. helyett; a témavezető 4. levele (amely megerősítette a nov. 20-at) után belső céldátumként tartottam meg a nov. 13-at, hogy legyen tartalék.
  - **O7 – a dolgozat formátuma:** megkérdeztem, írhatunk-e Google Docsba, és hogy az AI tud-e Wordben szerkeszteni; a Wordet választottam, a `python-docx` telepítését engedélyeztem.
  - **O8 – UAT:** 2 főt tudtam biztosítani, és magamat ajánlottam a harmadiknak; az AI a kiírás alapján ezt elvetette. A 4. levél után 4 fő lett (2 + 2 új).
  - **O10 – levelek:** az okt. 16-i beszámolót külön, a terv után küldtem el, a 4. levélre válaszként, és az utolsó két bekezdést (a szűkítés határideje, a januári cél kérdése) kivettem.
  - **M2 és M10:** az M2-nél rákérdeztem, miért adódna össze a kipipált sor mennyisége – az AI ajánlása hibás volt, javította; az M10-nél az induló recept archiválhatóságát választottam a tiltás helyett.
  - **A review után (D-R1–R3):** az ajánlásokat elfogadtam (204, a recept neve, a megtartó PUT).
  - **Seed:** a 15 receptet és a 121 hozzávalót átnéztem, és változtatás nélkül jóváhagytam.
  - **Folyamat:** a T2-t új chatben folytatom; kértem, hogy a CI eredményét push után az AI ellenőrizze. A külső reviewt az Antigravityben Claude Sonnet 4.6-tal futtattam (nem Geminivel), ezért a függetlenség gyengébb; ezt a review_prompts és az ai_manifest rögzíti.

### P-17 – T2: a bevásárlólista felülete, összekötés a recepttel és a főzéssel, R1
- **Dátum:** 2026-10-18
- **Cél:** A US-5 felülete; a „Bevásárlólistára” a receptnél; a hiány a listára és az összegző mondat a főzésnél; a közben fogyott készlet kiemelése (R1).
- **Eszköz:** Claude Code (claude-opus-5-5); review: Claude Code `/code-review` (két ügynök, Claude Opus 5.5).
- **Prompt összefoglaló:** „Folytatjuk… az ALLAPOT.md Következő lépése szerint” → mini-spec jóváhagyásra → „A, a többi is jóváhagyva” → piros tesztek → „mehet az implementáció” → review → javítások jóváhagyással.
- **AI javaslat (eredeti):**
  - **Lelet:** a recept hiánysorában nincs `ingredientId`, tömeges felvétel nincs.
  - **K1:** A) azonosító a hiánysorba és hiányonként egy kérés, részleges hibánál csak a maradék (ajánlott); B) tömeges végpont; C) névalapú kikeresés.
  - **K2–K13:** felépítés, szövegek, R1.
  - **Tesztek:** a spec 15 Vitest-tesztet írt; 23 lett, mert több esetet külön tesztre bontottam.
  - **Implementáció:** vezérelt jelölőnégyzettel; a review után 7 javítás és 3 új szöveg.
- **Érintett fájlok:**
  - backend: `Domain/Recommendations/Recommender.cs`, `Application/Recommendations/GetRecommendations.cs`, `openapi.json`;
  - frontend: `ShoppingListPage.tsx`, `ShortageToList.tsx`, `IngredientPicker.tsx`, `CookingPage.tsx`, `RecommendationsPage.tsx`, `PantryItemForm.tsx`, `App.tsx`, `Layout.tsx`, `useHashRoute.ts`, `format.ts`, `fieldErrors.tsx`, `api/client.ts`, `api/messages.ts`, `api/schema.d.ts`, `index.css`;
  - tesztek: `ShoppingListPage.test.tsx`, `RecommendationsPage.test.tsx`, `CookingPage.test.tsx`, `RecommenderTests.cs`, `ShoppingListApiTests.cs`, `tests/e2e/shopping.spec.ts`;
  - docs: ux_flows, api, capability_map, test_report, verification_log V-44–V-45, `docs/assets/shopping-360.png`;
  - a repón kívül: `t2_minispec.md`, `t2_review.md`, `t2_docs.md`, a terv T2 szakasza.
- **Mit változtattam / döntésem:**
  - **K1:** rákérdeztem, hogy az A opcióval megmarad-e a terv szerinti működés; az AI kimutatta, hogy a scope_contract „egy kattintással”-t kér, a terv „egy kéréssel”-e pontatlan volt. Az A-t választottam.
  - **K2–K13 és a szövegek:** változtatás nélkül jóváhagytam.
  - **A review után:** a javasolt javításokat és a három új szöveget elfogadtam.

### P-18 – T3: saját recept, archiválás, saját hozzávaló
- **Dátum:** 2026-10-23
- **Cél:** Saját recept saját hozzávalóval, bármely recept archiválása a saját háztartásban, az archivált recept főzésének tiltása (M8, D-c); a felület a piszkozattal (M14).
- **Eszköz:** Claude Code (claude-opus-5-5); review: Claude Code `/code-review` (két ügynök, Claude Opus 5.5) és `/security-review`; külső review és ellenséges teszt: Antigravity (Claude Sonnet 4.6).
- **Prompt összefoglaló:** „hozz képbe, hogyan állunk” → „menjen minden az eddigi terv szerint” → mini-spec → „Jóváhagyom” → piros tesztek → a főzésoldal linkjéről döntés („A”) → implementáció → két review → javítások („A”) → külső review és ellenséges teszt → szétválogatás („A”).
- **AI javaslat (eredeti):**
  - **Ütemezés:** a lekésett okt. 20-i döntési pont után a T3 halasztását javasoltam, a kötelező részek (telepítés, dolgozat) előre vételével.
  - **Mini-spec:** N1–N12; a lelet szerint a `GET /recipes/{id}` fölösleges, a D-c már teljesül, a névütközést a use case-nek kell ellenőriznie.
  - **Tervezet:** két új port a meglévő interfészek bővítése helyett; az archiválás nyers SQL `ON CONFLICT`-tal; a megerősítés a böngésző ablakával.
  - **A főzésoldal linkje:** a spec új szöveget írt, a meglévőt nem vettem észre.
- **Érintett fájlok:**
  - backend: `Domain/Recipes/Recipe.cs`, `Domain/Ingredients/Ingredient.cs`, `Application/Recipes/*` (új), `Application/Ingredients/CreateIngredient*.cs`, `IOwnIngredientRepository.cs` (új), `Application/Cookings/PreviewCooking.cs`, `ConfirmCooking.cs`, `CookingErrors.cs`, `Recommendations/IRecipeRepository.cs`, `Infrastructure/Persistence/RecipeRepository.cs`, `IngredientRepository.cs`, `KamraDbContext.cs`, `DependencyInjection.cs`, migráció `AddRecipeArchives`, `Api/Recipes/RecipesController.cs` (új), `Api/Pantry/IngredientsController.cs`, `Program.cs`, `openapi.json`;
  - frontend: `RecipesPage.tsx`, `RecipeForm.tsx` (új), `IngredientPicker.tsx`, `CookingPage.tsx`, `RecommendationsPage.tsx`, `PantryPage.tsx`, `PantryItemForm.tsx`, `App.tsx`, `Layout.tsx`, `useHashRoute.ts`, `fieldErrors.tsx`, `api/client.ts`, `api/schema.d.ts`, `index.css`, `package-lock.json`;
  - tesztek: `OwnRecipeTests.cs`, `CookingUseCaseTests.cs`, `RecipesApiTests.cs`, `AdversarialRecipesAndCookingTests.cs`, `RecipesPage.test.tsx`, `CookingPage.test.tsx`, `RecommendationsPage.test.tsx`, `tests/e2e/recipes.spec.ts`;
  - docs: ux_flows, api, error_handling, data_model, ADR-0003, capability_map, test_report, verification_log V-46–V-51, `docs/assets/recipes-360.png`, `recipes-form-360.png`;
  - a repón kívül: `t3_minispec.md`, `t3_review.md`, `t3_review_promptok.md`, `t3_docs.md`.
- **Mit változtattam / döntésem:**
  - **Ütemezés:** a halasztást elvetettem: „menjen minden az eddigi terv szerint”, a levél egy nappal később mehet.
  - **Mini-spec:** változtatás nélkül jóváhagytam.
  - **A főzésoldal linkje:** a meglévő „Vissza a receptekhez” maradt.
  - **A review és a külső review után:** a javasolt javításokat, a két port megtartását és a „Válassz…” szöveget elfogadtam; az ellenséges tesztek a három tesztkód-javítással bekerültek.
  - **A Vite frissítése:** jóváhagytam a lockfile-frissítést.

### P-19 – T5: telepítés tiszta környezetben, kézi tesztek, UAT-terv, megfeleltetés, fenyegetésmodell
- **Dátum:** 2026-10-23
- **Cél:** Az okt. 22-i mérföldkő dokumentációs és üzemeltetési része: az első dokumentált telepítés tiszta környezetben, mért idővel; kézi tesztjegyzőkönyv; a felhasználói kipróbálás terve; megfeleltetési táblázat; fenyegetésmodell, adatkezelés és licencek; komponensnézet.
- **Eszköz:** Claude Code (claude-opus-5-5). Külső review nem volt (lásd lent).
- **Prompt összefoglaló:** „pusholtam, A” (a feature-PR külön review nélkül) → „mergeltem, a runbook jóváhagyva” → az MT-02 lépésről lépésre („honnan indítsam ezt a parancsot?”, „most a disztróban vagyok?”, „most jó ez?”) → „igen, kiegészítetted a leírást azokkal amikre rá kellett kérdeznem?” → az UAT-terv és a táblázat jóváhagyása → „A” (D1, D2) → a PR #17.
- **AI javaslat (eredeti):**
  - **Feature-PR:** a vágólista 2. pontja szerint külön PR-review nélkül, mert a T1 és a T3 már kapott független reviewt.
  - **Runbook:** a Docker Engine telepítése a hivatalos apt-tároló szerint; a tiszta környezet egy új, saját nevű WSL-disztró (`--name kamra-clean`), a Docker Desktop leállítva.
  - **A tiszta környezet:** azt feltételeztem, hogy az új disztró a Docker Desktop integrációja nélkül tiszta. A WSL alapból hozzáfűzi a Windows-`PATH`-ot, ezért a Docker Desktop `docker` parancsa mégis látszott. Ezt az ellenőrző lépés (`command -v docker`) mutatta ki, a runbook javítva.
  - **UAT:** öt önálló forgatókönyv; az első változatban az S1 készletéből (25 dkg vaj) a főzés után nem lett volna javaslat, így az S3 üres lett volna; a valós induló listán ellenőrizve javítottam (2 dkg vaj, plusz kenyér).
  - **Megfeleltetés:** a US-2-nél egy nem létező végpontnevet írtam (`/intake`); ellenőrzéskor javítva.
  - **Biztonsági doksik:** a leadandó csomag sablonjából kiderült, hogy a runbookból hiányzik a rollback és a két incidens; pótolva. Két döntést igénylő pont: D1 – az adatbázis portja a helyi hálózaton is elérhető volt, ismert példajelszóval; D2 – a projekt licence nem volt eldöntve. A licenceket a csomagok metaadatából olvastam ki ([V-53](verification_log.md)). Két túlzó állítást a kód alapján pontosítottam (a fióktörlés láncolása, a „kipróbált” rollback helyett tervezett MT-04).
- **Érintett fájlok:**
  - docs: `05_security_ops/deploy_runbook.md`, `threat_model.md`, `privacy_licensing.md` (új), `observability.md`; `04_quality/manual_tests.md`, `uat.md`, `traceability.md` (új); `02_architecture/c4_component.md` (új); `00_index.md`, `README.md`, verification_log V-52–V-53; `docs/assets/mt02-shopping-360.png`;
  - kód és egyéb: `docker-compose.yml` (az adatbázis portja `127.0.0.1`-en), `LICENSE` (MIT);
  - a repón kívül: `t5_deploy_runbook.md`, `t5_mt02.md`, `t5_uat.md`, `t5_traceability.md`, `t5_security_docs.md`, `pr_us5.md`; a toborzó üzenet.
- **Mit változtattam / döntésem:**
  - **Feature-PR:** elfogadtam, külön review nélkül mergeltem (PR #16).
  - **MT-02:** a telepítést én végeztem a runbook szerint, tiszta WSL-disztróban; a klóntól a `/health`-ig 1 perc 26 másodperc. Ahol elakadtam, kérdeztem, és kértem, hogy ezek a részletek kerüljenek be a runbookba (PowerShell, a disztró parancssora, `cd ~`, a `wsl.conf` hozzáfűzése, újra belépés).
  - **MT-01:** a saját okt. 17-i kipróbálásomról: kényelmes volt.
  - **UAT-terv és toborzó üzenet:** változtatás nélkül jóváhagytam, és az üzenetet elküldtem a résztvevőknek.
  - **D1, D2:** az ajánlás szerint (az adatbázis portja csak a gépről, MIT licenc).
  - **CI a `docs/**` ágon:** pushra nem indul; a PR megnyitásával futott le, a `ci.yml`-t nem módosítottuk.

### P-20 – T6: a dolgozat 1–4. fejezete
- **Dátum:** 2026-10-23
- **Cél:** Az okt. 22-i mérföldkő dolgozatrésze: az 1–4. fejezet a kari sablon és formai előírás szerint. A szöveg követett változtatásként kerül a fejlesztő fájljába, hogy a fejlesztő változtatásai elkülöníthetők legyenek az MI-szövegtől.
- **Eszköz:** Claude Code (claude-opus-5-5). Segédeszközök: python-docx 1.2.0 (a Word-fájl), Mermaid 11.4.1 és a tesztek Playwright Chromiuma (az ábrák). Külső review még nem volt (lásd lent).
- **Prompt összefoglaló:** „jó a word hova fogod tenni a kamra_dolgozat.docx-t?” → „formai követelmények: <a kari oldal linkje>” → „A, egyenlőre hagyjuk üresen” (a feladatkiírás) → „mehet a többi” → „bezártam” → „commitoltam, … néhány dolgot átírtam az ai szövegből de nagyobb módosításokat később végzek”.
- **AI javaslat (eredeti):**
  - **Folyamat:** a dolgozat saját git-repóban van (`00_dolgozat`). A git a `docx2txt.py`-jal a szöveget hasonlítja össze. Az MI „Claude” szerzőjű követett változtatásként szúrja be a szöveget, a fejlesztő a Wordben fogadja el, írja át vagy veti el. Előbb egy próbafájl készült (`word_proba.docx`, [V-54](verification_log.md)).
  - **Formázás:** a kari Word-sablon szerkezete és a kari weboldal írott formai előírása: Times 12, 1,5-es sorköz, 14/12 pontos címsorok, fejezetenként számozott ábrák és táblázatok, [n] alakú hivatkozás.
  - **Tartalom:**
    - 1. Háttér és piaci áttekintés, 2. Követelmények és hatókör, 3. Tervezés, 4. A determinisztikus mag megvalósítása;
    - 5 ábra Mermaid-forrásból (`abrak/*.mmd`), 4 képernyőkép a `docs/assets`-ből, 7 táblázat, a [3]–[18] források.
  - **Ellenőrzés:** a számadatokat és a szabványokat az eredeti forrásban ellenőriztem ([V-55](verification_log.md)). Kiderült, hogy a repó doksiai az RFC 7807-re hivatkoznak, amelyet az RFC 9457 felváltott ([V-56](verification_log.md)); a dolgozat a 9457-et hivatkozza.
  - **Menet közbeni hibák:**
    - A Wordben nyitva lévő fájlt nem lehetett felülírni. Ezért az új változat külön fájlba került, és a cserére a bezárás után került sor, előtte md5-összevetéssel ellenőriztem, hogy közben nem módosult.
    - Az első ábrák olvashatatlanul kicsik lettek. Javítás: `useMaxWidth: false`, egyszerűsített komponensábra, rövidebb csoportnevek.
  - **Hiba, amely a szövegben maradt:** „Az 3.1. táblázat” és „az 3.2. táblázat” (helyesen: „A”, „a”). A szám mező, de az előtte álló névelő sima szöveg, ezért nem igazodik a számhoz.
- **Érintett fájlok:**
  - a repóban: verification_log V-54–V-56, prompt_log P-20;
  - a repón kívül: `00_dolgozat/kamra_dolgozat.docx`, `00_dolgozat/abrak/` (5 `.mmd` és `.png`), `.gitattributes`, `docx2txt.py`; `04_munkaanyagok/okt22/word_proba.docx`, `t6_docs.md`.
- **Mit változtattam / döntésem:**
  - **A fájl helye:** a dolgozat a `00_dolgozat` saját repójába került, nem a projekt repójába.
  - **A feladatkiírás:** üres hely marad, amíg a témavezető meg nem fogalmazza.
  - **Az 1–4. fejezet:** minden változtatást elfogadtam, öt helyen átírtam a szöveget: pontosabb megfogalmazás („egy háztartás”, „valóban egyszerűbbé” az „először teszi olcsóvá” helyett), rövidebb táblázatcella, és töröltem a munkaórát, a vágási szabályt és a „jelenlegi változatában bemutatott” mellékmondatot. A nagyobb átdolgozást később végzem.
  - **Külső review:** a tartalmi review (Gemini, a review_prompts 6. sablonja) a nagyobb átdolgozásom után, a novemberi változat előtt lesz; amit úgyis átírok, azt most fölösleges reviewztatni.

### P-21 – T7: a 0.3.0-s kiadás és a levél a témavezetőnek
- **Dátum:** 2026-10-23 – 2026-10-24
- **Cél:** Az okt. 22-i mérföldkő lezárása. A kiadás doksija (changelog, a hibamodell szabványhivatkozása, capability_map, test_report, README), a `develop` → `main` merge és a `v0.3.0` tag; a levél a témavezetőnek az okt. 20-i visszajelzéssel és az okt. 22-i csomaggal együtt.
- **Eszköz:** Claude Code (claude-opus-5-5). Külső review nem volt: a PR-ok csak doksit tartalmaztak, a kiadott kód a T1-ben és a T3-ban kapott reviewt.
- **Prompt összefoglaló:** „commitoltam, mehet a T7” → „jóváhagyom, mehet” → „pusholtam, megnyitottam a PR-t” → „mergeltem, megnyitottam a kiadási PR-t, beleírtam az emailbe, exportáltam” → „mergeltem, a tag fent van, commitoltam”.
- **AI javaslat (eredeti):**
  - **Ellenőrzések a tervezet előtt:**
    - mind a 25 OpenAPI-végpont szerepel az `api.md`-ben, mind a 7 migráció a `data_model.md`-ben, és nincs törött relatív link;
    - a diffben nincs titok vagy személyes adat (csak a tesztek `example.com`-os címei);
    - az MT-02 (`7b6a484`) óta a `src` és a `tests` változatlan, csak a `docker-compose.yml` port-kötése változott.
  - **Lelet:** a `v0.2.0` tag a `develop` egy commitján volt, a `main` a `v0.1.0`-n állt, holott a runbook szerint a kiadás a `main`-en van. A mostani merge után a `v0.2.0` is a `main` történetében van, így az MT-04 visszaállása működik.
  - **Doksi:** changelog 0.3.0; RFC 7807 → 9457 négy doksiban, az ADR-0007-ben pontosító sorral ([V-56](verification_log.md)); CAP-10 a runbookkal (*Partial* marad, mert metrikák nincsenek); a test_report a `develop` zöld CI-futásával; README: az 1. lépcső kész. A changelog dátuma 2026-10-24, mert a tag éjfél után készült.
  - **A levél:**
    - egy mondat a csúszásról;
    - válasz a témavezető fő kérdésére: mi történik az elfogadott és az elutasított javaslattal, ha változik a készlet. A kódból (`SuggestionRules.cs`) és a P11–P20 példákból ellenőriztem;
    - a nov. 6-i lépés tarthatósága, három kérdés.
  - **Saját pontatlanság a levéltervezetben:** az egyidejű esetre „öt párhuzamos kérést” írtam. A teszt két párhuzamos kérést futtat, ötször ismételve; a küldés előtt javítva.
  - **A PDF:** a dolgozatban még benne van a két névelőhiba („Az 3.1. táblázat”), ezt jeleztem.
- **Érintett fájlok:**
  - docs: `06_release/changelog.md`, `02_architecture/adr/0007-rest-api-hibamodell.md`, `03_design/api.md`, `03_design/error_handling.md`, `00_index.md`, `01_product/capability_map.md`, `04_quality/test_report.md`, `README.md`;
  - git: PR #18 (`docs/thesis-okt22` → `develop`), PR #19 (`develop` → `main`), `v0.3.0` annotált tag a `main` merge-commitján (`ad2d93a`), a `main` CI-ja zöld;
  - a repón kívül: `t7_release.md`, `level_okt22.md`, `t7_p21.md`; `00_dolgozat` (mezőfrissítés); `kamra_dolgozat_v1_2.pdf`.
- **Mit változtattam / döntésem:**
  - **A kiadás doksija:** változtatás nélkül jóváhagytam.
  - **Merge és tag:** a két PR-t zöld CI után mergeltem, a kiadásit merge committal; a `v0.3.0` taget én tettem fel.
  - **A levél:** a saját kipróbálásról szóló mondatot én írtam („Gördülékeny volt a telepítés.”), a dolgozatot PDF-be exportáltam, a levél első mondatát átírtam, és a levelet 2026-10-24-én elküldtem.
  - **A névelőhibák:** később javítom.

### P-22 – MT-03 és MT-04: az UAT-forgatókönyvek és a visszaállás kipróbálása
- **Dátum:** 2026-10-24 – 2026-10-25
- **Cél:** Az UAT 1. köre (okt. 26.) előtt a fejlesztő végigpróbálja az öt forgatókönyvet a `v0.3.0`-n (MT-03), és kipróbálja a visszaállást a runbook szerint (MT-04).
- **Eszköz:** Claude Code (claude-opus-5-5); az automatikus akadálymentességi ellenőrzéshez a tesztek Playwright-Chromiuma. Külső review nem volt: a változás csak dokumentáció.
- **Prompt összefoglaló:** „Folytatjuk … a »Következő lépés« szerint haladj” → „A, jóváhagyom” → „mielőtt nekiállok … ellenőrizd át, hogy biztosan nem lesz-e valami probléma, pl. elveszik a v0.3.0” → „egy ideig leállt … nyomtam egy ctrl c-t” → „ezek alapján jó a migrator is?” → „írd be te a táblázatba a verziókat” → „megcsináltam az S1–S5-t … a felolvasót csak az S1-nél néztem, a többinél te ellenőrizd” → „A, mind sikerült, eltérés nincs” → „semmit nem mondott, elhallgatott” → „A, nekiállok az MT-04-nek” → „megcsináltam a takarításig, jók voltak az elvárt dolgok” → „jóváhagyom, indíthatom akkor a takarítást?” (a kimenetekkel) → „A, eddig jó a takarítás?” → „hozd létre te az ágat” → „pusholtam … de a CI rajta piros (#73)” → „A”.
- **AI javaslat (eredeti):**
  - **Előzetes ellenőrzés:** a `v0.2.0` → `v0.3.0` migrációk csak hozzáadnak, a `v0.2.0` háztartásra szűr, két várt eltérés ([V-57](verification_log.md)); a mentési parancs bash-szintaxis, ezért Git Bash.
  - **Terv:** külön klón a `v0.3.0`-n, az MT-03 adataira épülő MT-04; az 1. kérdés: a parancsokat a fejlesztő futtatja (ajánlott) vagy a Claude Code.
  - **Ellenőrzőlista** (`04_munkaanyagok\nov06\mt03_mt04_lista.md`): parancsok, a felület pontos szövegei, a sorszámok SQL-je. Írás közben derült ki, hogy a compose-fájl fix projektnevet ad, és a klón a fejlesztői volume-okat használta volna ([V-58](verification_log.md)); javítás: `COMPOSE_PROJECT_NAME` a `.env`-ben, `config`-ellenőrzés.
  - **Kockázat-átnézés a fejlesztő kérésére:** a tagek fent vannak a GitHubon, a klón nem pushol, a `v0.2.0` migrátora nem töröl; az egyetlen veszélyes lépés a takarítás `down -v` parancsa volt, ez teljes útvonalat és `-p kamra-mt` kapcsolót kapott; inkognitó ablak.
  - **Az elakadt build:** nem fagyás volt; a bundle-lépés 273,7 mp alatt kimenet nélkül elkészült (a Claude Code egyszer külön lefuttatta). A `docker compose ps` nem mutatja a kilépett migrátort; helyesen `ps -a` (a listában javítva, a runbookba javaslat).
  - **Akadálymentesség:** az S2–S5 felolvasására három opció (automatikus ellenőrzés – ajánlott, a fejlesztő Narrátorral, csak az S1); az eredmény két lelet (L1, L2, [V-59](verification_log.md)). A javítás idejére két opció: az UAT után, a nov. 13-i változatban (ajánlott), vagy most egy `v0.3.1`-gyel.
  - **MT-04:** a fejlesztő kimenetei alapján PASS, mind a 10 sorszám egyezik mindhárom állapotban (a `v0.2.0`-s futást a Docker build-előzményei is igazolják, a `2deb55e`-ből). A kimenetekből egy új megfigyelés: a `v0.2.0` alatt az adatbázis portja `0.0.0.0`-ra kötött (E3); javaslat egy runbook-figyelmeztetésre.
  - **A piros CI (#73):** nem a doksi okozta; új advisory az `undici`-ra (fejlesztői függőség). Három opció: `npm audit fix` ezen az ágon (ajánlott), külön ág, vagy a dependabot PR #21 előbb ([V-60](verification_log.md)).
- **Érintett fájlok:**
  - docs: `04_quality/manual_tests.md`, `05_security_ops/deploy_runbook.md`, `00_index.md`, `07_ai/verification_log.md` (V-57–V-60), `07_ai/prompt_log.md` (P-21, P-22);
  - build: `src/frontend/package-lock.json` (`undici` 7.30.0 → 7.30.1);
  - a repón kívül: `04_munkaanyagok\nov06\mt03_mt04_lista.md`, `mt03_mt04_jegyzokonyv.md`; az ellenőrző szkript a scratchpadben (nem kerül a repóba).
- **Mit változtattam / döntésem:**
  - **A parancsok:** az MT-04 parancsait én futtattam (1. kérdés, A).
  - **A kockázat-átnézés:** a lista futtatása előtt kértem, hogy nézze át, mi veszhet el.
  - **Az S2–S5 felolvasása:** automatikus ellenőrzés (A); az S1-et én hallgattam meg a Narrátorral.
  - **Az L1 és az L2:** az UAT 1. köre után, a nov. 13-i változatban javul (A).
  - **Az E3:** megfigyelésként a jegyzőkönyvbe, és figyelmeztetés a runbookba (A).
  - **Az `undici`:** `npm audit fix` ezen az ágon, külön commitként (A).
  - **A szöveg:** változtatás nélkül jóváhagytam.

### P-23 – v0.3.1: a fejlesztő UAT előtti módosításai
- **Dátum:** 2026-10-31
- **Cél:** A fejlesztő UAT előtti módosítási listájának (5 pont) ütközésvizsgálata, és a jóváhagyott pontok megvalósítása a nov. 1-i UAT előtt.
- **Eszköz:** Claude Code (claude-opus-5-5), a `/code-review` két ágensével (kódolási szabályok, spec); független review: Antigravity (Gemini).
- **Prompt összefoglaló:** „Folytatjuk … Álljunk át új branchre is.” → a lista (5 pont: „Megfőztem”, „Bevásárlólistára”, „Levonás”, minimumszint, kereső, „szelet” egység) → „A, az 1a, 1b, 2, 3, pontok jók, az 5-t vessük el, a 4 pontot valósítsuk meg akkor is ha bővíti a vállalást” → „A” (oldalanként egy kereső) → „Igen” (terv) → „Igen” (R1, R2) → „commitold meg addold be te most” → az Antigravity-review → „Igen” (#7, #1) → „Igen” (a doksi szövege) → „commitold és pushold te most, a mostani változtatásokról is írj proptlogot és adr-t ha még mem tetted” → „Igen” (ADR-0012).
- **AI javaslat (eredeti):**
  - **Ütközésvizsgálat:** 1a és 1b elfogadható; a 2 módosítva („Levonás (lejárat – mennyiség)” a „Lejárati dátum” helyett, mert a sor a levont mennyiségeket mutatja, FEFO); a 3: a funkció megvan (a Bevásárlólista oldal alján), javaslat: tájékoztató a Készlet oldalon; a 4 bővíti a vállalást, javaslat: az UAT előtt ne, a Receptek névszűrője a nov. 13-i változatba; az 5 ütközik az ADR-0002-vel, a scope_contract 25. sorával és a CONTEXT.md Mértékegység szabályával, javaslat: „kenyérszelet” saját hozzávaló darab dimenzióval.
  - **Az UAT:** `v0.3.1` még aznap, az UAT marad nov. 1-jén (A, ajánlott); a `v0.3.0` a változtatások nélkül (B); csúsztatás (C).
  - **A kereső:** oldalanként, a böngészőben szűrve (A, ajánlott); közös kereső a fejlécben új végponttal (B). Az A leírásában téves állítás: a készletkereső „ékezet nélkül is talál” ([V-61](verification_log.md)).
  - **Terv** (D1–D4, `03_tervek\2026-10-31_v031_terv.md`), piros tesztek, implementáció, mutációs ellenőrzés: az üres levonássor (M7) elsőre túlélt, erre negatív ellenőrzés készült.
  - **ADR:** a böngészős szűrés eltér a készlet szerveroldali keresésétől, ezért ADR-0012 javasolt (a 3 alternatívával és a korláttal).
  - **Review:** `/code-review` – nincs hiba; R1 (a teszt neve többet ígért), R2 (két régi feliratú komment) javítva, R3–R5 elfogadva. Antigravity – 9 megállapítás, szétválogatva, kísérlettel ([V-62](verification_log.md)); a #7 és a #1 alapján két teszt erősödött.
- **Érintett fájlok:**
  - kód: `src/frontend/src/` `CookingPage.tsx`, `PantryPage.tsx`, `RecipesPage.tsx`, `RecommendationsPage.tsx`, `ShoppingListPage.tsx`, `format.ts`; komment: `AddShoppingListItem.cs`;
  - tesztek: `CookingPage`, `PantryPage`, `RecipesPage`, `RecommendationsPage`, `ShoppingListPage` (`.test.tsx`), `tests/e2e/cooking.spec.ts`, `shopping.spec.ts`; komment: `ShoppingListApiTests.cs`;
  - docs: `02_architecture/adr/0012-bongeszos-kereses.md`, `00_index.md`, `scope_contract.md`, `ux_flows.md`, `capability_map.md`, `traceability.md`, `test_report.md`, `uat.md`, `deploy_runbook.md`, `changelog.md`, `README.md`, `verification_log.md` (V-61, V-62), `prompt_log.md` (P-23);
  - a repón kívül: `03_tervek\2026-10-31_v031_terv.md`.
- **Mit változtattam / döntésem:** Elfogadtam az 1a, 1b, 2 (a javasolt szöveggel) és 3 pontot. A 4-et az ajánlással szemben az UAT előtt megvalósíttattam, a vállalás bővítésével. Az 5-öt elvetettem. A commitokat és a pusht kérésemre a Claude Code végezte (Co-Authored-By nélkül).

### P-24 – Design system és új felület (v0.4.0)
- **Dátum:** 2026-10-31 – 2026-11-01
- **Cél:** A felület újratervezése a fejlesztő által választott design szerint, a működés megtartásával; az UAT ezen az új felületen fut.
- **Eszköz:** Claude Code (claude-opus-5-5), a Claude Design-vászon és artifactok („Kamra stílusirányok”), `/code-review` két ágenssel (kódolási szabályok, spec); független review: Antigravity (Gemini).
- **Prompt összefoglaló:** „Szeretnék nagy UI javításokat és újratervezést is csinálni … modern de nem tipikus AI kinézetű webdesign. a claude design erre jó lenne nekem” → „Kezdjük el a design systemet és kódoljuk is le (új branchen), az UAT ez után csináljuk” → „igen” (3 irány) → „A C2 és C3 designokat választottam … Tartsd meg a C2 és C3 designokból a színeket, elrendezést, stílust.” → a 8 ütközésre adott döntések, „jóváhagyom a tervet” → „12: mehet, commitolj te most” → „kell egy kis javítás” (hosszú nevű minimumszint, a kereső helye) → „igen, javítsd mind az F1–F5 pontokat” → az Antigravity-review → „A” (0.4.0) → „Igen” (doksi).
- **AI javaslat (eredeti):**
  - **Módszer:** a Claude-on belüli Design-vászon és Design System artifact (a különálló claude.ai/design 2026. dec. 14-én bezár); három irány: A „Címke”, B „Konyhai jegyzetfüzet”, C „Piaci tábla”; ajánlás: A.
  - **Ütközésvizsgálat a C2/C3 táblákon:** 8 pont (a Receptek elkészíthetőségi szűrője és címkéje, „Megnyitás”/„Szerkesztés”, a listatétel forrása, a szabad szöveges új tétel, a javaslatok gombjai, a soronként szerkeszthető minimumszint, a hiányzó lista-funkciók, a Készlet „Új tétel” űrlapja); mindegyiknél javaslat a mostani működés megtartására.
  - **Terv** (`03_tervek\2026-10-31_design_system_terv.md`): tokenek CSS-változóként, saját tárhelyű betűk (ADR-0013), kontrasztteszt, oldalankénti átépítés; a fejsávon a kék fókuszkeret kb. 2:1-es – ezt a Claude Code a kontrasztszámításnál maga találta.
  - **Review:** a `/code-review` szétválogatva (F1–F5), az Antigravity 8 megállapítása szétválogatva ([V-65](verification_log.md)); verzió: `0.4.0` (A, ajánlott), a US-2 `0.5.0`.
- **Eltérés a folyamattól:** ennél a szakasznál a tesztek az implementáció után készültek, nem előtte; a két regressziós teszt (hosszú nevű minimumszint, a fejsáv hover-kontrasztja) a javítás előtt pirosan futott, de az elsőt csak a `feat:` commit után commitoltuk.
- **Érintett fájlok:**
  - kód: `src/frontend/src/` `index.css`, `Layout.tsx`, `App.tsx`, `AuthForm.tsx`, `CookingPage.tsx`, `PantryPage.tsx`, `PantryItemForm.tsx`, `RecipeForm.tsx`, `RecipesPage.tsx`, `RecommendationsPage.tsx`, `ShoppingListPage.tsx`; `src/frontend/vite.config.ts`; `src/frontend/public/fonts/` (4 woff2, 2 OFL);
  - tesztek: `tokens.test.ts`, `RecipesPage.test.tsx`, `RecommendationsPage.test.tsx`, `tests/e2e/pantry.spec.ts`, `shopping.spec.ts`;
  - docs: `02_architecture/adr/0013-vizualis-rendszer.md`, `00_index.md`, `scope_contract.md`, `ux_flows.md`, `privacy_licensing.md`, `test_report.md`, `uat.md`, `changelog.md`, `README.md`, `docs/assets/` (7 frissített és 3 új képernyőkép), `verification_log.md` (V-63 – V-65), `prompt_log.md` (P-24);
  - a repón kívül: `03_tervek\2026-10-31_design_system_terv.md`, a „Kamra stílusirányok” vászon (privát artifact).
- **Mit változtattam / döntésem:** A C irányt választottam, és a vásznon C2 (mobil) és C3 (asztali) változattá fejlesztettem tovább (rózsa kiemelés, asztali elrendezés). Az ütközéseknél: a Receptek oldalon „Megnyitás”; a „Csak az elkészíthetők” szűrő és a listatétel forrása későbbi továbbfejlesztés; a többi a javaslat szerint. Két hibát én találtam a felületen (a hosszú nevű minimumszint kicsúszott; a kereső helye a Készlet oldalon). A verzió 0.4.0. A commitokat kérésemre a Claude Code végezte.
