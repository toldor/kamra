# Megfigyelhetőség (Observability)

## Naplózás

- **Keretrendszer:** Serilog, JSON a konzolra (`RenderedCompactJsonFormatter`), döntés: [ADR-0011](../02_architecture/adr/0011-serilog-strukturalt-naplozas.md)
- **Szint:** `Serilog:MinimumLevel` konfigurációból; alapértelmezetten `Information`, fejlesztésben (a `http` launch profile) `Debug`
- **Mezők minden bejegyzésnél:**
  - `correlationId` – a szerver által generált kérésazonosító (`TraceIdentifier`); ugyanez jön az `X-Correlation-Id` válaszfejlécben és a hibaválasz `correlationId` mezőjében
  - `@t` (időbélyeg), `@l` (szint, `Information`-nél hiányzik), `@m` (üzenet), `SourceContext`
- **Hol olvasható:** a konténer stdoutján, Docker Compose-ban `docker compose logs api` (a migrátoré: `docker compose logs migrator`); egy hibához tartozó sorok: `docker compose logs api | grep <correlationId>`
- **Kérésenként egy összefoglaló sor** (`UseSerilogRequestLogging`): metódus, útvonal, státusz, időtartam

### Naplóminta

Egy kezelt üzleti hiba (409) és a kérés összefoglaló sora, ugyanazzal a `correlationId`-vel (integrációs tesztfutásból):

```json
{"@t":"2026-10-03T17:11:02.9338351Z","@m":"Request rejected with \"TEST_CONFLICT\"","@l":"Warning","ErrorCode":"TEST_CONFLICT","SourceContext":"KamraApp.Api.ErrorHandling.AppExceptionHandler","RequestPath":"/api/v1/test-errors/conflict","correlationId":"0HNP1E56RH33M"}
{"@t":"2026-10-03T17:11:02.9763150Z","@m":"HTTP \"GET\" \"/api/v1/test-errors/conflict\" responded 409 in 67.0626 ms","RequestMethod":"GET","RequestPath":"/api/v1/test-errors/conflict","StatusCode":409,"Elapsed":67.0626,"SourceContext":"Serilog.AspNetCore.RequestLoggingMiddleware","correlationId":"0HNP1E56RH33M"}
```
- **Soha nem kerülhet naplóba:** PII (személyes adat), promptok, API kulcsok, jelszavak

### Naplózott események

| Esemény | Szint | Leírás |
|---|---|---|
| HTTP kérés/válasz | Information | Metódus, útvonal, státusz, időtartam |
| LLM hívás | Information | Eszköznév, token-szám (tartalom nélkül) |
| Validációs hiba | Warning | Hibakód, érintett mező |
| Váratlan kivétel | Error | Kivétel típusa, correlationId |
| Alkalmazás indulás | Information | Verzió, konfiguráció összegzés |

## Health Check

- **Végpont:** `GET /health` (nyilvános) – válasz: `200 Healthy` (szöveg), ha a folyamat fut és az adatbázis elérhető ([ErrorHandlingTests.cs](../../tests/KamraApp.Integration.Tests/ErrorHandlingTests.cs) `Health_returns_200`)
- **Ellenőrzések:**
  - PostgreSQL kapcsolat (`DatabaseHealthCheck`, `CanConnectAsync`): elérhetetlen adatbázisnál a válasz **503 `Unhealthy`** ([StartupTests.cs](../../tests/KamraApp.Integration.Tests/StartupTests.cs) `Health_returns_503_when_the_database_is_unreachable`)
  - LLM API elérhetőség – opcionális, a nem AI-alapú funkciók degraded állapotban is működjenek

## Metrikák

Jelenleg nem implementált. Jövőbeli lehetőség: Prometheus + Grafana.

## Ismert hiányosságok

- Váratlan kivételnél a kivétel üzenete és stack trace-e a naplóba kerül (a válaszba nem); ha egy külső könyvtár üzenete személyes adatot tartalmazna, az a naplóba jutna ([ADR-0011](../02_architecture/adr/0011-serilog-strukturalt-naplozas.md) Consequences).
- Metrikák és riasztás még nincsenek.
