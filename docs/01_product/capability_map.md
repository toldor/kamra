# Capability Map

A termék képességei és megvalósítottsági állapotuk: mennyi a felhasználói érték (**Value**) és mennyi a termékminőség (**Productization**: biztonság, üzemeltethetőség, hibakezelés, minőségbiztosítás). A story-k és az ütemezés forrása: [scope_contract.md](scope_contract.md).

**Frissítés:** minden lépcső végén. „Done” státusz csak evidence- és tesztlinkkel adható. Evidence: repón belüli relatív link (screenshot vagy GIF a `docs/assets/` alatt) vagy CI-futás link. Teszt: konkrét tesztfájl vagy tesztosztály.

## Képességek

| Capability | Kategória | Evidence (link) | Teszt (link) | Státusz |
|---|---|---|---|---|
| CAP-01 Készlet rögzítése és áttekintése (US-1) | Value | – | – | Planned |
| CAP-02 Bevásárlás rögzítése egy mondattal (US-2) | Value | – | – | Planned |
| CAP-03 „Mit főzzek?” – ajánlás a készletből (US-3) | Value | – | – | Planned |
| CAP-04 Főzés és a készlet automatikus frissítése (US-4) | Value | – | – | Planned |
| CAP-05 Bevásárlólista javaslatokkal (US-5) | Value | – | – | Planned |
| CAP-06 Kérdezés a kamráról élő nyelven (US-6) | Value | – | – | Planned |
| CAP-07 Bejelentkezés és háztartásonkénti adatelkülönítés | Productization | [CI](https://github.com/toldor/kamra/actions/runs/37157528207); [e2e](../../tests/e2e/auth.spec.ts) (regisztráció → üres készlet); [V-10](../07_ai/verification_log.md), [V-11](../07_ai/verification_log.md) | [AuthTests.cs](../../tests/KamraApp.Integration.Tests/AuthTests.cs), [AdversarialAuthTests.cs](../../tests/KamraApp.Integration.Tests/AdversarialAuthTests.cs), [AuthUseCaseTests.cs](../../tests/KamraApp.Unit.Tests/AuthUseCaseTests.cs) | Partial – a bejelentkezés kész; az adatelkülönítés (S-2) az első háztartáshoz kötött végponttal (US-1) jön |
| CAP-08 Készletváltozások naplózása okkal | Productization | – | – | Planned |
| CAP-09 Folyamatos minőségellenőrzés (build, lint, tesztek, lefedettség, függőségvizsgálat) | Productization | [CI](https://github.com/toldor/kamra/actions/runs/37157528207); [ci.yml](../../.github/workflows/ci.yml); [branch-védelem](../assets/branch-ruleset.png) | [test_report.md](../04_quality/test_report.md) (61 teszt) | Partial – a lefedettségi küszöb (kapu) a test_strategy.md-vel jön |
| CAP-10 Üzemeltethetőség: strukturált naplózás és állapotfigyelés | Productization | [observability.md](../05_security_ops/observability.md) (naplóminta, `/health`); [V-14](../07_ai/verification_log.md) | [ErrorHandlingTests.cs](../../tests/KamraApp.Integration.Tests/ErrorHandlingTests.cs), [StartupTests.cs](../../tests/KamraApp.Integration.Tests/StartupTests.cs) | Partial – metrikák és deploy runbook még nincsenek |
| CAP-11 Működés AI-kiesés esetén | Productization | – | – | Planned |
| CAP-12 A siker mérése: North Star és guardrail metrikák | Productization | – | – | Planned |

**Státuszok:** `Planned` · `Partial` · `Done`

## Mérés és kockázat

| Capability | Mérés | Kockázat |
|---|---|---|
| CAP-01 | G2 korai megtartás ([metrics.md](metrics.md)) | R2 pontatlan készlet ([vision.md](vision.md)) |
| CAP-02 | G3 bevitel ideje: p50 ≤ 40 mp, p90 ≤ 90 mp | R1 hibás AI-kimenet |
| CAP-03 | North Star: megmentett főzések | R3 hideg indulás, R4 hozzávaló-egyeztetés |
| CAP-04 | North Star; G1 pazarolt arány | R4 hozzávaló-egyeztetés (rossz levonás) |
| CAP-05 | Mérési ötlet: a bevásárlójavaslatok elfogadási aránya | Zajos, ismétlődő javaslatok → elutasítási szabály (US-5) |
| CAP-06 | Mérési ötlet: a chat-kérdések hány százalékára érkezik hibamentes válasz | Prompt injection → csak olvasó MCP toolok ([mcp_tools.md](../03_design/mcp_tools.md)) |
| CAP-07 | Az adatelkülönítési integrációs tesztek 100%-a zöld | Más háztartás adatának elérése azonosító ismeretében |
| CAP-08 | Minden készletváltozás csökkenési okkal naplózott (invariáns teszt) | Napló nélküli módosítás torzítja a G1-et |
| CAP-09 | A `main` ágon a CI-futások zöld aránya; Domain és Application lefedettség ≥ 80% | Instabil (flaky) tesztek |
| CAP-10 | A health check válaszol; minden hiba `correlationId` alapján visszakereshető | Személyes adat, prompt vagy API-kulcs a naplóban |
| CAP-11 | LLM p95 válaszidő ≤ 10 mp; az időtúllépések aránya | Külső API kiesése → kézi form, AI-funkciók kikapcsolhatók |
| CAP-12 | A metrika-lekérdezések a szintetikus seed-adaton egyeznek a kézzel számolt értékekkel | Felhasználó által rögzített adatból fakadó torzítás |

## Roadmap

Céldátumok a [scope_contract.md](scope_contract.md) ütemezése szerint; ha az ütemezés változik, a scope_contract az irányadó.

| Lépcső | Céldátum | Capability-k |
|---|---|---|
| Alapok | okt. 4. | CAP-07, CAP-09 (bővül a teljes fejlesztés alatt) |
| 1. lépcső – determinisztikus mag | nov. 1. | CAP-01, CAP-03, CAP-04, CAP-05, CAP-08, CAP-10 |
| 2. lépcső – AI-bevitel | nov. 15. | CAP-02, CAP-11 |
| 3. lépcső – chat és mérés | nov. 22. | CAP-06, CAP-12 |

- **Csúszás esetén** először a CAP-06 szűkül; az 1. és a 2. lépcső képességei védettek.
- **Stretch** (nem vállalt, csak ha a 3. lépcső a nov. 22-i feature freeze előtt kész és zöld):
  - *AI-receptötlet mentése jóváhagyással:* a hideg indulást az induló receptkészlet már kezeli, ez csak kényelmi bővítés.
  - *AI-os helyettesítés és adagjavaslat:* a lineáris adagskálázás az MVP-ben van; az AI-os rész nem szükséges a fő flow-hoz.
  - *Kipipált bevásárlólista-tételből készletbevitel:* a készletbevitel e nélkül is működik; ez a kör zárását kényelmesíti.

## Ismert hiányosságok

- Egyik képesség sincs még implementálva, ezért evidence- és tesztlink sincs.
