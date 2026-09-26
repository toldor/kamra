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
