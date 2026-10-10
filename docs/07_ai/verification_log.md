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

### V-08 – A `global.json` `test.runner` beállítása „hallucinált” (Gemini szakasz-review)
- **Dátum:** 2026-10-03
- **Állítás:** A walking skeleton S1 szakaszának reviewjában az Antigravity (Gemini 3.1 Pro) a `global.json` `"test": { "runner": "Microsoft.Testing.Platform" }` csomópontját vélhetően hallucináltnak nevezte, és helyette a `<TestingPlatformDotnetTestSupport>` MSBuild-tulajdonságot javasolta (a .NET 8/9 tudására hivatkozva, elavultsági figyelmeztetéssel).
- **Kockázat:** Közepes. Ha elfogadjuk, a `dotnet test` a .NET 10 SDK-n nem futtatja az xUnit v3 teszteket.
- **Ellenőrzési módszer:** Forrásellenőrzés ([Microsoft Learn – unit testing with dotnet test](https://learn.microsoft.com/nb-no/dotnet/core/testing/unit-testing-with-dotnet-test), [.NET blog](https://devblogs.microsoft.com/dotnet/?p=57713)) és kísérlet (Claude Code).
- **Eredmény:** **FAIL.** A .NET 10 SDK-ban a `TestingPlatformDotnetTestSupport` elavult, a `global.json` `test.runner` a dokumentált mód. A beállítás nélkül a `dotnet test` „Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later” hibával leáll, vele a 4 teszt zöld.
- **Következtetés:** A [global.json](../../global.json) változatlan. Tanulság: a reviewer keretrendszer-verziós állítását kísérlettel ellenőrizzük (vö. V-04, V-06).

### V-09 – Az adatbázis-hiba részletei alapból nem kerülnek a kivételbe (Npgsql)
- **Dátum:** 2026-10-03
- **Állítás:** A Claude Code az ADR-0011-ben és az S2 Gemini-reviewjának szétválogatásában azt állította, hogy az Npgsql a PostgreSQL-hibák részleteit (amelyekben sor- és mezőértékek, így személyes adat is lehet) alapból nem teszi a kivételbe, csak az `Include Error Detail` kapcsolati beállítással; ezért elég ezt (és az EF Core `EnableSensitiveDataLogging`-ot) kikapcsolva tartani, külön napló-tisztító nem kell.
- **Kockázat:** Közepes. Ha nem igaz, egy egyedi index megsértésekor (például foglalt e-mail-cím) az e-mail-cím a váratlan kivétel üzenetével a naplóba kerülhet, ami sérti az AGENTS.md 5. és 7. pontját.
- **Ellenőrzési módszer:** Forrásellenőrzés: [Npgsql – Connection String Parameters](https://www.npgsql.org/doc/connection-string-parameters) (Claude Code, webkeresés).
- **Eredmény:** **PASS.** A dokumentáció szerint az `Include Error Detail` bekapcsolásakor kerülnek a részletek a `PostgresException.Detail`-be, és ezek „can contain sensitive data”; az alapértelmezés `false`.
- **Következtetés:** A [DependencyInjection.cs](../../src/backend/KamraApp.Infrastructure/DependencyInjection.cs) egyik beállítást sem kapcsolja be, a szabály az [ADR-0011](../02_architecture/adr/0011-serilog-strukturalt-naplozas.md) „Figyelni kell” részében szerepel. A foglalt e-mail esetét a kód amúgy is kezelt `EMAIL_ALREADY_REGISTERED` hibává alakítja, így az nem jut el a váratlan-kivétel naplóig.

### V-10 – A fiókzárolás párhuzamos kérésekkel nem kerülhető meg (ellenséges teszt)
- **Dátum:** 2026-10-03
- **Állítás:** Az [ADR-0006](../02_architecture/adr/0006-cookie-auth-identity.md) és a Claude Code implementációja szerint az ASP.NET Core Identity lockoutja (5 sikertelen próbálkozás → 5 perc) megvédi a fiókot a jelszópróbálgatástól; a soros teszt ezt igazolta is.
- **Kockázat:** Magas. Ha a küszöb megkerülhető, a lockout nem véd a brute force ellen, csak az IP-alapú rate limit marad.
- **Ellenőrzési módszer:** Ellenséges teszt: az Antigravity (Gemini 3.1 Pro) által írt `Lockout_threshold_is_enforced_under_concurrent_requests_and_is_case_insensitive` ([AdversarialAuthTests.cs](../../tests/KamraApp.Integration.Tests/AdversarialAuthTests.cs)) 10 párhuzamos hibás bejelentkezést küld egy bejelentkezett kliensről; diagnózis ideiglenes próbatesztekkel (Claude Code).
- **Eredmény:** **FAIL**, két okkal. (1) Mind a 10 válasz 401 volt, és utána a 11. sem zárolt: a cookie security-stamp ellenőrzése a kérés elején betöltötte a felhasználót a kérés DbContextjébe, a bejelentkezés ezt az elavult példányt kapta vissza, és a számláló frissítése optimista ütközés miatt csendben elveszett. (2) A friss olvasás után is elbukott a teszt zár nélkül: valódi párhuzamosságnál az Identity számlálónövelései elvesznek (ezt a zár ideiglenes kivételével igazoltuk).
- **Következtetés:** Az [IdentityService.cs](../../src/backend/KamraApp.Infrastructure/Identity/IdentityService.cs) a jelszó-ellenőrzést egy folyamaton belüli zár mögött futtatja, és előtte üríti a DbContext követett entitásait; a teszt zöld (háromszor egymás után). Korlát (`ponytail:` komment): egy Api-példányra érvényes; több példánynál adatbázis-szintű atomikus számlálás kellene. Tanulság: a soros teszt zöld volt, a hibát csak a párhuzamos, ellenséges teszt mutatta meg.

### V-11 – A kijelentkezés utáni régi cookie érvényes marad (ellenséges teszt)
- **Dátum:** 2026-10-03
- **Állítás:** Az [ADR-0006](../02_architecture/adr/0006-cookie-auth-identity.md) maradó kockázatként rögzítette, hogy a kijelentkezés csak az adott eszközön törli a cookie-t, egy ellopott cookie a lejáratig érvényes.
- **Kockázat:** Közepes. Egy ellopott cookie-val a támadó a felhasználó kijelentkezése után is hozzáfér a háztartás adataihoz, legfeljebb 14 napig.
- **Ellenőrzési módszer:** Ellenséges teszt: `Reusing_a_cookie_after_logout_is_rejected` ([AdversarialAuthTests.cs](../../tests/KamraApp.Integration.Tests/AdversarialAuthTests.cs)), amely a regisztrációkor kapott cookie-t a kijelentkezés után egy másik kliensről küldi vissza.
- **Eredmény:** **FAIL** (a dokumentált viselkedés szerint): a régi cookie-val a `/auth/me` 200-at adott.
- **Következtetés:** A fejlesztő a kockázat csökkentése mellett döntött: a kijelentkezés új security stampet ad (`UpdateSecurityStampAsync`), és a cookie-t minden kérésnél a stamp alapján ellenőrizzük (`SecurityStampValidatorOptions.ValidationInterval = 0`), így a kijelentkezés minden eszközön érvényteleníti a sessiont. Ára kérésenként egy felhasználó-lekérdezés. Az ADR-0006 Consequences szakasza dátummal kiegészítve; a teszt zöld.

### V-12 – Az antiforgery cookie `Secure = Always` beállítása biztonságosabb (hibás AI-javaslat)
- **Dátum:** 2026-10-03
- **Állítás:** Az S3 elő-reviewjának szétválogatásában a Claude Code javasolta (J1), hogy az antiforgery cookie is kapjon `SecurePolicy = Always`-t, mint a session-cookie; a fejlesztő jóváhagyta, és a 43 integrációs teszt zöld maradt.
- **Kockázat:** Magas (működés). Ha a beállítás hibás, a sima HTTP-n futó telepítésben (helyi Docker Compose) senki nem tud bejelentkezni vagy regisztrálni.
- **Ellenőrzési módszer:** Az S4 Playwright e2e tesztje ([auth.spec.ts](../../tests/e2e/auth.spec.ts)) valódi Chrome-ban, `http://localhost`-on; az Api naplójának elemzése.
- **Eredmény:** **FAIL.** Minden e2e futás elbukott: az ASP.NET Core antiforgery-rendszere `SecurePolicy = Always` mellett szerveroldalon elutasítja a tokenkiadást nem-HTTPS kérésnél („the current request is not an SSL request”), így a `GET /auth/antiforgery` 500-at adott. Az integrációs tesztek ezt nem láthatták, mert HTTPS-címmel futnak.
- **Következtetés:** Az antiforgery cookie visszakerült a keretrendszer alapértelmezésére (`SameAsRequest`: HTTPS alatt Secure), a [AuthenticationSetup.cs](../../src/backend/KamraApp.Api/Auth/AuthenticationSetup.cs) kommentje rögzíti az okot; új integrációs teszt (`Antiforgery_token_is_issued_over_plain_http`) és a 4 e2e futás zöld. Tanulság: a biztonsági „szigorítást” a valódi telepítési környezetben (HTTP, böngésző) is ellenőrizni kell; a HTTPS-es tesztkliens elfedte a hibát.

### V-13 – A `Secure` session-cookie `http://localhost`-on is működik a böngészőben
- **Dátum:** 2026-10-03
- **Állítás:** A walking skeleton tervezésekor a Claude Code azt állította, hogy a `Secure` jelzésű session-cookie-t a böngészők `http://localhost`-on is elfogadják (a localhost biztonságos kontextusnak számít), ezért a helyi, HTTP-s Docker Compose telepítéshez nem kell TLS.
- **Kockázat:** Magas (működés). Ha nem igaz, a helyi telepítésben a bejelentkezés után a cookie nem kerül vissza, és a felhasználó azonnal kijelentkezettnek látszik.
- **Ellenőrzési módszer:** Teszt: a Playwright e2e ([auth.spec.ts](../../tests/e2e/auth.spec.ts)) Chromiumban, `http://localhost:5083`-on: regisztráció, oldal-újratöltés után is bejelentkezett állapot, kijelentkezés, bejelentkezés (asztali és 360 px-es nézet).
- **Eredmény:** **PASS.** Mind a 4 futás zöld; a session az újratöltés után is megmaradt.
- **Következtetés:** A helyi telepítés HTTP-n marad. Korlát: a böngészők ezt csak a `localhost`-ra engedik; más gépnévvel vagy IP-címmel elért telepítéshez TLS kell (a deploy runbookban rögzítendő).

### V-14 – A bejelentkezés túléli az api konténer újraindítását (Data Protection kulcsok volume-on)
- **Dátum:** 2026-10-04
- **Állítás:** Az [ADR-0006](../02_architecture/adr/0006-cookie-auth-identity.md) szerint a Data Protection kulcsok (a cookie és az antiforgery token titkosítása) Docker volume-on vannak, ezért az api konténer újraindítása, deployja vagy rollbackje nem jelentkezteti ki a felhasználókat. A nem-root `app` felhasználó miatt a Claude Code a `/keys` mappát az image-ben előre létrehozta az `app` tulajdonában, hogy az új named volume írható legyen.
- **Kockázat:** Közepes. Ha nem igaz, minden újraindítás kijelentkezteti a felhasználókat (G2 megtartás), és a régi antiforgery tokenek érvénytelenek lesznek.
- **Ellenőrzési módszer:** PoC a Docker Compose stacken (Claude Code): Playwright-szkripttel regisztráció, `docker compose restart api`, oldal-újratöltés; a `/keys` tartalmának és az api naplójának ellenőrzése.
- **Eredmény:** **PASS.** Újraindítás után a felhasználó bejelentkezve maradt; a `/keys`-ben egy kulcsfájl van az `app` tulajdonában, és újraindításkor nem generálódott új kulcs. (Egy első próbaszkript hamis negatív eredményt adott, mert az újratöltés után nem várta meg az oldal betöltését; ezt javítottuk.)
- **Következtetés:** A [Dockerfile](../../Dockerfile) és a [docker-compose.yml](../../docker-compose.yml) beállítása marad. Maradó kockázat: a kulcsok titkosítatlan XML-ként vannak a volume-on (a keretrendszer erre figyelmeztet: „No XML encryptor configured”); a volume-hoz hozzáférő ember a cookie-kat visszafejtheti. A threat modelbe kerül.

### V-15 – A repóban nincs titok (secret hygiene)
- **Dátum:** 2026-10-04
- **Állítás:** A walking skeleton végén a Claude Code azt állította, hogy a repóban (a teljes git-historyban is) nincs token, jelszó vagy API-kulcs: a titkok csak a gitignore-olt `.env`-ben vannak, a `.env.example` és a kód csak helykitöltőket és szándékos, nem valódi tesztértékeket tartalmaz.
- **Kockázat:** Magas. A v1.2 szerint a repóban talált titok miatt a leadás elfogadhatatlan a javításig (kapufeltétel).
- **Ellenőrzési módszer:** (1) [gitleaks](https://github.com/gitleaks/gitleaks) v8.30.1 a push előtt a 36 új commitra, majd a teljes historyra (56 commit); (2) kézi ellenőrzés: követett titok-jellegű fájlok (`.env`, `appsettings.Development.json`, kulcsfájlok), jelszó- és kulcsmintázatok a hozzáadott sorokban, helyi útvonalak és személyes adat a diffben (Claude Code); (3) a GitHub Secret Protection és Push protection bekapcsolása ([képernyőkép](../assets/secret-protection.png)); (4) a CI `secrets` jobja minden pushnál a teljes historyt szkenneli ([ci.yml](../../.github/workflows/ci.yml)).
- **Eredmény:** **PASS.** gitleaks: „no leaks found” (36, illetve 56 commit). A jelszómintázatok mind kódra (mezőnevek, validáció) vagy szándékos tesztértékre mutatnak: a `Password=hunter2` egy csali, amelynek a hibaválaszból való hiányát teszt ellenőrzi; a `correct horse battery staple` tesztjelszó. A `.env` és az `appsettings.Development.json` a `.gitignore` szerint kimarad.
- **Következtetés:** A secret hygiene három rétegű: helyi ellenőrzés push előtt, GitHub push protection (ismert szolgáltatói kulcsminták), gitleaks a CI-ban (a teljes history minden pushnál; a `develop`/`main` ágon kötelező check a `protect-develop-main` rulesetben). Korlát: a gitleaks és a GitHub is mintázat-alapú; egy egyedi, ismert formátum nélküli jelszót csak a kézi review fog meg.

### V-16 – Friss klónból 15 percen belül elindul (QA-7)
- **Dátum:** 2026-10-04
- **Állítás:** A [quality_attributes.md](../02_architecture/quality_attributes.md) QA-7 és a v1.2 futtathatósági kapufeltétele szerint a rendszer tiszta gépen, a README alapján, `docker compose up` paranccsal 15 percen belül elindul.
- **Kockázat:** Magas. Ha nem teljesül, a v1.2 szerint a pontszám plafonja 40.
- **Ellenőrzési módszer:** Mérés (Claude Code, Windows 11, Docker Desktop 29.4): `git clone` a GitHubról egy üres ideiglenes mappába → `cp .env.example .env` → `docker compose build --no-cache --pull` → `docker compose up -d` → időmérés a `/health` első 200-as válaszáig; utána a Playwright regisztrációs e2e tesztje a mért stack ellen.
- **Eredmény:** **PASS.** Klónozás 2 mp, cache nélküli build 114 mp, a `/health` 200 a kezdéstől számítva **122 mp** alatt; a regisztráció → üres készlet e2e teszt zöld. Korlát: az alap-image-ek (`dotnet/sdk:10.0`, `dotnet/aspnet:10.0`, `node:24-alpine`, `postgres:18`) a gépen már korábban le voltak töltve, így a mérés nem tartalmazza az első letöltést; ez a hálózati sávszélességtől függően néhány perccel növelheti az időt, ami a 15 perces kereten belül marad.
- **Következtetés:** A README „Gyors indítás” lépései változatlanok. A leadás előtti végső ellenőrzést egy másik, tiszta gépen (vagy a bíráló környezetéhez hasonló környezetben) érdemes megismételni.

### V-17 – Üres `AllowedUserNameCharacters` minden felhasználónevet elutasít (Copilot PR-review)
- **Dátum:** 2026-10-04
- **Állítás:** A walking skeleton PR-jának GitHub Copilot reviewja (lite) magas súlyossággal azt állította, hogy az ASP.NET Core Identity `User.AllowedUserNameCharacters = ""` beállítása egy üres engedélylista, ezért minden nem üres felhasználónevet (így az e-mail-címet is) elutasít, és a regisztráció nem működik.
- **Kockázat:** Magas. Ha igaz, senki nem tud regisztrálni.
- **Ellenőrzési módszer:** Az Identity `UserValidator` forráskódja (a karakterellenőrzés csak akkor fut, ha a lista nem üres: `!string.IsNullOrEmpty(manager.Options.User.AllowedUserNameCharacters)`) és a meglévő tesztek: a regisztrációs integrációs tesztek ([AuthTests.cs](../../tests/KamraApp.Integration.Tests/AuthTests.cs)), az unicode- és határérték-ellenséges teszt ([AdversarialAuthTests.cs](../../tests/KamraApp.Integration.Tests/AdversarialAuthTests.cs)) és a böngészős e2e ([auth.spec.ts](../../tests/e2e/auth.spec.ts)), mind valódi regisztrációval (Claude Code).
- **Eredmény:** **FAIL.** Az üres lista kikapcsolja a karakterkorlátozást; a regisztráció működik, minden említett teszt zöld (CI is).
- **Következtetés:** A beállítás marad (az e-mail formátumát a use case validálja, [AuthRequests.cs](../../src/backend/KamraApp.Application/Auth/AuthRequests.cs)). Tanulság: a review ugyanabban a körben két valós hibát is talált (folyamatszintű zár, eldobott security-stamp eredmény), de egy magabiztosan téves „High” megállapítást is tett; a súlyosság nem helyettesíti a forrás- vagy tesztellenőrzést.

### V-18 – A kategóriák alapértelmezett eltarthatósága forrásból (USDA FoodKeeper)
- **Dátum:** 2026-10-08
- **Állítás:** A Claude Code a becsült lejárathoz 14 kategóriát és napértékeket javasolt a USDA FSIS FoodKeeper adataiból, azzal a szabállyal, hogy a napérték a kategória jellemző termékei FoodKeeper-minimumai közül a legkisebb ([data_model.md](../03_design/data_model.md), Kategórialista).
- **Kockázat:** Közepes. A túl késői becslés miatt a felhasználó későn veszi észre a romló tételt (G1 pazarolt arány), és a becslés igazolt eltarthatóságnak tűnhet.
- **Ellenőrzési módszer:** A hivatalos `foodkeeper.json` letöltése „Access Denied” választ adott, ezért a FoodKeeper XLS GitHub-tükrét ([FoodKeeperResearch](https://github.com/BrandonChenze/FoodKeeperResearch), adatverzió 108, 662 termék) szkript dolgozta fel; a kategóriák jellemző termékeinek tartományait egyenként kigyűjtöttem. Szúrópróba a hivatalos FSIS hűtési táblázattal ([FoodSafety.gov](https://www.foodsafety.gov/food-safety-charts/cold-food-storage-charts)): darált hús 1–2 nap, steak 3–5 nap, héjas tojás 3–5 hét, bontott felvágott 3–5 nap (Claude Code).
- **Eredmény:** **PASS.** A szúrópróba négy értéke egyezik a tükörrel; a napértékek a táblázatban szereplő termékekből a szabály szerint visszaszámolhatók.
- **Következtetés:** A napértékek forrással a data_model.md-ben vannak. Korlátok: a tükör a 108-as adatverzió, nem a legfrissebb hivatalos fájl; a FoodKeeper amerikai termékekre és tárolási szokásokra készült (például a kifli helyett a bagel a közelítés); a tejnél és a tejfölnél a FoodKeeper a csomagon lévő dátumot tekinti mérvadónak, ezért a felület a csomag dátumának megadására biztat. A becsült lejárat szervezési segédadat, nem igazolt eltarthatóság.

### V-19 – Az `xmin` háttérműveletekre is változik (vak trianguláció, ADR-0008)
- **Dátum:** 2026-10-08
- **Állítás:** Az ADR-0008 vak triangulációjában az Antigravity (Gemini 3.1 Pro) azt állította, hogy a PostgreSQL `xmin` rendszeroszlopa „bármilyen (akár háttérbeli, technikai) módosításra” változik, ezért optimista zárolási tokenként indokolatlan 409-et okozhat, és helyette saját `Version` oszlopot javasolt.
- **Kockázat:** Közepes. Ha igaz, a két fül elleni védelem véletlenszerűen elutasítana jogos módosításokat; ha téves és elfogadjuk, felesleges oszlop és kézi verzióléptetés kerül a kódba.
- **Ellenőrzési módszer:** Hivatalos dokumentáció (Claude Code): a [PostgreSQL 18 – Routine Vacuuming](https://www.postgresql.org/docs/18/routine-vacuuming.html) szerint a 9.4 előtti verziókban a fagyasztás felülírta az `xmin`-t, „az újabb verziók csak egy jelzőbitet állítanak, megőrizve a sor eredeti `xmin`-jét”; az [Npgsql EF Core – Concurrency Tokens](https://www.npgsql.org/efcore/modeling/concurrency.html) az `xmin`-t ajánlja tokennek (`uint` property, `IsRowVersion()`), mert „minden alkalommal frissül, amikor a sor megváltozik”.
- **Eredmény:** **FAIL.** A háttérbeli VACUUM/fagyasztás PostgreSQL 9.4 óta nem változtatja az `xmin` látható értékét; az `xmin` csak a sor `UPDATE`-jére változik, vagyis pontosan akkor, amikor egy saját `Version` oszlopot is léptetni kellene. A reviewer által említett `UseXminAsConcurrencyToken` helyett a dokumentáció a `uint` + `IsRowVersion()` formát mutatja.
- **Következtetés:** Az ADR-0008 az `xmin`-t használja; a saját `Version` oszlop az Alternatives 1. pontjába került. A reviewer másik eltérő javaslatát (optimista zárolás a főzésnél is) elfogadtam. Korlát: az ellenőrzés dokumentáción alapul; a megvalósítást az ADR-0008 integrációs tesztje (elavult verzióval küldött szerkesztés → 409) igazolja.

### V-20 – A `coverlet.MTP` lefedettségi küszöbe (dokumentáció vs. mérés)
- **Dátum:** 2026-10-09
- **Állítás:** A coverlet hivatalos dokumentációja ([Coverlet.MTP.Integration.md](https://github.com/coverlet-coverage/coverlet/blob/master/Documentation/Coverlet.MTP.Integration.md)) szerint a Microsoft Testing Platform-bővítmény elfogadja a `--coverlet-threshold` kapcsolót, de a küszöb érvényesítése „még nem támogatott”; erre alapozva a Claude Code először saját Cobertura-feldolgozó szkriptet tervezett a CI-kapuhoz.
- **Kockázat:** Közepes. Ha a küszöb nem érvényesül, a „Domain + Application ≥ 80%” DoD-kapu csak papíron létezik; ha érvényesül, a saját szkript felesleges kód.
- **Ellenőrzési módszer:** PoC-mérés a projektben használt `coverlet.MTP` 10.1.0-val (Claude Code, helyben): `dotnet test --project tests/KamraApp.Unit.Tests --coverlet --coverlet-output-format cobertura --coverlet-include "[KamraApp.Domain]*" --coverlet-include "[KamraApp.Application]*" --coverlet-threshold <N> --coverlet-threshold-type line`, N = 80 és N = 95, a kilépési kód és a Cobertura-riport csomagonkénti `line-rate` értéke (Domain 1,0, Application 0,889).
- **Eredmény:** **FAIL** (a dokumentáció állítására). N = 80-nál a futás sikeres, N = 95-nél „Failed!” 14-es kilépési kóddal, miközben mind a 24 teszt zöld; szűrő nélkül már 1%-os küszöb is elbukik, mert az Infrastructure lefedettsége 0%. A küszöb tehát a 10.1.0-ban érvényesül, a dokumentáció elavult.
- **Következtetés:** A CI a beépített kapcsolókkal érvényesíti a kaput, saját szkript nélkül ([ci.yml](../../.github/workflows/ci.yml), [test_strategy.md](../04_quality/test_strategy.md)). Korlát: a 14-es kilépési kód nem mondja meg, melyik csomag bukott el; ehhez a feltöltött Cobertura-riportot kell megnézni. Tanulság: a hivatalos dokumentáció is lehet elavult, a verziófüggő állítást mérni kell.

### V-21 – A főzéskérés tételverziók nélkül „vak” (kapu-review, ADR-0008)
- **Dátum:** 2026-10-09
- **Állítás:** A B1 kapu-reviewjában az Antigravity (Gemini 3.1 Pro) kritikus súlyossággal azt állította, hogy mivel a `POST /cookings` kérés nem tartalmazza az érintett készlettételek verzióját, a szerver nem tudja észlelni a közbeni állapotváltozást, ezért a „két fülből indított műveletnél nem vész el változás” követelmény csak részben teljesül; javaslata a tételverziók elküldése.
- **Kockázat:** Magas. Ha igaz, a főzés felülírhat egy másik fülön mentett változást (QA-4).
- **Ellenőrzési módszer:** Az ADR-0008 mechanizmusának végigvezetése a lehetséges közbeékelődésekre (Claude Code): a szerver a főzés tranzakcióján belül olvassa be a tételeket, és a mentést az `xmin` védi az olvasás és az írás között; a kliens hozzávalónként erősít meg mennyiséget (US-4), a FEFO-felosztás a friss készleten történik. Megvizsgált esetek: másik fül csökkenti az egyik tételt (elégtelen → `INSUFFICIENT_STOCK`, elegendő → helyes levonás a friss értékből), másik fül a szerver olvasása után módosít (`PANTRY_ITEM_MODIFIED`), másik fül új tételt vesz fel (a FEFO a friss készletből számol; a javasolt verziólista ezt nem is fogná meg).
- **Eredmény:** **FAIL** a „vak szerver, elvesző változás” állításra: elveszett módosítás egyik esetben sem keletkezik. Valós rész: az ADR nem mondta ki, hogy a megerősítés hozzávalónként, nem készlettételenként történik; ez pótolva (ADR-0008, 2. pont és Alternatives 7). A gyakorlati bizonyítékot az ADR-0008 Verification integrációs tesztje (két egyidejű főzés) adja az implementációval.
- **Következtetés:** A kérés nem kap tételverziókat. Ugyanez a review egy valós hiányt is talált (a bevásárlójavaslat egyidejű elfogadása és elutasítása), ez az ADR-0008 4. pontjába került. Tanulság: a súlyosság itt is túlzó volt; a mechanizmust lépésenként kell végigvezetni, nem a mezőlistából következtetni.

### V-22 – Null-mennyiség a bevásárlólista upsertjében (Copilot PR-review)
- **Dátum:** 2026-10-09
- **Állítás:** A B1 PR GitHub Copilot-reviewja azt állította, hogy ha egy elfogyott-javaslatból `Quantity = null` listatétel keletkezik, akkor az azonos hozzávaló későbbi, ismert mennyiségű felvétele az összeadó upsertben (`Quantity = Quantity + excluded.Quantity`) elvész, mert a PostgreSQL-ben a null-lal végzett összeadás null.
- **Kockázat:** Magas. A felhasználó által megadott mennyiség csendben eltűnne a bevásárlólistáról.
- **Ellenőrzési módszer:** Mérés PostgreSQL 18.6-on (`postgres:18` image, ugyanaz, mint a Compose-ban és a Testcontainersben; Claude Code): `SELECT (NULL::numeric + 5) IS NULL` → `t`; a `numeric_add` függvény `proisstrict = t` (null bemenetre null az eredmény); a javasolt `CASE WHEN mindkettő null THEN NULL ELSE COALESCE(a,0) + COALESCE(b,0) END` kifejezés `NULL` és 5 esetén 5-öt ad. A `COALESCE` viselkedése: [PostgreSQL 18 – Conditional Expressions](https://www.postgresql.org/docs/18/functions-conditional.html) („az első nem null argumentumát adja vissza”).
- **Eredmény:** **PASS.** A null + ismert összeadás null; a sima összeadó upsert valóban elvesztené az ismert mennyiséget.
- **Következtetés:** A data_model.md rögzíti az összevonási szabályt (ismert + ismert = összeg; ismeretlen + ismert = ismert; ismeretlen + ismeretlen = ismeretlen), az ADR-0008 5. pontja erre hivatkozik; integrációs teszt mindkét beszúrási sorrendre a US-5-tel készül.

### V-23 – A „ma” Budapest szerint a konténerben is számolható (időzóna-adat)
- **Dátum:** 2026-10-10
- **Állítás:** Az AI (az 1. lépcső tervének Q4 döntésénél) azt állította, hogy a `Europe/Budapest` IANA-időzóna a futtató `mcr.microsoft.com/dotnet/aspnet:10.0` image-ben és a Windows-os fejlesztői gépen is feloldható, így a „ma” (hamarosan lejáró, becsült lejárat) gépfüggetlenül, fix időzónával számolható.
- **Kockázat:** Közepes. Hiányzó időzóna-adatnál a `TimeZoneInfo.FindSystemTimeZoneById` kivételt dob; UTC-re visszaesve éjfél és 1–2 óra között rossz napot kapnánk.
- **Ellenőrzési módszer:** Mérés (Claude Code): `docker run --rm --entrypoint ls mcr.microsoft.com/dotnet/aspnet:10.0 -l /usr/share/zoneinfo/Europe/Budapest` → a fájl létezik (az image Ubuntu 24.04.5 LTS-alapú). Teszt: `Today_is_the_calendar_day_in_Budapest` ([ExpiryTests.cs](../../tests/KamraApp.Unit.Tests/ExpiryTests.cs); UTC okt. 9. 23:30 → okt. 10., dec. 31. 23:30 UTC → jan. 1., nyári és téli idő) Windowson és a Linuxos CI-runneren ([run 38069303341](https://github.com/toldor/kamra/actions/runs/38069303341)).
- **Eredmény:** **PASS.**
- **Következtetés:** A „ma” egyetlen helyen számolódik ([Today.cs](../../src/backend/KamraApp.Application/Common/Today.cs)); az egy időzóna korlátja a [data_model.md](../03_design/data_model.md) ismert hiányosságai között szerepel.

### V-24 – A `numeric(12,3)` oszlop kerekít és túlcsordul (elő-review)
- **Dátum:** 2026-10-10
- **Állítás:** Az S1 elő-reviewja (Claude Code `/code-review`, spec-tengely) azt állította, hogy a Domain bármilyen decimalt elfogad, a PostgreSQL viszont a `numeric(12,3)` oszlopban a 3. tizedes után kerekít, így a memóriabeli mennyiség és napló eltérhet a tárolttól, és 10^9 fölött a mentés elbukik.
- **Kockázat:** Közepes. A napló-invariáns (mennyiség = a mozgások összege) csendben sérülhetne.
- **Ellenőrzési módszer:** Mérés PostgreSQL 18.6-on (`postgres:18` image, Claude Code): `SELECT 1.2345::numeric(12,3)` → `1.235`; `SELECT 1000000000::numeric(12,3)` → `ERROR: numeric field overflow`. Teszt: `More_than_three_decimals_in_base_units_is_rejected`, `A_quantity_above_the_column_capacity_is_rejected` ([PantryItemTests.cs](../../tests/KamraApp.Unit.Tests/PantryItemTests.cs)).
- **Eredmény:** **PASS.** Az állítás igaz volt; a Domain most alapegységben legfeljebb 3 tizedest és 999 999 999,999-et fogad el.
- **Következtetés:** A korlát a [data_model.md](../03_design/data_model.md) PantryItems szakaszában; a felhasználóbarát validáció (400) a US-1 API-val készül.

### V-25 – A Domain invariáns-kivételei 500-ként érnének el a felhasználóig (Gemini-review)
- **Dátum:** 2026-10-10
- **Állítás:** Az S1 Gemini-reviewja (Antigravity, Gemini 3.1 Pro) azt állította, hogy a Domain `ArgumentException`-jei a későbbi végpontokon kezeletlenül, 500-as hibaként jutnának el a felhasználóig; javaslata saját `DomainException` vagy validáció az Application rétegben.
- **Kockázat:** Közepes. Hibás bemenetre a felhasználó „Váratlan hiba” üzenetet kapna, és a hibaarány torzulna.
- **Ellenőrzési módszer:** Kódvizsgálat (Claude Code): [AppExceptionHandler.cs](../../src/backend/KamraApp.Api/ErrorHandling/AppExceptionHandler.cs) – az `AppException`-ön és a `BadHttpRequestException`-ön kívül minden kivétel 500 `INTERNAL_ERROR`. A reviewer által megadott sor (`PantryItem.cs:219`) nem létezik (a fájl 125 soros), a megállapítás tartalma ettől független.
- **Eredmény:** **PASS.**
- **Következtetés:** A `DomainException` helyett a már tervezett megoldás marad: a használati eset validál a Domain hívása előtt (ADR-0004: validáció az Application rétegben; a Domain nem hivatkozhat az `AppException`-re). A US-1 API negatív integrációs tesztjei minden Domain-invariánsra 400 `VALIDATION_FAILED`-et ellenőriznek.
