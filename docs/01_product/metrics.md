# Metrikák

A [vision.md](vision.md) siker definíciójának mérési terve. Minden metrika a rendszer saját domain-adataiból, lekérdezéssel számolható: nincs külső analitikai eszköz, és adat nem hagyja el a rendszert.

## Metrikák

| Típus | Név | Definíció | Hogyan mérném | Cél |
|---|---|---|---|---|
| North Star | **Megmentett főzések** | Aktív háztartásonként hetente megfőzött receptek átlagos száma, amelyek legalább egy hamarosan lejáró tételt felhasználnak | Adott hét főzés-eseményei közül azok, amelyeknél a felhasznált tételek között van olyan, amelynek lejárata a főzés napján, az azt követő napon vagy az azutáni napon van (a már lejárt nem számít). Ezek száma osztva az adott heti aktív háztartások számával. | ≥ 1 / aktív háztartás / hét |
| Guardrail | **G1 – Pazarolt arány** | A lejárt tételek eredeti mennyiségének átlagosan hány százaléka ment pazarlásba | A 2 hetes ablakban lejárt tételekre tételenként: *pazarolt mennyiség / eredeti mennyiség*, majd ezek átlaga. Pazarolt = a *kidobtam* okú csökkenések összege, plusz az ablak végén lejártan még készleten lévő mennyiség. A lejárat után *elfogyott*-ként rögzített mennyiség fogyasztásnak számít. | ≤ 25% (2 hetes gördülő ablak) |
| Guardrail | **G2 – Korai megtartás** | A regisztrált felhasználók hány százaléka végzett legalább 3 készletbevitelt a regisztrációt követő 14 napban | Csak azok a felhasználók számítanak, akiknek a regisztrációja óta már eltelt 14 nap. A sikeresen mentett készletbevitelek száma (form-mentés vagy jóváhagyott mondatos bevitel, tételszámtól függetlenül; az el nem fogadott mondat nem számít) a regisztráció utáni 14 napban. | ≥ 50% |
| Guardrail | **G3 – Bevitel ideje** | A szöveg beküldésétől a jóváhagyott mentésig eltelt idő, benne az LLM válaszidejével | Teljes idő: a tételjavaslat beküldési és jóváhagyási időbélyegének különbsége; az elvetett javaslatok kimaradnak. LLM válaszidő: a backend strukturált logjából, az LLM-hívás eseményének `DurationMs` mezőjéből ([observability.md](../05_security_ops/observability.md)). | Teljes: p50 ≤ 40 mp, p90 ≤ 90 mp · LLM: p95 ≤ 10 mp |

## Szükséges adatok

A metrikák három, a domainben amúgy is tárolt adatra épülnek:

1. **Készletmozgás-napló:** minden készletváltozás mennyiséggel, időponttal és okkal.
2. **Főzés-események:** időpont, recept és a felhasznált tételek.
3. **Tételjavaslat időbélyegei:** beküldés és jóváhagyás (vagy elvetés). Megvalósítva: `ItemProposals.SubmittedAt` és `ResolvedAt` ([data_model.md](../03_design/data_model.md)); a G3-lekérdezés a [metrics.sql](metrics.sql)-ben.

**A készletcsökkenés okai** (a felhasználó választja; a főzés utáni levonás automatikusan *elfogyott*):

| Ok | Jelentés | Hatás a metrikákra |
|---|---|---|
| *elfogyott* | megette, felhasználta | fogyasztás |
| *kidobtam* | megromlott, kidobta | pazarlás (G1), akkor is, ha lejárat előtt történt |
| *hibás rögzítés* | javítás | kimarad a metrikákból |

## Mérési terv

1. **Szintetikus ellenőrzés (kötelező, kész – 2026-11-19):** metrikánként egy kitalált adatsor ismert, kézzel kiszámolt értékekkel (a North Starnál és a G1-nél egy-egy szimulált háztartás 2 hete, a G2-nél négy felhasználó). A metrika-lekérdezéseket automatizált tesztek ellenőrzik ezen az adaton (lásd lent). Ez a számítás helyességét bizonyítja, a termék hatását nem.
2. **Tesztfelhasználói mérés (terv):** 5–8 tesztfelhasználó, köztük legalább egy egyedül élő és egy családi háztartás, 2 hét valódi használattal. Utána a négy metrikát egyszer kiszámoljuk, és az eredmény ebbe a dokumentumba kerül.

## Lekérdezések (CAP-12)

A négy metrika egy SQL-fájlban van: [metrics.sql](metrics.sql). Csak olvas, és csak összesített számot ad vissza (személyes adatot nem); a futtatáshoz adatbázis-hozzáférés kell, amely az üzemeltetőé ([privacy_licensing.md](../05_security_ops/privacy_licensing.md)). Futtatás a Docker Compose stacken (PowerShell, a repó gyökeréből):

```powershell
Get-Content docs/01_product/metrics.sql -Raw | docker compose exec -T db sh -c 'psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "$POSTGRES_DB"'
```

Az értékelés napja (D) alapértelmezetten a mai budapesti nap; más nap: `docker compose exec -T -e PGOPTIONS="-c kamra.as_of=2026-11-16" db …`. A D napjának 00:00-ja (Budapest) előtti adat számít; az ablak a D előtti 14 naptári nap. Minden időpont budapesti naptári napra számolódik, ahogy az alkalmazás „mai” napja.

**A definíciók pontosítása a lekérdezéshez:**

- **Hét:** hétfőtől vasárnapig; a North Star a D előtti két teljes hetet adja, hetenként külön sorban. A hamarosan lejáró határ (a főzés napja + 2 nap) a Domain `Expiry.SoonExpiringDays` állandója; ha a kettő eltér, a teszt elbukik.
- **Aktív háztartás:** azon a héten volt készletbevitele (bármely `Added` mozgás) vagy főzése ([CONTEXT.md](../../CONTEXT.md)); a kézi csökkentés nem tesz aktívvá.
- **G1, lejárt tétel:** a tétel *aktuális* lejárata esik az ablakba (a lejárat szerkesztése nem naplózódik, ezért a mozgáskori lejárat egy kijavított dátumot is lejártnak mutatna). **Eredeti mennyiség:** a bevitel és a *hibás rögzítés* javításainak összege; a teljesen visszavont (0) tétel kimarad. **Pazarolt:** a *kidobtam* okú csökkenések és a D előtt még meglévő mennyiség.
- **G2, a 14 nap** a budapesti falióra szerint számít (a nyári időszámítás vége nem tolja el egy órával). **Készletbevitel:** egy form-mentés, vagy egy jóváhagyott mondat (a mondat tételei a beküldés időpontja szerint összevonva, az első jóváhagyás ideje számít); a regisztráció ideje a háztartás létrehozása. Csak a D előtt legalább 14 napja regisztrált háztartás számít.
- **G3, teljes idő:** mondatonként a beküldéstől az utolsó jóváhagyott tétel mentéséig, azokra a mondatokra, amelyek utolsó jóváhagyása az ablakba esik; az elvetett tételek kimaradnak. Percentilis: a PostgreSQL `percentile_cont` (lineáris interpoláció).
- **G3, LLM válaszidő:** az alkalmazás naplójából (próbálkozásonként egy `DurationMs`, [observability.md](../05_security_ops/observability.md)), PowerShell-lel:

```powershell
$d = docker compose logs --no-log-prefix api | ForEach-Object { try { $_ | ConvertFrom-Json -ErrorAction Stop } catch {} } | Where-Object { $null -ne $_.DurationMs } | ForEach-Object { [double]$_.DurationMs } | Sort-Object
if ($d) { "n=$($d.Count) p95=$($d[[math]::Ceiling($d.Count * 0.95) - 1]) ms" }
```

**Ellenőrzés a szintetikus adaton** ([MetricsQueryTests.cs](../../tests/KamraApp.Integration.Tests/MetricsQueryTests.cs), metrikánként külön, frissen migrált adatbázisban, D = 2026-11-16):

| Metrika | Adatsor | Kézzel számolt | A lekérdezés eredménye |
|---|---|---|---|
| North Star | 7 főzés (köztük a +2 napos határ, egy nappal késő, lejárt, két hamarosan lejáró tétel egy főzésben, éjfél utáni főzés, amely UTC szerint még előző napi); egy csak bevivő és egy csak kézzel csökkentő háztartás | 1. hét: 1 / 2 = 0,50; 2. hét: 2 / 2 = 1,00 | 0,50 és 1,00 |
| G1 | 11 tétel: elfogyott, részben kidobott, az ablak végén még meglévő, javított eredetű, teljesen visszavont, lejárat után megevett, az ablak első napján lejáró és kidobott, az utolsó napján az utolsó percekben megevett, ablakon kívüli (2) és utólag javított lejáratú | (0 + 25 + 50 + 100 + 0 + 100 + 0) / 7 = 39,3% | 39,3% (7 tétel) |
| G2 | 6 háztartás: nyári időben regisztrált, 3 bevitellel (benne egy 3 tételes mondat; a 3. a 14. nap délelőttjén, amely 14 × 24 óra után van); 2 bevitel és egy elvetett mondat; a 3. bevitel pontosan a 14. nap végén; pontosan 14 napos, bevitel nélkül; egy perccel fiatalabb; túl friss | 1 / 4 = 25,0% | 25,0% |
| G3 | 4 jóváhagyott mondat (20 mp az ablak első percében, 30 – két tétel 10 és 30 mp-nél –, 40, 100 mp az utolsó percekben), egy elvetett, kettő az ablakon kívül | p50 = 35 mp, p90 = 82 mp | 35,0 és 82,0 mp (4 mondat) |

Üres adatbázison a darabszámok 0-k, az arányok üresek (nincs nullával osztás). A lekérdezések 36 szándékos hibájából (a határok szűkítve és tágítva, az időzóna, a nyári időszámítás, a mondatonkénti számolás, a szűrők) és a Domain határállandójának elrontásából mind a 37-et elkapják a tesztek ([V-100](../07_ai/verification_log.md)).

## Ismert korlátok

- **Nincs kiinduló adat:** a célértékek indokolt feltételezések. A tesztfelhasználói mérés után felül kell vizsgálni őket.
- **Kis, motivált minta:** a tesztfelhasználói mérés ismerősökkel történik, ezért a G2 túlteljesítése kevés információt hordoz. A sablon szerint a célértékek azt írják le, *„mit mérnék egy valódi terméknél”*.
- **Felhasználó által rögzített adat:** a rögzítetlen fogyasztás vagy kidobás torzít. A G1 ezt a pazarlás irányába kerekíti (a lejártan készleten maradt mennyiség pazarlásnak számít).
- **Kis mintán a percentilisek bizonytalanok:** néhány tucat bevitelnél a p90 és a p95 lényegében a leglassabb egy-két eset.
- **A G1 és a lejárat késői szerkesztése:** a G1 a tétel aktuális lejáratával számol, így ha valaki egy már lejárt tétel dátumát az ablak után javítja, a múltbeli G1 is változik. A G1 nem látja azt a kidobott tételt, amelynek lejárata az ablakon kívül esik.
- **A mondat azonosítása:** a mondatot nem tároljuk, ezért a tételeit a háztartás és a beküldés időpontja (mikroszekundumos pontossággal) köti össze a G2-ben és a G3-ban; két, pontosan ugyanabban a pillanatban beküldött mondat egynek számítana.
- **A G3 LLM-része** kézi parancs, nincs rá automatikus teszt, és csak addig ad adatot, amíg a konténer naplója megvan (a konténer újralétrehozása törli).
- **Továbbfejlesztési lehetőségek:** a G1 relatív határral (a háztartás saját korábbi átlagához mérve), ami hosszabb adatsort igényel; a G3 közvetlen összevetése a kézi form idejével, ami frontend-eseménykövetést igényelne.
