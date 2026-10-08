# Metrikák

A [vision.md](vision.md) siker definíciójának mérési terve. Minden metrika a rendszer saját domain-adataiból, lekérdezéssel számolható: nincs külső analitikai eszköz, és adat nem hagyja el a rendszert.

## Metrikák

| Típus | Név | Definíció | Hogyan mérném | Cél |
|---|---|---|---|---|
| North Star | **Megmentett főzések** | Aktív háztartásonként hetente megfőzött receptek átlagos száma, amelyek legalább egy hamarosan lejáró tételt felhasználnak | Adott hét főzés-eseményei közül azok, amelyeknél a felhasznált tételek között van olyan, amelynek lejárata a főzés napján, az azt követő napon vagy az azutáni napon van (a már lejárt nem számít). Ezek száma osztva az adott heti aktív háztartások számával. | ≥ 1 / aktív háztartás / hét |
| Guardrail | **G1 – Pazarolt arány** | A lejárt tételek eredeti mennyiségének átlagosan hány százaléka ment pazarlásba | A 2 hetes ablakban lejárt tételekre tételenként: *pazarolt mennyiség / eredeti mennyiség*, majd ezek átlaga. Pazarolt = a *kidobtam* okú csökkenések összege, plusz az ablak végén lejártan még készleten lévő mennyiség. A lejárat után *elfogyott*-ként rögzített mennyiség fogyasztásnak számít. | ≤ 25% (2 hetes gördülő ablak) |
| Guardrail | **G2 – Korai megtartás** | A regisztrált felhasználók hány százaléka végzett legalább 3 készletbevitelt a regisztrációt követő 14 napban | Csak azok a felhasználók számítanak, akiknek a regisztrációja óta már eltelt 14 nap. A sikeresen mentett készletbevitelek száma (form-mentés vagy jóváhagyott mondatos bevitel, tételszámtól függetlenül; az el nem fogadott mondat nem számít) a regisztráció utáni 14 napban. | ≥ 50% |
| Guardrail | **G3 – Bevitel ideje** | A szöveg beküldésétől a jóváhagyott mentésig eltelt idő, benne az LLM válaszidejével | Teljes idő: a tételjavaslat beküldési és jóváhagyási időbélyegének különbsége; az elvetett javaslatok kimaradnak. LLM válaszidő: a backend strukturált logjából ([observability.md](../05_security_ops/observability.md)). | Teljes: p50 ≤ 40 mp, p90 ≤ 90 mp · LLM: p95 ≤ 10 mp |

## Szükséges adatok

A metrikák három, a domainben amúgy is tárolt adatra épülnek:

1. **Készletmozgás-napló:** minden készletváltozás mennyiséggel, időponttal és okkal.
2. **Főzés-események:** időpont, recept és a felhasznált tételek.
3. **Tételjavaslat időbélyegei:** beküldés és jóváhagyás (vagy elvetés).

**A készletcsökkenés okai** (a felhasználó választja; a főzés utáni levonás automatikusan *elfogyott*):

| Ok | Jelentés | Hatás a metrikákra |
|---|---|---|
| *elfogyott* | megette, felhasználta | fogyasztás |
| *kidobtam* | megromlott, kidobta | pazarlás (G1), akkor is, ha lejárat előtt történt |
| *hibás rögzítés* | javítás | kimarad a metrikákból |

## Mérési terv

1. **Szintetikus ellenőrzés (kötelező):** seed-adat egy szimulált háztartás 2 hetéről, ismert, kézzel kiszámolt metrikaértékekkel. A metrika-lekérdezéseket automatizált tesztek ellenőrzik ezen az adaton. Ez a számítás helyességét bizonyítja, a termék hatását nem.
2. **Tesztfelhasználói mérés (terv):** 5–8 tesztfelhasználó, köztük legalább egy egyedül élő és egy családi háztartás, 2 hét valódi használattal. Utána a négy metrikát egyszer kiszámoljuk, és az eredmény ebbe a dokumentumba kerül.

## Ismert korlátok

- **Nincs kiinduló adat:** a célértékek indokolt feltételezések. A tesztfelhasználói mérés után felül kell vizsgálni őket.
- **Kis, motivált minta:** a tesztfelhasználói mérés ismerősökkel történik, ezért a G2 túlteljesítése kevés információt hordoz. A sablon szerint a célértékek azt írják le, *„mit mérnék egy valódi terméknél”*.
- **Felhasználó által rögzített adat:** a rögzítetlen fogyasztás vagy kidobás torzít. A G1 ezt a pazarlás irányába kerekíti (a lejártan készleten maradt mennyiség pazarlásnak számít).
- **Kis mintán a percentilisek bizonytalanok:** néhány tucat bevitelnél a p90 és a p95 lényegében a leglassabb egy-két eset.
- **Továbbfejlesztési lehetőségek:** a G1 relatív határral (a háztartás saját korábbi átlagához mérve), ami hosszabb adatsort igényel; a G3 közvetlen összevetése a kézi form idejével, ami frontend-eseménykövetést igényelne.
