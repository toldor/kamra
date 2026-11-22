# Megfigyelhetőség (Observability)

## Naplózás

- **Keretrendszer:** Serilog, JSON a konzolra (`RenderedCompactJsonFormatter`), döntés: [ADR-0011](../02_architecture/adr/0011-serilog-strukturalt-naplozas.md)
- **Szint:** `Serilog:MinimumLevel` konfigurációból; alapértelmezetten `Information`, fejlesztésben (a `http` launch profile) `Debug`
- **Mezők minden bejegyzésnél:**
  - `correlationId` – a szerver által generált kérésazonosító (`TraceIdentifier`); ugyanez jön az `X-Correlation-Id` válaszfejlécben és a hibaválasz `correlationId` mezőjében
  - `@t` (időbélyeg), `@l` (szint, `Information`-nél hiányzik), `@m` (üzenet), `SourceContext`
- **Hol olvasható:** a konténer stdoutján, Docker Compose-ban `docker compose logs api` (a migrátoré: `docker compose logs migrator`); egy hibához tartozó sorok: `docker compose logs api | grep <correlationId>`. Az élő példányon ugyanígy, SSH-n a VM-en a `~/kamra` mappában (a `COMPOSE_FILE` miatt a prod felülíró fájllal együtt); a TLS-proxy naplója: `docker compose logs caddy` (tanúsítványkérés és -megújítás, [deploy_runbook.md](deploy_runbook.md) „Élő példány (Azure)”). A kliens IP-címe nem kerül a naplóba.
- **Kérésenként egy összefoglaló sor** (`UseSerilogRequestLogging`): metódus, útvonal, státusz, időtartam

### Naplóminta

Egy kezelt üzleti hiba (409) és a kérés összefoglaló sora, ugyanazzal a `correlationId`-vel (integrációs tesztfutásból):

```json
{"@t":"2026-10-03T17:11:02.9338351Z","@m":"Request rejected with \"TEST_CONFLICT\"","@l":"Warning","ErrorCode":"TEST_CONFLICT","SourceContext":"KamraApp.Api.ErrorHandling.AppExceptionHandler","RequestPath":"/api/v1/test-errors/conflict","correlationId":"0HNP1E56RH33M"}
{"@t":"2026-10-03T17:11:02.9763150Z","@m":"HTTP \"GET\" \"/api/v1/test-errors/conflict\" responded 409 in 67.0626 ms","RequestMethod":"GET","RequestPath":"/api/v1/test-errors/conflict","StatusCode":409,"Elapsed":67.0626,"SourceContext":"Serilog.AspNetCore.RequestLoggingMiddleware","correlationId":"0HNP1E56RH33M"}
```
- **Soha nem kerülhet naplóba:** PII (személyes adat), promptok, a felhasználó mondata és a modell válasza, API kulcsok, jelszavak

### Naplózott események

| Esemény | Szint | Leírás |
|---|---|---|
| HTTP kérés/válasz | Information | Metódus, útvonal, státusz, időtartam |
| LLM hívás | Information | Próbálkozásonként egy esemény: modell, promptverzió, a próbálkozás sorszáma, kimenet (`ok`, `invalid`, `timeout`, `unavailable`, `refused`), időtartam (ms), bemenő és kimenő token; a mondatos bevitelé és a chaté is (`quick-entry.v3`, `chat.v2`; [GeminiHttp.cs](../../src/backend/KamraApp.Infrastructure/Llm/GeminiHttp.cs)); a mondat, a kérdés, a válasz, az eszközeredmény és a kulcs nélkül. Egy chat-kérdés modellhívásait a `correlationId` köti össze. A kimenet legfeljebb 2048 token; ha a modell nem fejezi be a választ (`finishReason` nem `STOP`, például elérte a korlátot), a kimenet `invalid`. |
| Validációs hiba | Warning | Hibakód, érintett mező |
| Váratlan kivétel | Error | Kivétel típusa, correlationId |
| Alkalmazás indulás | Information | Verzió, konfiguráció összegzés |

## Health Check

- **Végpont:** `GET /health` (nyilvános) – válasz: `200 Healthy` (szöveg), ha a folyamat fut és az adatbázis elérhető ([ErrorHandlingTests.cs](../../tests/KamraApp.Integration.Tests/ErrorHandlingTests.cs) `Health_returns_200`); az élő példányon: https://kamra-demo.austriaeast.cloudapp.azure.com/health (MT-05)
- **Ellenőrzések:**
  - PostgreSQL kapcsolat (`DatabaseHealthCheck`, `CanConnectAsync`): elérhetetlen adatbázisnál a válasz **503 `Unhealthy`** ([StartupTests.cs](../../tests/KamraApp.Integration.Tests/StartupTests.cs) `Health_returns_503_when_the_database_is_unreachable`)
  - Az LLM nem része a health checknek: a kiesése nem teszi `Unhealthy`-vé az alkalmazást (degradált üzem, S-1); a kiesés a naplóban látszik (`timeout`, `unavailable`).

## Metrikák

Nincs külön metrikagyűjtő rendszer (Prometheus, OpenTelemetry): egy gépen, kevés felhasználóval a kérésenkénti naplósor (útvonal, státusz, időtartam) és a `/health` elég a hibakereséshez. A három üzemeltetési metrika ezért a naplóból számolható; definíciójuk és lekérdezésük:

| Metrika | Definíció | Forrás a naplóban | Célérték | Lekérdezés (PowerShell, a repó gyökeréből) |
|---|---|---|---|---|
| **M1 – API-válaszidő** | A HTTP-kérések p95 időtartama, végpontonként vagy összesen | a kérés összefoglaló sora: `RequestPath`, `StatusCode`, `Elapsed` (ms) | az ajánlásnál p95 ≤ 300 ms (QA-5, [performance.md](../04_quality/performance.md)) | `$t = docker compose logs --no-log-prefix api \| % { try { $_ \| ConvertFrom-Json -EA Stop } catch {} } \| ? { $_.RequestPath -and $_.StatusCode } \| % { [double]$_.Elapsed } \| sort; $t[[math]::Ceiling($t.Count*0.95)-1]` |
| **M2 – Váratlan szerverhiba-arány** | Az 500-as válaszok aránya az összes kéréshez; a 502 és az 503 az LLM-kiesés, azt az M3 méri | a kérés összefoglaló sora: `StatusCode` | 0% (minden 500-as hiba incidens, [deploy_runbook.md](deploy_runbook.md) 1. incidens) | `$r = docker compose logs --no-log-prefix api \| % { try { $_ \| ConvertFrom-Json -EA Stop } catch {} } \| ? { $_.RequestPath -and $_.StatusCode }; ($r \| ? { $_.StatusCode -eq 500 }).Count / $r.Count` |
| **M3 – LLM-hívások kimenete és ideje** | Az LLM-próbálkozások aránya kimenetenként (`ok`, `invalid`, `timeout`, `unavailable`, `refused`) és a p95 időtartam | az LLM-hívás eseménye: `Outcome`, `DurationMs` | p95 ≤ 10 mp (QA-5); a nem `ok` arány 5% alatt | `docker compose logs --no-log-prefix api \| % { try { $_ \| ConvertFrom-Json -EA Stop } catch {} } \| ? { $_.Outcome } \| group Outcome \| select Name, Count` (a p95: [metrics.md](../01_product/metrics.md), G3) |

A naplót a konténer újralétrehozása törli, ezért a metrikák csak a konténer életidejére számolhatók (Ismert hiányosságok). Kipróbálva (2026-11-19) az LLM-csonkos stacken a `quick-entry` és a `chat` e2e-tesztek után: M1 413 kérés, p95 112 ms; M2 0 / 413 (a 4 db 502 és a 20 db 503 a tesztek szándékos kiesései); M3 14 `ok`, 8 `unavailable`, 2 `invalid`. Figyelem: a PowerShell a mezőnevekben nem különbözteti meg a kis- és nagybetűt, ezért a szűrő a kérés összefoglaló sorát a `RequestPath` és a `StatusCode` együttesével azonosítja. Egy gépes telepítésnél ez a hibakereséshez elég ([deploy_runbook.md](deploy_runbook.md), Incidensek). A termékmetrikák (North Star, guardrailek) SQL-lekérdezésekből jönnek ([metrics.sql](../01_product/metrics.sql), futtatás és definíciók: [metrics.md](../01_product/metrics.md)); a G3 LLM-válaszideje az LLM-hívás naplóeseményéből.

## Ismert hiányosságok

- Váratlan kivételnél a kivétel üzenete és stack trace-e a naplóba kerül (a válaszba nem); ha egy külső könyvtár üzenete személyes adatot tartalmazna, az a naplóba jutna ([ADR-0011](../02_architecture/adr/0011-serilog-strukturalt-naplozas.md) Consequences).
- Riasztás nincs: a hibát a felhasználó vagy a `/health` ellenőrzése jelzi.
