# AI Ellenőrzési Napló

Ha az AI biztonsági, teljesítménybeli, helyességi vagy licencelési állítást tesz, ide kerül egy bejegyzés (lásd `AGENTS.md` 10. pont).

## Formátum

```
### V-XX – [állítás rövid leírása]
- **Dátum:** ÉÉÉÉ-HH-NN
- **Állítás:** ...
- **Kockázat:** Alacsony / Közepes / Magas
- **Ellenőrzési módszer:** ...
- **Eredmény:** ...
- **Következtetés:** ...
```

---

### V-01 – A termék megkülönböztető értéke a meglévő appokhoz képest
- **Dátum:** 2026-09-26
- **Állítás:** A vision.md értékajánlatához az AI (Claude Code) két megkülönböztető állítást javasolt: (1) a versenytársak fotóval, vonalkóddal, blokkszkenneléssel vagy tételenkénti listával dolgoznak, nálunk viszont egy szabad szöveges, magyar mondat több tételt, mennyiséget és lejáratot tartalmazhat; (2) a determinisztikus, indoklással ellátott receptajánlás a meglévő appokban nincs meg.
- **Kockázat:** Magas. A bíráló egy keresés után megcáfolhatja a termék fő pozicionálását, ez pedig az egész vision hitelességét rontja.
- **Ellenőrzési módszer:** Forrásellenőrzés: webkeresés és a versenytársak saját oldalainak, illetve összehasonlító cikkeinek átnézése (Samsung Food, KitchenPal, NoWaste.ai, SuperCook, Pantryfy, Recipy, Pantry Vault AI).
- **Eredmény:** **FAIL.** (1) A szabad szöveges, több tételes bevitel létezik: a [Pantry Vault AI](https://github.com/hardijain26/pantry_vault_ai) README-je szerint *„paste a shopping list and Gemini splits it into items with quantities and expiry dates”*, a [Pantryfy](https://www.pantryfy.ai/blog/best-pantry-inventory-apps) pedig szabad szöveges bevitelt ír. (2) Az átlátható ajánlásra és a magyar nyelvre vonatkozó állítás negatív állítás („más appban nincs”), amit nem lehet ellenőrizni. Mellékeredmény: a lejárat szerint rangsorolt receptajánlás és a főzés utáni levonás a [Samsung Food](https://support.samsungfood.com/hc/en-us/articles/30251599415956-How-to-Search-for-Recipes-Using-Your-Available-Ingredients)-ban is megvan, ezért ezt már korábban sem állítottuk egyedinek.
- **Következtetés:** A „meglévő alkalmazásokhoz képest” rész kikerült a [vision.md](../01_product/vision.md)-ből. Az értékajánlat csak a felhasználó jelenlegi helyzetéhez (státusz quo) mér, a G3 guardrail szövegét is ehhez igazítottuk. Tanulság: az AI által javasolt „csak nálunk van” típusú állítás alapból gyanús. Versenytárs-összehasonlítást csak funkciónkénti, forrásolt táblázatban teszünk, egyediséget nem állítunk.

### V-02 – Háztartási élelmiszer-pazarlási adatok a vision problémaleírásához
- **Dátum:** 2026-09-24
- **Állítás:** Az AI (Claude Code) a vision.md problémaleírásához számszerű adatot javasolt: egy magyar háztartásban fejenként évi 60,7 kg élelmiszer-hulladék keletkezik, ebből 20,8 kg elkerülhető, a fő okok a megfeledkezés és a túlvásárlás; az uniós háztartási átlag 69 kg/fő.
- **Kockázat:** Közepes. Téves vagy forrás nélküli szám a vision hitelességét rontja, és sérti az evidence-szabályt.
- **Ellenőrzési módszer:** Forrásellenőrzés az elsődleges forrásokon: NÉBIH Maradék nélkül program közleménye (2025-ös mérés, megjelent 2026-06-29) és az Eurostat 2023-as adatközlése.
- **Eredmény:** **PASS.** [NÉBIH](https://portal.nebih.gov.hu/-/mennyi-elelmiszerhulladek-keletkezik-a-haztartasokban-a-nebih-felmeresebol-kiderul): 60,7 kg/fő/év, ebből 20,8 kg/fő/év (34,3%) elkerülhető; minta: 151 háztartás; a kidobás okai: *„megfeledkezünk róluk, túl sokat vásárlunk, vagy a szükségesnél nagyobb mennyiséget készítünk el”*. [Eurostat](https://ec.europa.eu/eurostat/web/products-eurostat-news/w/ddn-20251016-2): a háztartások 69 kg/fő élelmiszer-hulladékot termeltek 2023-ban.
- **Következtetés:** Az adatok forrásmegjelöléssel kerültek a [vision.md](../01_product/vision.md) problémaleírásába. A harmadik okot (túl sok elkészített étel) a termék nem célozza, ezért a vision csak az első kettőre épít.

### V-03 – A Definition of Done követelmény-küszöbei (Gemini konzulensi felülvizsgálat)
- **Dátum:** 2026-09-27
- **Állítás:** A Gemini (P-06) szerint a scope_contract DoD-ja pontlevonást kockáztat: (1) 5 helyett legalább 8 ADR kell; (2) a verification logban legalább 3 tesztet eredményező és 2 mérésen/PoC-n alapuló bejegyzés kell (2 és 1 helyett); (3) a capability map kötelezően 5 oszlopos, Value/Productization kategóriával.
- **Kockázat:** Magas. Ha igaz, a DoD teljesítése mellett is pontveszteség érné a C és a H kategóriában; ha hamis, feleslegesen nő a scope.
- **Ellenőrzési módszer:** Forrásellenőrzés a v1.2 követelmény-dokumentumban (Claude Code, szövegkereséssel).
- **Eredmény:** **PASS.** (1) 5.3.3 fejezet: *„Minimum 8 ADR a kritikus döntésekhez”*, és a scorecard is 8 ADR-t pontoz (a 3.E fejezet minimuma 5). (2) A scorecard H sora: *„legalább 10 verifikáció, ebből minimum 3 teszttel és 2 méréssel/PoC-val”* (a 3.J fejezet 2-t és 1-et ír; a scorecard a szigorúbb). (3) 3.C fejezet, a kötelező oszlopok: Capability, Kategória (Value/Productization), Evidence, Teszt, Státusz (Done/Partial/Planned), legalább 3 Value és 3 Productization sorral.
- **Következtetés:** A [scope_contract.md](../01_product/scope_contract.md) DoD-ja 8 ADR-re és 3/2 verifikációs arányra módosult; a [capability_map.md](../01_product/capability_map.md) az 5 oszlopos szerkezetre állt át 6 Productization-sorral. Tanulság: a követelmény-dokumentum fejezetei és a scorecard eltérhetnek; mindig a szigorúbbat vesszük.

### V-04 – „Fiktív” modellnevek az AI-dokumentációban (Gemini audit)
- **Dátum:** 2026-09-28
- **Állítás:** A Gemini dokumentációs auditja szerint az ai_manifest.md és a prompt_log.md nem létező, kitalált modellneveket tartalmaz (`claude-sonnet-4-6`, `claude-opus-5-5`), és ezeket 2024-es azonosítókra (például `claude-3-5-sonnet-20241022`) kellene cserélni.
- **Kockázat:** Közepes. Ha a nevek tényleg hamisak, az AI-dokumentáció hiteltelen; ha a javaslatot vakon elfogadjuk, épp így kerülne hamis adat a manifestbe.
- **Ellenőrzési módszer:** Összevetés a Claude Code munkamenet által jelentett, ténylegesen használt modellazonosítóval (a P-02–P-05 munkamenetek `claude-opus-5-5` modellen futottak; a P-01 munkamenet a `claude-sonnet-4-6` azonosítót jelentette).
- **Eredmény:** **FAIL** (az audit állítása hibás). A két azonosító valós, a munkamenetek ténylegesen ezeken a modelleken futottak. A reviewer tudása a modell-kiadásokról elavult, és a javasolt csere a saját „ne találj ki” szabályát sértette volna meg.
- **Következtetés:** A modellnevek változatlanok maradnak. Tanulság: a reviewer AI állításai is ellenőrizendők; a keresztvalidáció eltérése nem jelenti automatikusan, hogy a reviewernek van igaza. A modellverziókat a futtató eszköz által jelentett azonosító alapján rögzítjük, nem egy másik modell tudása alapján.

### V-05 – A quality_attributes.md tervezetének konzisztenciája (GitHub Copilot review)
- **Dátum:** 2026-10-02
- **Állítás:** A GitHub Copilot (gpt-5.6-luna) reviewja szerint a Claude Code által készített quality_attributes.md tervezetben öt pont pontosítandó: (1) az S-2 „végpontok 100%-a” végpontleltár nélkül nem mérhető; (2) az S-1 „legkésőbb 31 mp” nem következik a 15 mp + egy újrapróbálás szabályból, mert nem definiált, hogy a 15 mp próbálkozásonként vagy a teljes műveletre értendő; (3) a QA-4 „nem vész el levonás” célértéke korai, mert a konkurenciakezelés nyitott; (4) az ajánlás-végpont p95 ≤ 300 ms célértéke nem szerepel a scope-ban és a metrics.md-ben, mégis meglévő követelményként van hivatkozva; (5) a ≥ 80% lefedettség nem bizonyít karbantarthatóságot, a reflexiós rétegszabály-teszt pedig nincs elfogadott megoldásként rögzítve.
- **Kockázat:** Közepes. Pontatlan mérőszám vagy forrás mellett a scenario nem igazolható, és a bíráló a C kategóriában kifogásolhatja; a téves pontok vak elfogadása viszont feleslegesen bővítené a dokumentumot.
- **Ellenőrzési módszer:** Összevetés a tervezettel, a [scope_contract.md](../01_product/scope_contract.md) (US-2, US-4, Külső API), a [metrics.md](../01_product/metrics.md), a prompt log P-06, P-10, P-11 bejegyzései és a kikérdezés döntései (Q3, Q5–Q7) alapján (Claude Code).
- **Eredmény:** **Vegyes.** (1) Részben PASS: a viszonyítási alap hiányzott, de leltár már van (api.md, mcp_tools.md). (2) PASS: a „próbálkozásonként” sem a tervezetben, sem a scope_contractban nem volt kiírva. (3) FAIL: a QA az elvárást rögzíti, a mechanizmust az ADR; a nyitott kérdés csak a mechanizmus volt, és mindkét jelölt teljesíti az elvárást. (4) Részben PASS: a forrásoszlop félrevezető volt, a mérési módszer viszont már szerepelt az S-3-ban. (5) Részben PASS: a reflexiós tesztet a kikérdezés Q7-ben elfogadtuk (a reviewer ezt nem láthatta), a lefedettség azonban valóban csak tesztelhetőséget támaszt alá.
- **Következtetés:** A [quality_attributes.md](../02_architecture/quality_attributes.md)-ben az S-2 mérőszáma az api.md és a mcp_tools.md leltárára hivatkozik; a 15 mp próbálkozásonkénti értelmezése a quality_attributes.md-be és a scope_contract.md-be is bekerült; a QA-5 forrása új elvárásként jelölt; a QA-6 neve „Modularitás és tesztelhetőség” lett. A QA-4 változatlan. Tanulság: a reviewer csak a fájlokat látja, a munkamenetben hozott, még le nem írt döntéseket nem, ezért a review előtt a döntéseket rögzíteni kell.

### V-06 – Minimális jelszóhossz a NIST szerint (Gemini vak trianguláció)
- **Dátum:** 2026-10-02
- **Állítás:** Az authentikációs döntés vak triangulációjában az Antigravity (Gemini 3.1 Pro) a NIST modern irányelveire hivatkozva legalább 8 karakteres jelszót javasolt, összetételi szabályok nélkül.
- **Kockázat:** Közepes. Ha a 8 karakteres minimum kerül be, a jelszószabály egy már nem érvényes szabványváltozatra hivatkozna, és a G kategóriában (security) a bíráló kifogásolhatja.
- **Ellenőrzési módszer:** Forrásellenőrzés a NIST SP 800-63B-4 hivatalos szövegében (Claude Code, webkeresés).
- **Eredmény:** **FAIL** (részben). Az összetételi szabályok tilalma helyes, a hossz nem: a [NIST SP 800-63B-4](https://pages.nist.gov/800-63-4/sp800-63b.html) szerint egyfaktoros jelszónál *„a minimum of 15 characters in length”* kötelező; a 8 karakteres minimum csak többfaktoros hitelesítés részeként használt jelszóra vonatkozik. A 8 karakter a szabvány korábbi változatának szabálya.
- **Következtetés:** Az [ADR-0006](../02_architecture/adr/0006-cookie-auth-identity.md) jelszószabálya marad legalább 15 karakter, összetételi szabályok nélkül. Tanulság: a reviewer szabvány- és verzióállításai is elavultak lehetnek (lásd V-04); szabványra hivatkozó javaslatot a szabvány aktuális szövegével vetünk össze.

### V-07 – A FluentAssertions 8.x licence nem Apache-2.0 (walking skeleton tesztcsomagjai)
- **Dátum:** 2026-10-03
- **Állítás:** A walking skeleton tervezésekor a Claude Code azt állította, hogy az AGENTS.md-ben megnevezett FluentAssertions a 8.0-tól kereskedelmi (Xceed) licencű, a 7.x pedig még Apache-2.0, ezért a 7.x rögzítését javasolta.
- **Kockázat:** Közepes. Ha a 8.x kerül be, a projekt olyan licencű függőséget használ, amely kereskedelmi célra fizetős; ez a G kategóriában (licenc) kifogásolható, és a későbbi felhasználást korlátozza.
- **Ellenőrzési módszer:** Forrásellenőrzés: az [Inedo cikke a licencváltásról](https://blog.inedo.com/proget/fluent-assertions-license-changes), a [FluentAssertions 8.3.0 NuGet licencoldala](https://packages.nuget.org/packages/FluentAssertions/8.3.0/License), valamint a 7.2.0 és a 7.2.2 csomag nuspec fájljának `license` mezője a NuGet API-ból (Claude Code).
- **Eredmény:** **PASS.** A 8.x az Xceed Community License Agreement for Non-Commercial Use alá tartozik, kereskedelmi használatra fizetős licenc kell; a 7.2.0 és a 7.2.2 nuspec-je `Apache-2.0` licenckifejezést tartalmaz.
- **Következtetés:** A [Directory.Packages.props](../../Directory.Packages.props) a FluentAssertionst `[7.2.2,8.0)` tartománnyal rögzíti, így véletlen frissítés sem emelheti 8.x-re. Az AGENTS.md stacktáblája változatlan.
