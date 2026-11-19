# Teljesítménymérés

A [quality_attributes.md](../02_architecture/quality_attributes.md) QA-5 célja és S-3 forgatókönyve: az ajánlás-végpont (`GET /api/v1/recommendations`) p95 válaszideje legfeljebb 300 ms, 0% hibás válasszal, 10 párhuzamos felhasználónál. Az LLM válaszidejét (QA-5 másik fele) a [test_report.md](test_report.md) „LLM-mérés” szakasza méri.

## Módszer

- **Eszköz:** [recommendations.mjs](../../tests/perf/recommendations.mjs) – Node 24, csak beépített modulokkal (új függőség nélkül). Minden virtuális felhasználó új háztartást regisztrál a valódi API-n (antiforgery-tokennel, sütivel), és `ITEMS` darab készlettételt rögzít véletlen, de rögzített sorrendű (determinisztikus) hozzávalóval, mennyiséggel és 0–29 napos lejárattal. Utána a felhasználók párhuzamosan kérik az ajánlást: 50 bemelegítő, majd 500 mért kérés. A mért idő a kliens oldali teljes válaszidő (a válasz törzsének beolvasásáig).
- **Környezet:** a Docker Compose stack egy külön projektnévvel (`kamra-perf`, 8091-es port), emelt bejelentkezési limittel; Windows 11, 16 logikai mag, Docker Desktop; a mérőszkript ugyanazon a gépen fut. Az adatbázis a migrációval betöltött 157 hozzávalót és 20 induló receptet tartalmazza.
- **Futtatás:**
  ```powershell
  $env:API_PORT='8091'; $env:POSTGRES_PORT='5434'; $env:RateLimiting__Auth__PermitLimit='1000'
  docker compose -p kamra-perf up -d --build
  node tests/perf/recommendations.mjs http://localhost:8091
  # változatok: $env:ITEMS='300'; vagy $env:USERS='50'; $env:REQUESTS='1000'
  docker compose -p kamra-perf down -v
  ```

## Eredmények (2026-11-19, `feature/metrics` @ `95eaf06`)

| Futás | Felhasználók | Tétel / háztartás | Mért kérés | Hiba | p50 | p95 | p99 | max |
|---|---|---|---|---|---|---|---|---|
| Alapmérés 1. | 10 | 50 | 500 | 0 | 13,7 ms | 22,2 ms | 28,9 ms | 33,9 ms |
| Alapmérés 2. | 10 | 50 | 500 | 0 | 13,0 ms | 21,1 ms | 27,4 ms | 31,6 ms |
| Alapmérés 3. | 10 | 50 | 500 | 0 | 13,1 ms | 18,0 ms | 21,8 ms | 24,7 ms |
| Nagy készlet | 10 | 300 | 500 | 0 | 15,6 ms | 24,7 ms | 31,3 ms | 43,4 ms |
| Sok felhasználó | 50 | 50 | 1000 | 0 | 59,6 ms | 109,8 ms | 154,5 ms | 167,9 ms |

**A QA-5 ajánlásra vonatkozó célja teljesül:** az S-3 forgatókönyvben a p95 18–22 ms (a cél 300 ms), hibás válasz nincs.

## Szűk keresztmetszet

- **A készlet mérete nem az:** hatszoros készletnél (300 tétel) a p95 csak kb. 3 ms-mal nő. Az illesztés és a rangsor memóriában fut, a háztartás teljes készletén és az összes receptjelölten; ez ekkora adatnál elhanyagolható.
- **A párhuzamosság az:** 50 egyidejű felhasználónál a p95 kb. ötszörösére nő (110 ms), miközben a konténerek CPU- és memóriaterhelése alacsony maradt. A válaszidő tehát a kérések sorban állásából jön, nem a számításból. Kérésenként több adatbázis-kör fut: a süti biztonsági bélyegének ellenőrzése ([ADR-0006](../02_architecture/adr/0006-cookie-auth-identity.md), D-3), a készlet és a receptjelöltek beolvasása. Ez a legvalószínűbb korlát; a körök számát és az adatbázis-kapcsolatkészlet méretét kellene külön mérni.
- **Javítás:** nem készült, mert a cél a vállalt terhelés ötszörösénél is teljesül. Ha kellene, az első lépés a receptjelöltek gyorsítótárazása lenne (a közös receptek ritkán változnak), utána a kapcsolatkészlet hangolása – mindkettőt méréssel kellene igazolni.

## Ismert korlátok

- A mérés egy gépen, a mérőszkripttel közös erőforráson futott, nem éles infrastruktúrán; az értékek a géppel együtt értelmezhetők.
- A háztartások mesterséges, véletlen készlete a valódi használat eloszlását nem követi; a receptkészlet kicsi (20 recept). Nagy receptkészletre (több száz recept) nincs mérés.
- Csak az ajánlás-végpontot mértük; a többi végpontra és a felület betöltési idejére nincs vállalt érték.
- Ellenőrzés: [V-103](../07_ai/verification_log.md).
