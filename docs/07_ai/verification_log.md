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

### V-26 – Az OpenAPI-generátor nem az MVC JSON-beállításait olvassa (mérés)
- **Dátum:** 2026-10-10
- **Állítás:** Az AI az S2 piros fázisában azt feltételezte, hogy a build közben generált `openapi.json` azért írja le a `Unit`/`Category` enumot `integer`-ként, mert a `JsonStringEnumConverter` csak az MVC JSON-beállításaiban (`AddJsonOptions`) szerepel, a `Microsoft.AspNetCore.OpenApi` pedig a minimal API (`Microsoft.AspNetCore.Http.Json.JsonOptions`) beállításait használja; futás közben viszont az MVC stringeket küld.
- **Kockázat:** Közepes. A generált frontend-típusok számot várnának ott, ahol a szerver szöveget küld, és a contract-ellenőrzés ezt nem venné észre.
- **Ellenőrzési módszer:** Mérés (Claude Code) a buildelt `openapi.json`-on: csak `AddJsonOptions`-szel `Unit: {"type":"integer"}`; a `ConfigureHttpJsonOptions` hozzáadása után `Unit: {"enum":["g","dkg","kg","ml","dl","l","db"]}`. Ugyanígy a számkezelés: az alapértelmezett webes beállításokkal az `amount` sémája `number | string` mintával, a `JsonNumberHandling.Strict` után `number`. Futás közben: `"200"` mennyiség → 400 ([PantryApiTests.cs](../../tests/KamraApp.Integration.Tests/PantryApiTests.cs) `Invalid_input_returns_400_not_500`).
- **Eredmény:** **PASS.** A feltételezés igazolódott.
- **Következtetés:** Mindkét JSON-beállítás ugyanazt a konvertert és számkezelést kapja ([Program.cs](../../src/backend/KamraApp.Api/Program.cs)); a CI contract-diffje azóta a string enumokat és a szigorú számokat őrzi.

### V-27 – A Scalar API-felület működik-e a szigorú CSP mellett (mérés)
- **Dátum:** 2026-10-10
- **Állítás:** Az AI (a Q7 döntésénél) azt állította, hogy a `Scalar.AspNetCore` 2.17.14 (MIT, net10.0, NuGet-függőség nélkül) a felületét a csomagba ágyazva, CDN nélkül szolgálja ki, de a beágyazott `<script type="module">` miatt a jelenlegi `default-src 'self'` CSP-vel csak lazítással működik.
- **Kockázat:** Közepes. Túl tág lazítás az egész alkalmazás XSS-védelmét gyengítené; lazítás nélkül a fejlesztői felület nem használható.
- **Ellenőrzési módszer:** Forrás: a letöltött csomag tartalma és XML-dokumentációja (`WithNonce()`: kérésenkénti nonce a `HttpContext.Items`-ben; `DisableDefaultFonts()`: a CDN-betűk kikapcsolása). Mérés (Claude Code): az Api `Development`-ben, a `/scalar/` oldal Chromiumban (Playwright) – (1) csak script-nonce-szal: 7 CSP-sértés (inline stílusok), a felület nem jelenik meg; (2) `style-src 'self' 'unsafe-inline'`-nal, csak a Scalar-oldalon: 0 CSP-sértés, 0 külső kérés, a végpontok megjelennek. Integrációs teszt: az API-válaszok CSP-je `default-src 'self'`, a Scalaré nonce-os, `script-src 'unsafe-inline'` nélkül; `Production`-ben a `/scalar` és az `/openapi/v1.json` 404 ([PantryApiTests.cs](../../tests/KamraApp.Integration.Tests/PantryApiTests.cs), [AdversarialPantryApiTests.cs](../../tests/KamraApp.Integration.Tests/AdversarialPantryApiTests.cs)).
- **Eredmény:** **PASS, korláttal.** A scriptek nonce-szal védettek; az inline stílusokra nonce nem alkalmazható (style-attribútumok), ezért ott `'unsafe-inline'` kell.
- **Következtetés:** A lazítás csak a Scalar-oldal válaszára és csak `Development`-ben érvényes; az ADR-0007 16. alternatívája rögzíti.

### V-28 – Bezárás nélkül megszüntetett modális dialog után elvész a fókusz (Gemini-review)
- **Dátum:** 2026-10-11
- **Állítás:** Az S3 Gemini-reviewja (Antigravity, Gemini 3.1 Pro) azt állította, hogy ha a `<dialog>`-ot (`showModal`) a React a `close()` hívása nélkül távolítja el, a böngésző nem adja vissza a fókuszt a megnyitó „Módosítás” gombra, így a billentyűzetes használat megtörik (ux_flows: „a fókusz mindig látható”). A reviewer súlyossága „Kritikus” volt.
- **Kockázat:** Közepes. A flow billentyűzettel végigvihető, de mentés után a fókusz a lap elejére ugrik.
- **Ellenőrzési módszer:** Mérés valódi böngészőben (Claude Code): Playwright, Chromium, asztali és 360 px-es nézet, Docker Compose stack ellen. A teszt (`returns the focus to the edit button after saving`, [pantry.spec.ts](../../tests/e2e/pantry.spec.ts)) a javítás előtt elbukott (`toBeFocused` – „Received: inactive”); a javítás után (`close()` a dialog eltávolítása előtt, [PantryItemEditor.tsx](../../src/frontend/src/PantryItemEditor.tsx)) három egymás utáni futásban zöld. A jsdom ezt nem tudja ellenőrizni, ezért e2e teszt készült.
- **Eredmény:** **PASS.** Az állítás igaz volt; a súlyosság túlzó (nem akadályozza a használatot).
- **Következtetés:** A szerkesztőablak mentésnél, „Mégse”-nél és közben törölt tételnél is előbb bezárul; törléskor a megnyitó gomb megszűnik, ezért ott a fókusz a lap elejére kerül (ux_flows, Ismert hiányosságok). Megjegyzés: a Spec-elő-review korábban a lecsatoláskori `close()` eltávolítását javasolta; az a hívás amúgy is későn, a dialog eltávolítása után futott volna.

### V-29 – A magyar névsorrend a futtató konténerben is működik (ICU)
- **Dátum:** 2026-10-11
- **Állítás:** Az AI az S4-ben a receptek névsorát magyar kollációval (`StringComparer.Create(CultureInfo.GetCultureInfo("hu-HU"))`) rendezi, és azt állította, hogy ez a futtató `mcr.microsoft.com/dotnet/aspnet:10.0` konténerben is működik, nem csak a fejlesztői gépen.
- **Kockázat:** Közepes. Invariáns globalizációs módban (vagy ICU nélkül) a `hu-HU` kultúra kivételt dob vagy ordinális rendezésre esik vissza, és az „Ágyas” a „Bableves” mögé kerülne.
- **Ellenőrzési módszer:** Mérés (Claude Code): `docker run --rm --entrypoint sh mcr.microsoft.com/dotnet/aspnet:10.0 -c 'ls /usr/lib/x86_64-linux-gnu | grep icu; env | grep GLOBALIZATION'` → `libicudata.so.74`, `libicui18n.so.74`, nincs `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT`. Teszt: `Equal_soon_expiring_counts_rank_by_the_earliest_expiry_then_by_Hungarian_name` ([RecommenderTests.cs](../../tests/KamraApp.Unit.Tests/RecommenderTests.cs)) Windowson és a Linuxos CI-runneren ([run 38138477380](https://github.com/toldor/kamra/actions/runs/38138477380)).
- **Eredmény:** **PASS.**
- **Következtetés:** A magyar névsor a konténerben is érvényes; ha a projekt valaha invariáns globalizációs módra váltana, ez a teszt jelezné.

### V-30 – Egy szabályváltozás után az elkülönítő teszt elvesztette az erejét (Gemini-review)
- **Dátum:** 2026-10-11
- **Állítás:** Az S4 Gemini-reviewja (Antigravity, Gemini 3.1 Pro) azt állította, hogy a `More_soon_expiring_items_used_rank_first_even_against_an_earlier_expiry_and_name` teszt zöld, de nem azt ellenőrzi, amit a neve állít: az E1 javítás (a lejárt tétel nem számít a rangsor 2. kulcsánál) után a versengő recept lejárt tétele már nem ad korábbi dátumot, így a teszt a 2. kulcs szerint is átmenne, az 1. kulcsot nem különíti el.
- **Kockázat:** Közepes. Az 1. kulcs (a felhasznált hamarosan lejáró tételek száma) hibája észrevétlen maradna – ez a pazarlás csökkentésének fő rangsorszabálya.
- **Ellenőrzési módszer:** Mutációs mérés (Claude Code): az 1. kulcsot ideiglenesen kivettük a `Recommender`-ből – a **régi** teszt zöld maradt (igazolva, hogy nem ellenőrizte); a tesztet átírtuk (a versengő recept tétele ma jár le, így a 2. kulcs és a név is őt favorizálja) – az 1. kulcs nélkül az **új** teszt elbukik, vele zöld; a kód visszaállítva.
- **Eredmény:** **PASS.** A reviewer állítása igaz volt; sem az implementáló, sem az elő-review nem vette észre.
- **Következtetés:** Egy szabály módosítása után a szomszédos, ugyanazt a rangsort érintő tesztek elkülönítő erejét is újra kell ellenőrizni; a rangsor-kulcsok tesztjei azóta mutációval igazoltak. A független, más modellel végzett review itt valódi hibát talált a tesztekben (ai_manifest, Munkamódszer).

### V-31 – Az üres GUID kérésazonosító átment a validáción (Gemini-review)
- **Dátum:** 2026-10-11
- **Állítás:** Az S5 Gemini-reviewja (Antigravity, Gemini 3.1 Pro) szerint a `ConfirmCookingRequest.RequestId` `[Required]` attribútuma a `Guid?` típuson csak a null-t szűri, az üres GUID-ot (`00000000-0000-0000-0000-000000000000`) nem.
- **Kockázat:** Közepes. Egy hibás kliens, amely mindig üres azonosítót küld, az első főzés után minden azonos tartalmú főzést csendes ismétlésnek kapna (200 a régi eredménnyel), **levonás nélkül**; eltérő tartalomnál pedig `IDEMPOTENCY_CONFLICT`-ot.
- **Ellenőrzési módszer:** Teszt (Claude Code): új eset az `Invalid_input_is_rejected_with_a_field_error_and_nothing_is_saved` theoryban (`requestId: Guid.Empty`, [CookingUseCaseTests.cs](../../tests/KamraApp.Unit.Tests/CookingUseCaseTests.cs)) – a javítás előtt piros, az elutasítás (`RequestIdMessage`) után zöld; [CI run 38163897618](https://github.com/toldor/kamra/actions/runs/38163897618).
- **Eredmény:** **PASS.**
- **Következtetés:** A `[Required]` egy `Guid?` mezőn csak a hiányt szűri; ahol az azonosító tartalma számít (idempotencia), az üres értéket külön kell elutasítani. A többi végponton az üres azonosító nem létező erőforrásra mutat (404), ott nincs ilyen kockázat.

### V-32 – A kg-os recept alapértelmezett mennyiségét a szerver elutasította (Claude spec-review)
- **Dátum:** 2026-10-11
- **Állítás:** Az S5 `/code-review` spec-ügynöke (Claude Code subagent) szerint a `Units.FromBase` nem kerekít, így kg-os receptnél és 12,5 g készletnél az előnézet alapértelmezett felhasznált mennyisége 0,0125 kg; a megerősítés validációja viszont legfeljebb 3 tizedest fogadott el a recept egységében, ezért az alapértelmezett út is 400-zal bukott volna (a seedben a burgonya kg-ban van).
- **Kockázat:** Közepes. A felhasználó a felkínált értékkel sem tudná rögzíteni a főzést; ha felfelé kerekít, `INSUFFICIENT_STOCK`-ot kap.
- **Ellenőrzési módszer:** Mutációs mérés (Claude Code): a javítás (a pontosság ellenőrzése alapegységben, a recept betöltése után) mellé két új teszt került – [CookingUseCaseTests.cs](../../tests/KamraApp.Unit.Tests/CookingUseCaseTests.cs) (`The_preview_default_in_kg_from_grams_in_stock_is_accepted_as_it_is`) és [CookingApiTests.cs](../../tests/KamraApp.Integration.Tests/CookingApiTests.cs) (`The_preview_default_from_finer_stock_than_the_recipe_unit_can_be_confirmed`); a régi validációt ideiglenesen visszaállítva mindkettő piros, a javítással zöld.
- **Eredmény:** **PASS.**
- **Következtetés:** A pontossági szabályt a tárolás egységében (alapegység, `numeric(12,3)`, ADR-0002, V-24) kell megfogalmazni, nem a megjelenítés egységében. A kódolás közben ezt sem az implementáló, sem a tesztek nem vették észre; a spec szerinti review a valós adatkombinációt (kg-os recept, grammos készlet) végigkövetve találta meg.

### V-33 – Az adatbázis névsora nem a magyar ábécé (Gemini PR-review)
- **Dátum:** 2026-10-11
- **Állítás:** A PR-review (Antigravity, Gemini 3.1 Pro) szerint a hozzávaló- és a készletlista az adatbázisban (`OrderBy(Name)`) a PostgreSQL alapértelmezett kollációjával rendeződik, amelyben „az ékezetes karakterek a Z után” állnak, míg a memóriabeli listák a magyar `Collation.Hungarian` szerint.
- **Kockázat:** Alacsony. A felhasználó ugyanazokat a neveket különböző képernyőkön eltérő sorrendben látná.
- **Ellenőrzési módszer:** Mérés (Claude Code) egy friss `postgres:18` konténerben: `SELECT datcollate, datlocprovider FROM pg_database` → `en_US.utf8`, `c` (libc). Ugyanaz a névlista `ORDER BY n`-nel: *„ágy, Alma, ananász, …, olaj, őrölt pirospaprika, ostya, uborka, üdítő, …”*; `COLLATE "hu-x-icu"`-val: *„…, olaj, ostya, őrölt pirospaprika, uborka, üdítő, …”*.
- **Eredmény:** **Részben PASS.** Az eltérés valós: a magyarban az ö/ő és az ü/ű külön betű, az `en_US` az o és az u változatának tekinti őket. Az indoklás viszont téves: az `en_US.utf8` az ékezetes betűket nem a Z után, hanem az alapbetű mellé rendezi (az „ágy” az „Alma” előtt van).
- **Következtetés:** A felhasználónak szóló névsort egy helyen, az Application rétegben, a Domain kollációjával rendezzük (`ListIngredients`, `ListPantryItems`); a repositoryk nem rendeznek. Tesztek: [PantryUseCaseTests.cs](../../tests/KamraApp.Unit.Tests/PantryUseCaseTests.cs) (*olaj, ostya, őrölt pirospaprika*), a javítás előtt pirosak; [CI run 38174181800](https://github.com/toldor/kamra/actions/runs/38174181800).

### V-34 – A kis- és nagybetű nem változtat a magyar betűrendi helyen (Gemini PR-review)
- **Dátum:** 2026-10-11
- **Állítás:** A PR-review szerint a `Collation.Hungarian` `ignoreCase: false` beállítása miatt a nagy kezdőbetű megváltoztatná a nevek természetes betűrendi helyét, ezért `ignoreCase: true` kellene.
- **Kockázat:** Alacsony. Ha igaz lenne, a nagybetűs receptnevek a kisbetűsek elé vagy mögé csoportosulnának.
- **Ellenőrzési módszer:** Mérés (Claude Code, F#-szkript a `StringComparer.Create(CultureInfo.GetCultureInfo("hu-HU"), false)` összehasonlítóval): a „Paprikás krumpli, lecsó, Bableves, almás pite, Ágyas, Túrós csusza, ostya, őrölt pirospaprika” lista rendezve *„Ágyas, almás pite, Bableves, lecsó, ostya, őrölt pirospaprika, Paprikás krumpli, Túrós csusza”*; `Compare("alma", "Banán")` = −1.
- **Eredmény:** **FAIL.** A nyelvi összehasonlítás a kis- és nagybetűt csak harmadlagos különbségként, holtversenynél veszi figyelembe; a betűrendi helyet nem változtatja meg.
- **Következtetés:** Nincs változtatás. A reviewer nyelvi rendezésről szóló állítását is mérni kell, mert a kultúra-alapú összehasonlítás nem bájtsorrend.

### V-35 – Az egyidejű fogyás elnyelheti a bevásárlójavaslatot (saját tervezés)
- **Dátum:** 2026-10-17
- **Állítás:** READ COMMITTED mellett ha ugyanannak a hozzávalónak két utolsó tételét két kérés egyszerre fogyasztja el, mindkettő a másik tételt még meglévőnek látja, és egyik sem hoz létre bevásárlójavaslatot (write skew); a hozzávalónkénti tranzakciós zár ([ADR-0008](../02_architecture/adr/0008-konkurencia-es-idempotencia.md) 6. pont) ezt kizárja.
- **Kockázat:** Közepes. A javaslat csendben elmaradna, a lista nem tükrözné a valós hiányt.
- **Ellenőrzési módszer:** Teszt és mutáció: a P20 (két tétel egyidejű 0-ra csökkentése, ötször, [ShoppingListApiTests.cs](../../tests/KamraApp.Integration.Tests/ShoppingListApiTests.cs)) a zárral zöld, a zár SQL-jének eltávolításával piros.
- **Eredmény:** **PASS.**
- **Következtetés:** A zár marad; a P20 a regressziós teszt.

### V-36 – Az ismételt elfogadás nem bájtra azonos választ adott (saját teszt)
- **Dátum:** 2026-10-17
- **Állítás:** Az ismételt elfogadás a [ADR-0008](../02_architecture/adr/0008-konkurencia-es-idempotencia.md) szerint az első válasszal azonos törzset ad.
- **Kockázat:** Alacsony. Eltérő ismételt válasz megtörné az idempotencia ígéretét.
- **Ellenőrzési módszer:** Integrációs teszt: az első válasz a memóriából (`200`), az ismétlés az adatbázisból (`200.000`, `numeric(12,3)`) épült.
- **Eredmény:** **FAIL.** Javítva: a válasz mennyisége normalizált (záró nullák nélkül); a spec-review után az elfogadás 204 lett, így törzs sincs.
- **Következtetés:** Az ismételt kérés válaszát mindig ugyanabból a forrásból vagy normalizálva kell építeni.

### V-37 – A nullázható enum a közös OpenAPI-sémát is nullázhatóvá tette (mérés)
- **Dátum:** 2026-10-17
- **Állítás:** A bevásárlólista-válasz nullázható `Unit?` mezője csak a saját mezőjét teszi nullázhatóvá.
- **Kockázat:** Közepes. A generált frontend-típusokban minden egységmező nullázható lenne.
- **Ellenőrzési módszer:** Mérés: a generált `openapi.json` közös `Unit` sémája `null`-t kapott az enum értékei közé; a `npm run lint` (`tsc`) a `PantryItemForm.tsx`-ben hibát jelzett.
- **Eredmény:** **FAIL.** Javítva: a nem kötelező mennyiség a válaszban `{ value, unit }` objektum (`AmountResponse`), így a `Unit` séma változatlan.
- **Következtetés:** Nullázható enumot a válaszban kerülünk; a frontend-típusok újragenerálása minden API-változás után kapu.

### V-38 – „SQL-injekció az advisory lockban” (Claude Sonnet 4.6, Antigravity-review)
- **Dátum:** 2026-10-17
- **Állítás:** A reviewer szerint az `IngredientLocks` kulcsa „előre összerakott stringként szó szerint bekerül az SQL-be”, ezért kritikus SQL-injekció.
- **Kockázat:** Ha igaz lenne, magas (AGENTS.md 7. pont).
- **Ellenőrzési módszer:** Forrás és kód: az EF Core [SQL Queries – Passing parameters](https://learn.microsoft.com/en-us/ef/core/querying/sql-queries#passing-parameters) szerint a `FormattableString` minden lyuka `DbParameter`-ré alakul; az `IngredientLocks.cs`-ben a kulcs C#-változó, amely lyukként (`{key}`) kerül a lekérdezésbe, és két Guidból áll.
- **Eredmény:** **FAIL** (téves állítás).
- **Következtetés:** Nincs változtatás. A „Kritikus” címke nem bizonyíték.

### V-39 – A T1-review további téves állításai (Claude Sonnet 4.6)
- **Dátum:** 2026-10-17
- **Állítás:** A főzés „before” összege a levonás után számolódik; hiányzik a párhuzamos elfogadás/elutasítás, a kettős felvétel és a háztartásközi teszt; a javasolt mennyiség alapegységben jelenik meg.
- **Kockázat:** Közepes, ha igazak lennének.
- **Ellenőrzési módszer:** Kód (`ConfirmCooking.cs`: a `ToDictionary` a `Cooking.Create` előtt fut) és a meglévő tesztek (P13; `ShoppingListApiTests` párhuzamos és háztartásközi tesztjei; unit teszt dl-re).
- **Eredmény:** **FAIL** mind. A review egyetlen valós pontja (nyitott „minimumszint alatti” javaslat törölt minimum mellett mennyiség nélkül) javítva, teszttel.
- **Következtetés:** A reviewer állításait a kódon kell ellenőrizni, nem a diff összefoglalóján.

### V-40 – Ellenséges bevásárlólista-tesztek (Claude Sonnet 4.6, Antigravity)
- **Dátum:** 2026-10-17
- **Állítás:** A US-5 ellenáll az ismétlésnek, a versenynek, a határértékeknek, a kiváltás kijátszásának és a hibaválasz-szivárgásnak.
- **Kockázat:** Magas, ha nem állna ellen (adatkonzisztencia, információszivárgás).
- **Ellenőrzési módszer:** Teszt: 8 ellenséges integrációs teszt; 7 érvényes és zöld ([AdversarialShoppingListApiTests.cs](../../tests/KamraApp.Integration.Tests/AdversarialShoppingListApiTests.cs)), 1 érvénytelen (a közös rendszer-hozzávaló minimumának „törlésére” 404-et várt; háztartásonkénti minimumnál a 204 a helyes), nem felvéve.
- **Eredmény:** **PASS** a kódra.
- **Következtetés:** Új határeset (pontosan 100 000) és szivárgás-ellenőrzés került a tesztek közé.

### V-41 – A formázó teszteket törölt (saját folyamathiba)
- **Dátum:** 2026-10-17
- **Állítás:** A `dotnet format` csak formáz.
- **Kockázat:** Magas. Tesztek csendben eltűnhetnek.
- **Ellenőrzési módszer:** Tesztszám-összevetés: két új teszt mellett a unit tesztek száma 293-ról 291-re csökkent; a diff szerint egy szintaktikailag hibás fájlon (az `unchecked` kulcsszó változónévként) négy tesztmetódus tűnt el.
- **Eredmény:** **FAIL.** A fájl a HEAD-ből helyreállítva, a módosítás újra alkalmazva; minden eltávolított metódus ellenőrizve.
- **Következtetés:** Formázás csak forduló kódon; a tesztszámot minden futás után összevetjük az előzővel.

### V-42 – A spec-review két valós eltérése (Claude Code, spec-review)
- **Dátum:** 2026-10-17
- **Állítás:** Az ismételt elfogadás törölt sor után 404-et ad; a listatétel `{ checked }` PUT-ja törli a mennyiséget.
- **Kockázat:** Közepes. Az idempotencia ígérete sérül; kipipáláskor elveszne a mennyiség.
- **Ellenőrzési módszer:** Kód és teszt (a P13 csak azért volt zöld, mert a teszt újraküldte a mennyiséget).
- **Eredmény:** **PASS** (mindkettő valós). Javítva: az elfogadás 204, az ismétlés is (D-R1); a mennyiség nélküli PUT megtartja a mennyiséget (D-R3); mindkettő teszttel, mutációval ellenőrizve.
- **Következtetés:** A spec-review a teszt-hiányt is megfogta: a teszt az implementáció hibáját kerülte meg.

### V-43 – Az induló receptek saját szövegűek, a mennyiségek reálisak (az MI írta)
- **Dátum:** 2026-10-17
- **Állítás:** A 15 új induló recept elkészítési szövege saját, nem másolt; a mennyiségek 4 adagra reálisak; a 121 új hozzávaló neve, dimenziója és kategóriája helyes; az új receptek nem változtatják meg a kézzel számolt példák eredményét.
- **Kockázat:** Közepes. Másolt szöveg licenckérdést, hibás mennyiség rossz ajánlást és levonást okozna.
- **Ellenőrzési módszer:** A fejlesztő a tervezetet soronként átnézte, és változtatás nélkül jóváhagyta (`kamra_seed_atnezes.md`, 2026-10-17); a beépítés szkripttel, közvetlenül ebből a táblázatból történt, és a spec-review szkripttel karakterre egyezőnek találta. Automatizált teszt: minden receptben van nem alap hozzávaló, az adagszám 1 és 100 közé esik, a név egyedi, a lista mérete a vállalás szerinti ([SeedDataTests.cs](../../tests/KamraApp.Unit.Tests/SeedDataTests.cs)); a P5 és a P7 integrációs tesztje a bővített seeden is pontosan ugyanazt a „majdnem elkészíthető” listát adja.
- **Eredmény:** **PASS**, egy javítással: a tervezet „Tervezési szempontok” pontja azt állította, hogy egyik új recept sem „majdnem elkészíthető” a P1–P10 egyik készletéhez képest sem; a T4 spec-reviewja megmutatta, hogy a **Rizibizi** a rántotta-készlethez (tojás, vaj) képest az (2 hozzávaló hiányzik). Ez a készlet csak főzési példa (P3), ajánlási teszt nem használja, így eredmény nem változik. A fejlesztő döntése: a recept marad, az állítás a P5/P7 készletekre szűkült.
- **Következtetés:** Az AI által írt adatnál a saját tervezési állítást is ellenőrizni kell (itt a spec-review szkriptje tette meg); a védőháló a P5 és a P7 pontos listájú tesztje.

### V-44 – Az e2e két saját hibát talált a bevásárlólista felületén (Claude Code)
- **Dátum:** 2026-10-18
- **Állítás:** A bevásárlólista oldal kész: a Vitest-tesztek (72/72) zöldek.
- **Kockázat:** Közepes. A boltban a felhasználó azt látja, hogy a pipa nem marad meg, ezért újra kattint.
- **Ellenőrzési módszer:** E2E valódi böngészőben (`shopping.spec.ts`, P13), elkülönített stacken; utána regressziós Vitest-tesztek és mutáció.
- **Eredmény:** **FAIL**, két hibával.
  - (1) A jelölőnégyzet vezérelt volt, ezért a mentés válaszáig visszaugrott üresre („Clicking the checkbox did not change its state”). A Vitest nem vette észre, mert a végállapotra várt. Javítás: optimista pipa, sikertelen mentésnél visszaáll; két regressziós teszt (azonnal látszik; hálózati hibánál visszaáll), mutációval ellenőrizve.
  - (2) A javítás után a saját e2e-tesztem nem várt a mentésre, és a `page.reload()` hideg szervernél megszakította a `PUT`-ot. Javítás: a teszt a `PUT` válaszát várja meg; az API hideg újraindítása után 3 × 16/16.
- **Következtetés:** A komponensteszt az azonnali visszajelzést nem bizonyítja; ehhez az e2e és egy kattintás utáni, várakozás nélküli ellenőrzés kell. Optimista felületnél az e2e-nek a kérés végét kell megvárnia, nem a látható állapotot.

### V-45 – A T2 kódreviewja (Claude Code `/code-review`, Claude Opus 5.5)
- **Dátum:** 2026-10-18
- **Állítás:** A reviewer szerint
  - a fókusz elvész, amikor a megnyomott gomb eltűnik;
  - a sikerüzenet nem olvasódik fel;
  - a főzésnél a mennyiség duplán kerülhet a listára;
  - a gombok a frissítés vége előtt újra aktívak;
  - üzleti logika van a komponensekben.
- **Kockázat:** Közepes. Akadálymentesség és duplikált listatétel; azonos modellcsaládú reviewer, ezért a függetlenség gyenge.
- **Ellenőrzési módszer:** Minden állítás a kódon és forrással (ux_flows a11y, K10, AGENTS.md 3. pont); a valósaknál regressziós teszt a javítás előtt (piros), utána mutáció (14/14 elbukott).
- **Eredmény:** **PASS** a valós megállapításokra (fókusz, élő régió, duplikálás, `busy`, `keepUsed`, minimum-súgó, üres minimummennyiség); az „üzleti logika a komponensben” **téves** (meglévő számítás kiemelése, illetve a K1-ben jóváhagyott kliensvezérlés). Két jóváhagyott eltérés a spectől maradt (az azonosító ellenőrzése a P11-ben; a picker interfésze a lint-szabály miatt). Három új felhasználói szöveg jóváhagyással.
- **Következtetés:** A két tengely itt is mást talált: a Standards az akadálymentességet, a Spec a K10-et és a versenyhelyzetet. Az akadálymentességi állításokat (például hogy egy élő régió felolvasódik-e) a Vitest csak a DOM szintjén igazolja; a képernyőolvasós kézi próba a T5 kézi tesztjeibe kerül.

### V-46 – Saját hibák a T3-ban: a sémateszt, a várakozás és a 360 px (Claude Code)
- **Dátum:** 2026-10-23
- **Állítás:** A T3 tesztjei és felülete készen állnak: a piros fázis után az implementációval minden teszt zöld lesz.
- **Kockázat:** Közepes. Egy rossz teszt hamis biztonságot ad; a kilógó mező mobilon nehezen használható.
- **Ellenőrzési módszer:** Teljes futás minden szinten; e2e valódi böngészőben 360 px-en; szkript a kilógó elemekre; a képernyőkép átnézése.
- **Eredmény:** **FAIL**, négy saját hibával.
  - (1) A migráció sématesztje a `pg_constraint` minden sorát várta, de a PostgreSQL 18 a `NOT NULL` megkötéseket is ott tartja. Javítás: a lekérdezés a kulcsokra szűkül; az ellenőrzött tartalom (PK, CASCADE, NO ACTION) nem változott.
  - (2) Három Vitest-teszt a hozzávalósort az űrlap betöltése előtt kereste. Javítás: előbb megvárják az űrlapot.
  - (3) Az e2e kimutatta, hogy 360 px-en a hozzávalósor 8 px-szel kilógott: a `fieldset` alapértelmezett `min-width: min-content`-je, margója és térköze. A Vitest ezt nem látja (jsdom). Javítás CSS-sel; utána a szkript 0 kilógó elemet talált, az e2e 18/18.
  - (4) A képernyőképen a sorok címkéje a mező mellé került, és az elkészítés gépírásos betűvel jelent meg. Javítás CSS-sel.
- **Következtetés:** A sématesztet az adatbázis tényleges verzióján kell megírni. A mobil elrendezést csak az e2e és a képernyőkép mutatja meg, ezért a képernyőkép átnézése az ellenőrzés része, nem csak dokumentáció.

### V-47 – A T3 kódreviewja (Claude Code `/code-review`, Claude Opus 5.5)
- **Dátum:** 2026-10-23
- **Állítás:** A reviewer szerint
  - archivált receptnél a megerősítés hibája után az űrlap aktív marad, link nélkül;
  - az „Új hozzávaló” mérési módjának hibája és a recept szintű hiba nincs mezőhöz kötve;
  - a kis űrlap nyitásakor és zárásakor elvész a fókusz;
  - az élő régió már az első rendereléskor tartalmazza az értesítést;
  - két port van aggregátumonként, ami az AGENTS.md „aggregate-level repository interfaces” szabályába ütközhet.
- **Kockázat:** Közepes. Akadálymentesség és egy félrevezető hibaállapot; azonos modellcsaládú reviewer.
- **Ellenőrzési módszer:** Minden állítás a kódon és forrással (ux_flows a11y, AGENTS.md 63. sor); a valósaknál regressziós teszt a javítás előtt (7, mind piros).
- **Eredmény:** **PASS** a valós megállapításokra (C1, S1, S2, S3, S5, S7). Az S9-nél a két port marad (nincs generikus repository, a meglévő fake-ek érintetlenek; indok a kódkommentben), a „Válassz…” felirat jóváhagyva. Az S5-höz nincs Vitest-teszt, mert a jsdom nem mutatja meg, felolvas-e a képernyőolvasó; kódolvasással ellenőrizve.
- **Következtetés:** A Spec-tengely olyan hibát talált (C1), amelyet a saját tesztem nem ellenőrzött; a teszt a hibaszöveget nézte, a következő lépést nem.

### V-48 – A T3 biztonsági reviewja (Claude Code `/security-review`)
- **Dátum:** 2026-10-23
- **Állítás:** A változásban nincs High vagy Medium sebezhetőség.
- **Kockázat:** Magas, ha téves: háztartások közti adatszivárgás vagy archivált recept főzése.
- **Ellenőrzési módszer:** Ügynök a `git diff 0d12ebf` felett; az ellenőrzött utak: SQL injection (paraméterezett `FormattableString`), IDOR (recept, hozzávaló, archiválás), a D-c kijátszása, bejelentkezés és antiforgery, mass assignment, XSS, a `sessionStorage`-piszkozat. Az utakat az ellenséges tesztek (V-50) futással is igazolták.
- **Eredmény:** **PASS**.
- **Következtetés:** A statikus vizsgálat és az ellenséges tesztek ugyanarra jutottak; a második futással bizonyít.

### V-49 – A T3 külső szakasz-reviewja (Antigravity, Claude Sonnet 4.6)
- **Dátum:** 2026-10-23
- **Állítás:** A reviewer 15 megállapítást tett. Köztük: az EF a paraméter nélküli konstruktor hiánya miatt nem tudná olvasni a `RecipeArchives`-t; az üres `role="alert"` ne legyen a DOM-ban; a főzési teszt előnézet-mock nélkül fut; a piszkozat-értesítés minden újrapróbáláskor megjelenik; az oldal címe nem kap fókuszt; az archiválás a `CookingErrors`-ra épül.
- **Kockázat:** Alacsony–közepes. Azonos modellcsaládú reviewer; a téves javaslatok elfogadása rontaná az akadálymentességet.
- **Ellenőrzési módszer:** Minden állítás forrással: [Microsoft Learn – EF Core constructors](https://learn.microsoft.com/ef/core/modeling/constructors) (paraméteres konstruktor név szerinti kötése), a kód (`CookingPage.test.tsx:73`, `Layout.tsx:20`, `RecipeForm.tsx` betöltési ág), a ux_flows a11y és a V-45 (élő régió).
- **Eredmény:** **FAIL** öt állításra: az EF-konstruktor, az üres élő régió, a hiányzó mock, az ismétlődő értesítés és a cím fókusza téves. **PASS** a függőség irányára (a `RecipeErrors` a `Recipes` névtérbe került), és a `RecipeArchive` kommentjére. A receptnév egyedisége már eldöntött (N3).
- **Következtetés:** A reviewer az akadálymentességben ellentétes irányú javaslatot is tett (az élő régió eltávolítása); ezt csak a saját, forrással rögzített szabály (V-45) alapján lehetett elvetni.

### V-50 – A T3 ellenséges tesztjei (Antigravity, Claude Sonnet 4.6)
- **Dátum:** 2026-10-23
- **Állítás:** A 8 ellenséges teszt (24 eset) közül 4 elbukik, ami hibát jelezhet az alkalmazásban.
- **Kockázat:** Magas, ha valós: IDOR, archivált recept főzése, névütközés megkerülése.
- **Ellenőrzési módszer:** Futtatás; minden bukásnál a teszt érvényessége a szerződés alapján (a kategórialista, a `used` szabálya, a normalizálás definíciója); a javított teszttel újra.
- **Eredmény:** **FAIL a tesztekre, PASS az alkalmazásra.** Három teszt érvénytelen volt: nem létező kategória (`fruits`, `spices`), a recept hozzávalóinak hiányos felsorolása, és a „zabtej” = „zab tej” elvárás (a belső szóköz összevonódik, de nem törlődik). Javítva (a támadás szándéka szerint, az NBSP-eset „zab tej” = „zab tej” lett) mind a 24 eset zöld. A reviewer a nulla szélességű szóközt kihagyta; ellenőrizve: a .NET-ben nem szóköz, ezért két egyformán kinéző név létrehozható; ismert korlát, csak a saját háztartást érinti.
- **Következtetés:** Az ellenséges teszt bukása először a teszt ellenőrzését kéri; itt egyik sem jelzett alkalmazáshibát.

### V-51 – A Vite biztonsági figyelmeztetései a CI-ban
- **Dátum:** 2026-10-23
- **Állítás:** A `feat:` commit CI-ja piros, mert a frontend sérülékeny csomagot tartalmaz.
- **Kockázat:** Közepes a fejlesztői gépen (a Vite fejlesztői szervere és a `launch-editor`), alacsony élesben.
- **Ellenőrzési módszer:** `npm audit` és `npm audit --omit=dev` helyben; a GitHub-figyelmeztetések (GHSA-698v-4r3x-3vcv, GHSA-r825-w8mm-5pxw, GHSA-587h-7wmj-h5pm, GHSA-9jrq-w75r-8gcw, GHSA-rq7h-c2jc-7f22, GHSA-vfpm-58rq-9qcg).
- **Eredmény:** **PASS a kapura:** a `vite 8.3.2` sérülékeny, a `8.3.5` javított (a `^8.3.0` tartományon belül, csak a lockfile változott, a fejlesztő jóváhagyásával); az éles csomagok tiszták (`--omit=dev`: 0). Utána a CI mind a négy jobja zöld.
- **Következtetés:** A függőség-ellenőrző kapu a saját kódtól függetlenül is megállíthatja az ágat; a javítás külön commitba kerül, hogy a változás nyoma egyértelmű legyen.

### V-52 – Az első telepítés tiszta környezetben 15 percen belül (mérés, MT-02)
- **Dátum:** 2026-10-23
- **Állítás:** A runbook szerint egy tiszta gépen a klóntól az első sikeres `/health`-ig legfeljebb 15 perc telik el (DoD; a V-16 előzménye: kb. 2 perc a fejlesztő gépén, cache nélküli builddel).
- **Kockázat:** Magas, ha téves: a kurzus-kiírás okt. 22-i pontja (első dokumentált telepítés), és a témavezető kérése, hogy kiderüljön, mi hiányzik ahhoz, hogy más is használni tudja.
- **Ellenőrzési módszer:** Mérés. Új WSL-disztró (Ubuntu 24.04.5), Docker Engine 29.9.2 a hivatalos apt-tárolóból, a Docker Desktop leállítva; a fejlesztő a runbookot szó szerint követte, az időpontokat `date -Is` rögzítette, a végén a P13 a böngészőben.
- **Eredmény:** **PASS.** A klóntól a `Healthy`-ig 1 perc 26 másodperc (18:12:14 → 18:13:40); a Docker Engine telepítésével együtt 8 perc 33 másodperc. A P13 végállapota egyezik a kézzel számolttal ([mt02-shopping-360.png](../assets/mt02-shopping-360.png)). Két runbook-hiány derült ki, és javítva: a WSL a Windows-`PATH`-on át a Docker Desktop `docker`-ét is látta, és a disztró a Windows-lemezen indult.
- **Következtetés:** A runbook egy tiszta Linuxon működik. A „tiszta” környezetet ellenőrizni kell (`command -v docker`), mert a WSL alapból átengedi a Windows programjait. A dec. 4-i próba másik gépen, lehetőleg más személlyel lesz (O6).
