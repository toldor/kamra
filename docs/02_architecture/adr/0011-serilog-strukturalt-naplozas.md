# 0011 - Strukturált JSON-naplózás Seriloggal, szerver által generált correlationId-vel

Dátum: 2026-10-03
Státusz: Proposed

## Context

- **Probléma:** hiba esetén meg kell tudni mondani, mi romlott el, hol és melyik kérésben (v1.2 5.3.7). A naplónak strukturáltnak (JSON) kell lennie, minden bejegyzésen `correlationId`-vel, amely a hibaválaszban is megjelenik ([ADR-0007](0007-rest-api-hibamodell.md)), kérésenként egy összefoglaló sorral (metódus, útvonal, státusz, időtartam). PII, jelszó, prompt és API-kulcs nem kerülhet a naplóba (AGENTS.md 5. pont).
- **Kényszerek:** a napló a konténer stdoutjára megy, és `docker compose logs`-szal olvasható ([QA-7](../quality_attributes.md)); külön naplógyűjtő infrastruktúra nincs. Az alkalmazáskód a `Microsoft.Extensions.Logging` `ILogger<T>` absztrakcióját használja. Kb. 200 óra; a naplót a fejlesztés közben is olvasni kell, és mintaként szerepel az [observability.md](../../05_security_ops/observability.md)-ben és a deploy runbookban.
- **Érintett minőségi attribútumok:** megfigyelhetőség (v1.2 F kategória), QA-1 (nincs adatszivárgás a naplóban), QA-3 (az LLM-kiesés naplóból mérhető).

## Decision

- Az Api a Serilogot használja a `Microsoft.Extensions.Logging` mögött: JSON (`RenderedCompactJsonFormatter`) a konzolra, kérésenként egy összefoglaló sor (`UseSerilogRequestLogging`), a `correlationId` pedig a szerver által generált `TraceIdentifier`, amely a `LogContext`-ből lapos mezőként kerül minden bejegyzésbe, és az `X-Correlation-Id` válaszfejlécben, valamint a hibaválaszban is megjelenik.

## Alternatives

1) **Beépített naplózás (`AddJsonConsole` + `AddHttpLogging`)** – előny: nincs új függőség; a `CombineLogs` kérésenként egy bejegyzést ad. Hátrány: a scope-ok (így a `correlationId`) a JSON-ban egy `Scopes` tömbön belül, a kérés mezői egy beágyazott `State` objektumban jelennek meg, így a napló nehezebben olvasható és kereshető. Ez volt az eredeti AI-javaslat (Claude Code); az Antigravity (Gemini 3.1 Pro) reviewja után a lapos szerkezet miatt a fejlesztő a Serilogot választotta ([P-13](../../07_ai/prompt_log.md)).
2) **OpenTelemetry (naplók, trace-ek, metrikák) exporterrel** – előny: iparági szabvány, trace-ek és metrikák is; hátrány: collector vagy backend (pl. Jaeger, Grafana) kell hozzá, ami a 15 perces indítást és az időkeretet terheli; a v1.2-ben „erősen ajánlott”, nem kötelező.
3) **A kliens által küldött `X-Correlation-Id` átvétele** – előny: a frontend és a backend naplója összeköthető; hátrány: a kliens tetszőleges értéket írhatna a naplóba (log-injekció, hamis összerendelés), és jelenleg nincs kliensoldali naplógyűjtés, ami kihasználná.

## Consequences

- **Pozitív:**
  - Lapos JSON: a `correlationId`, az `ErrorCode`, a `StatusCode` és az `Elapsed` közvetlenül kereshető (pl. `jq 'select(.correlationId=="…")'`).
  - Egy hibajelentés `correlationId`-jéből a hozzá tartozó naplósorok megtalálhatók.
  - Az alkalmazáskód nem függ a Serilogtól (`ILogger<T>`), a csere csak a `Program.cs`-t és a csomaglistát érintené.
- **Negatív / kockázatok:**
  - Új függőség: `Serilog.AspNetCore` (Apache-2.0) és kb. 7 tranzitív Serilog-csomag; a sérülékenységeket a CI függőség-vizsgálata figyeli.
  - A váratlan kivétel üzenete és stack trace-e a naplóba kerül; ha egy külső könyvtár kivételüzenete személyes adatot tartalmazna, az a naplóba jutna. A saját kivételek (`AppException`) üzenete a biztonságos magyar `title`.
  - A `correlationId` folyamaton belüli azonosító; az Api és egy külön MCP-host közötti továbbítása az MCP-integráció ADR-jének része.
- **Figyelni kell a megvalósítás során:**
  - A naplószint konfigurációból jön (`Serilog:MinimumLevel`), alapértelmezetten `Information`; fejlesztésben a `http` launch profile állítja `Debug`-ra (`Serilog__MinimumLevel__Default`), mert az `appsettings.Development.json` nem verziózott.
  - Naplóüzenet csak strukturált sablonnal készül (`LoggerMessage` forrásgenerátor, a CA1848 analyzer kikényszeríti); kérés- és választörzs, cookie, jelszó és prompt nem naplózható.
  - A `correlationId`-t és a biztonsági fejléceket a válasz elküldésekor kell beállítani (`OnStarting`), mert a kivételkezelő a hibaválasz írása előtt törli a válaszfejléceket.

## Verification

- **Hogyan ellenőrizzük?**
  - Integrációs teszt: a hibaválasz `correlationId`-je egyezik az `X-Correlation-Id` fejléccel ([ErrorHandlingTests.cs](../../../tests/KamraApp.Integration.Tests/ErrorHandlingTests.cs)); ez a teszt találta meg, hogy a kivételkezelő törli a fejlécet.
  - Integrációs teszt: váratlan hibánál a válasz nem tartalmazza a kivétel üzenetét és stack trace-ét.
  - Naplóminta az [observability.md](../../05_security_ops/observability.md)-ben (a walking skeleton S6 szakaszában).
  - Tervezési validáció: [P-13](../../07_ai/prompt_log.md); az Antigravity (Gemini 3.1 Pro) reviewja a naplózási kérdésről, és az S2 szakasz Gemini-reviewja.
- **Evidence link:** a fenti tesztek és a naplóminta; a CI-futás linkje az S5 szakasz után kerül ide.
