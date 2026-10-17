# AI Manifest

A Kamra projekt AI-first módon készül. Ez a dokumentum leírja, **milyen AI eszközöket, mire és milyen korlátokkal** használok.
Élő dokumentum: a fejlesztés során folyamatosan bővül. Részletek: [prompt_log.md](prompt_log.md), [verification_log.md](verification_log.md).

Utolsó frissítés: 2026-10-11

---

## 1. Használt eszközök és verziók

| Eszköz | Típus | Modell / verzió | Szerep |
|---|---|---|---|
| Claude Code | CLI ágens | claude-sonnet-4-6, claude-opus-5-5 | Architect / Tutor, CLI Agent: tervezés, ADR; a walking skeleton és az 1. lépcső implementálója (kód, tesztek, Docker, CI); elő-review a `/code-review` (két külön subagent: kódolási szabályok és spec) és a `/security-review` skillekkel (ugyanaz a modellcsalád, ezért nem független) |
| Copilot CLI | CLI ágens | Claude Sonnet 4 | Claude Code alternatívája |
| GitHub Copilot | IDE asszisztens | Claude Sonnet 4, gpt-5.6-luna | Pair Programmer: implementáció, boilerplate, unit tesztek; Reviewer: dokumentum-review |
| Antigravity | Agentic IDE | Gemini 3.1 Pro (High); 2026-10-17-től Claude Sonnet 4.6 | Reviewer: keresztvalidáció, dokumentációs audit; független kódreview szakaszonként és ellenséges tesztek (auth, készlet, főzés) |
| Gemini chat | Chat LLM | Gemini 3.1 Pro | Scope tervezés, debug, trianguláció |

## 2. Felhasználási területek

| Terület | Elsődleges eszköz | Mire | Példa (prompt log) |
|---|---|---|---|
| Tervezés (scope, architektúra, ADR) | Claude Code | MVP, non-goals, ADR-alapanyag, trade-off elemzés, kikérdezéses (grilling) tervezés | [P-03–P-05](prompt_log.md) |
| Kód | Claude Code | Walking skeleton: rétegek, hibamodell, naplózás, auth, frontend, Docker | [P-13](prompt_log.md) |
| Teszt | Claude Code, Antigravity | Spec-first auth-tesztek (ADR-0006 Verification), ellenséges tesztek (Antigravity), Vitest, Playwright | [P-13](prompt_log.md), [V-10](verification_log.md) |
| Review | Antigravity, GitHub Copilot | Edge case, security, architekturális konzisztencia, dokumentációs audit | [P-06, P-07, P-10](prompt_log.md) |
| Debug | Claude Code, Gemini chat | Stacktrace-elemzés, root cause hipotézisek, regressziós teszt | – |
| Dokumentáció | Claude Code | README, API leírás, docs skeleton | [P-01](prompt_log.md) |
| CI/CD | Claude Code | GitHub Actions (build, format, lint, tesztek, OpenAPI-diff, audit, gitleaks, e2e), dependabot, Docker Compose | [P-13](prompt_log.md) |

### Munkamódszer

- **Workflow:** generál → review → teszt → integrál (kézi ellenőrzés + commit). Részletek: [AGENTS.md](../../AGENTS.md).
- **Keresztvalidáció:** aki a kódot írta, nem az reviewzza – Claude generál, Antigravity (Gemini) reviewz, a végső elfogadás az enyém. A reviewer állításait is ellenőrzöm (lásd [V-04](verification_log.md)).
- **Ellenséges teszt:** kritikus moduloknál egy másik modell próbálja eltörni a kódot.
- **Spec-first (TDD):** komplex moduloknál előbb specifikáció és teszt, utána implementáció.
- **Trianguláció:** fontos architekturális döntésnél két modell véleménye; az ellentmondás az ADR része lesz.
- **Commitonkénti átnézés:** minden commit előtt átnézem a diffet; a Claude Code minden commitnál kipipált AI-review checklistet mutat (lent).
- **Szakaszonkénti review-kapu (walking skeleton):** elő-review (Claude Code `/code-review`, az auth-szakaszban `/security-review`) → független review (Antigravity) → **a review után `git status`: a reviewer nem hozhat létre fájlt** → kritikus modulnál (auth, készlet, főzés) ellenséges teszt → szétválogatás forrással (valós / részben / már benne van / téves / döntést igényel) → javítás csak jóváhagyás után → commit → push. A promptsablonok verziózva: [review_prompts.md](review_prompts.md).
- **Spec-first szakaszonként (1. lépcső):** mini-spec → jóváhagyás → piros tesztek `NotImplementedException`-vázakkal (`test:`) → implementáció (`feat:`) → a „zöld” jelentés előtt minden CI-kapu helyben, a `dotnet format` is → reviewk → `docs:`.
- **Kikérdezés:** a megértés-ellenőrző kérdéseket (MI-eszköztár 27.) a walking skeletonban saját döntésem alapján kihagytam; helyette minden szakasz összefoglalóját és a diffet néztem át.

### AI-review checklist (generált kódnál, commitonként)

- [ ] Nincs tiltott rétegfüggés; nincs üzleti logika controllerben vagy komponensben.
- [ ] Minden háztartáshoz kötött hívás `HouseholdId`-t kap (az `ICurrentHousehold`-ból).
- [ ] Minden hibaút `AppException` → ProblemDetails, stabil `code`-dal; nincs stack trace a válaszban.
- [ ] Nincs PII, jelszó vagy cookie a naplóban.
- [ ] Nincs titok, `.env` vagy beégetett connection string a diffben.
- [ ] Új logikához új teszt, negatív esettel; a teszt ténylegesen elbukik, ha a szabályt megsértjük.
- [ ] Nincs kitalált API: a nem triviális keretrendszer-hívás lefordul és teszttel lefut, vagy forrás igazolja.
- [ ] A változott viselkedést a vonatkozó dokumentum is tükrözi (AGENTS.md 9. pont).

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
| D-3 | **Kijelentkezéskor minden session visszavonása** | Az ADR-0006 maradó kockázatként elfogadta, hogy a kijelentkezés csak az adott eszközön törli a cookie-t; az ellenséges teszt után a Claude Code a javítást és az elfogadott kockázat megtartását is opcióként kínálta | A javítást választottam: a kijelentkezés új security stampet ad, és a cookie-t minden kérésnél ellenőrizzük, így egy ellopott cookie a kijelentkezés után használhatatlan. Egy elfogadott ADR-kockázatot írtam felül, mert egy teszt megmutatta a valós hatást, a javítás ára (kérésenként egy lekérdezés) pedig kicsi. | [V-11](verification_log.md), [ADR-0006](../02_architecture/adr/0006-cookie-auth-identity.md), [P-13](prompt_log.md) |

## 5. Kockázatok és kezelésük

| Kockázat | Mi a baj, ha bekövetkezik | Kezelés |
|---|---|---|
| Hallucináció (nem létező API, csomag, metódus) | Nem fordul, vagy rejtett hibás viselkedés | Build + tesztek zöldek kell legyenek; új függőség csak jóváhagyással; dokumentációban csak implementált dolog |
| Hibás security tanács | Auth bypass, injection, adatszivárgás | Minden security állítás a [verification_log](verification_log.md)-ba kerül; keresztvalidáció másik modellel; negatív tesztek |
| Hibás teljesítmény-állítás | Lassú lekérdezés, rossz UX | Mérés vagy PoC a verification logban |
| Licenc | Nem kompatibilis licencű kód/csomag | Függőségek licencének ellenőrzése (dependency scan); részletek: `privacy_licensing.md` |
| Prompt injection (az alkalmazás LLM-funkcióiban) | Az LLM kimenete jogosulatlan műveletet indít | LLM kimenet séma-validáció DB előtt; az AI kimenete csak felhasználói jóváhagyással kerül az adatbázisba; az MCP toolok csak olvasnak ([mcp_tools.md](../03_design/mcp_tools.md)) |
| Megértés hiánya | Nem tudom megvédeni a kódot | Commitonkénti diff-átnézés, szakaszonkénti összefoglaló, kis commitok; a döntések a prompt logban |
| Tesztek gyengítése a zöld CI kedvéért | Hamis biztonságérzet | AGENTS.md tiltja; teszt módosítás csak indoklással és jóváhagyással |

## 6. Tanulságok – AI hibák és korlátok

> A fejlesztés során talált AI hibák összefoglalója, hivatkozással a verification log bejegyzésre.

- **Az AI túlértékeli a termék egyediségét ([V-01](verification_log.md)).** Az értékajánlathoz javasolt „csak nálunk van” állítás egy forrásellenőrzés után megdőlt: a szabad szöveges, több tételes bevitel más eszközökben is létezik. Tanulság: az AI által javasolt egyediségi és versenytárs-állítást mindig forrással ellenőrzöm, és negatív állítást („más appban nincs”) nem teszek, mert nem igazolható.
- **A forrás pontos olvasása a problémát is szűkítette ([V-02](verification_log.md)).** Az AI által javasolt pazarlási adat az elsődleges forrás (NÉBIH) szerint helyes volt, de a forrás a kidobás három okát sorolja fel, ebből a termék csak kettőt (megfeledkezés, túlvásárlás) céloz. Tanulság: számszerű állítást csak az elsődleges forrásból veszek át, és azt is ellenőrzöm, hogy a forrás pontosan azt támasztja-e alá, amit állítok, nem csak a számot.
- **A reviewer AI nem dönt helyettem ([P-10](prompt_log.md), [V-04](verification_log.md)).** A UX-validálás során a reviewer (Antigravity) „hivatalosan engedélyezett” egy új függőséget (axe-core), és korábban téves állítást tett a modellnevekről. Tanulság: a reviewer AI megállapításai javaslatok; új függőségről és minden döntésről én döntök, és a reviewer állításait is ellenőrzöm.
- **Az üres review nem bizonyíték ([V-10](verification_log.md), [V-11](verification_log.md)).** Az auth-szakasz független kódreviewja egyetlen hibát sem talált, a saját biztonsági review sem; ugyanannak a modellnek (Antigravity) az ellenséges tesztjei viszont két valódi hibát mutattak meg (párhuzamos kérésekkel megkerülhető fiókzárolás, kijelentkezés után is érvényes cookie). Tanulság: kritikus modulnál a review mellé futtatható, támadó szemléletű teszt kell.
- **A saját AI-javaslatom is lehet hibás ([V-12](verification_log.md)).** Egy biztonsági „szigorítás” (antiforgery cookie `Secure = Always`), amelyet a Claude Code javasolt és a tesztek sem buktattak el, HTTP-n teljesen megakadályozta volna a bejelentkezést; az e2e teszt valódi böngészőben mutatta meg. Tanulság: a biztonsági beállítást a valódi telepítési környezetben (HTTP, böngésző) is ellenőrizni kell, mert a HTTPS-es tesztkliens elfedheti a hibát.
- **A reviewer elavult tudásból is állít ([V-08](verification_log.md)).** A Gemini a .NET 10 tesztfuttató-beállítását „hallucináltnak” nevezte a .NET 8/9 tudása alapján; kísérlet és forrás cáfolta. Tanulság: keretrendszer-verziós állítást kísérlettel ellenőrzök.
- **A reviewer nem tartja be a tiltásokat.** A promptban tiltott fájlírást a reviewer négyszer is megkísérelte (diff-fájlok, próbafájl); a parancsfuttatási engedélykéréseknél ezért mindig ellenőrzöm, nincs-e benne fájlba írás (`>`). Tanulság: az AI eszköz korlátait technikai kontrollal (engedélykérés) érdemes kikényszeríteni, nem csak a prompttal. Az 1. lépcsőben további két alkalommal (S2, S3) hozott létre fájlt; azóta minden review után `git status` következik.
- **Az AI-írt teszt is lehet vakon zöld ([V-30](verification_log.md)).** Az S4-ben egy rangsorteszt egy szabályváltozás után elvesztette az elkülönítő erejét; az S5-ben a Gemini „hibás JSON” ellenséges tesztje el sem jutott a szerverig, és két saját tesztem a Layout üres hibarégióját találta meg az oldal hibaüzenete helyett. Tanulság: új vagy módosított tesztnél ellenőrizni kell, hogy hibás kód mellett elbukna-e (mutáció).
- **Azonos modellcsaládú reviewer, kritikusnak jelölt téves állítással ([V-38](verification_log.md), [V-39](verification_log.md)).** A T1 reviewja (Claude Sonnet 4.6) „Kritikus” SQL-injekciót jelzett egy paraméterezett lekérdezésben, és három már meglévő tesztet hiányzónak nevezett. Tanulság: a súlyosság nem bizonyíték; minden állítást a kódon és forrással ellenőrzök, és a reviewer modellcsaládját a naplóban rögzítem.
- **A formázó csonkolhatja a nem forduló fájlt ([V-41](verification_log.md)).** Egy szintaktikailag hibás tesztfájlon futtatott `dotnet format` négy tesztet törölt; a tesztszám csökkenéséből derült ki. Tanulság: formázás csak forduló kódon, és a tesztszámot minden futás után összevetem az előzővel.
- **A spec szerinti review valódi hibát talált ([V-32](verification_log.md)).** A `/code-review` spec-ügynöke a kg-os recept és a grammos készlet kombinációját végigkövetve olyan hibát talált, amelyet sem a tesztek, sem az implementáló nem vett észre. Tanulság: a két tengelyt (kódolási szabályok és spec) érdemes külön ügynökkel vizsgálni.
- **Helyi kapu kimaradt, a CI piros lett (S5).** Az AI „zöldet” jelentett a `dotnet format` ellenőrzés nélkül, a CI backend-jobja elbukott. Tanulság: a „zöld” jelentés előtt minden CI-kaput helyben kell futtatni.

