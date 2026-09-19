# Megfigyelhetőség (Observability)

## Naplózás

- **Keretrendszer:** Serilog, JSON formátum
- **Szint:** `Information` (prod), `Debug` (dev)
- **Kötelező mezők minden log bejegyzésnél:**
  - `correlationId` – kérésenkénti egyedi azonosító
  - `timestamp`, `level`, `message`, `sourceContext`
- **Soha nem kerülhet naplóba:** PII (személyes adat), promptok, API kulcsok, jelszavak

### Naplózott események (tervezett)

| Esemény | Szint | Leírás |
|---|---|---|
| HTTP kérés/válasz | Information | Metódus, útvonal, státusz, időtartam |
| LLM hívás | Information | Eszköznév, token-szám (tartalom nélkül) |
| Validációs hiba | Warning | Hibakód, érintett mező |
| Váratlan kivétel | Error | Kivétel típusa, correlationId |
| Alkalmazás indulás | Information | Verzió, konfiguráció összegzés |

## Health Check

- **Végpont:** `GET /health`
- **Ellenőrzések (tervezett):**
  - PostgreSQL kapcsolat
  - LLM API elérhetőség (opcionális, degraded állapotban is működjön)

## Metrikák

Jelenleg nem implementált. Jövőbeli lehetőség: Prometheus + Grafana.

## Ismert hiányosságok

- Naplózás még nincs bekonfigurálva.
- Health check végpont még nem létezik.
