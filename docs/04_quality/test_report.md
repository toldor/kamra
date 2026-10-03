# Tesztjelentés

Frissítsd minden CI futás után, amelyik releváns változást hoz.
Cél: 30+ automatizált teszt (≥18 unit, ≥6 integrációs, ≥6 e2e/contract), ebből ≥5 negatív eset.

## Környezet

- OS: Windows 11 (fejlesztői gép)
- Runtime/SDK: .NET SDK 10.0.201 (`global.json`), xUnit v3 Microsoft Testing Platform módban
- DB: – (még nincs adatbázis-teszt)
- Teszt adat: –

## Teszt suite-ek és futtatás

- Unit: `dotnet test` (a repo gyökeréből; `tests/KamraApp.Unit.Tests`)

## Összesítés

| Kategória | Cél | Jelenlegi | Átmegy |
|---|---|---|---|
| Unit | ≥ 18 | 4 | 4 |
| Integrációs | ≥ 6 | 0 | – |
| E2E | ≥ 6 | 0 | – |
| Negatív esetek | ≥ 5 | 0 | – |
| **Összesen** | **≥ 30** | **4** | **4** |

## Legutolsó futás eredménye

- Dátum: 2026-10-03
- Eredmény: PASS (4/4), helyi futás
- CI link: –

## Tesztelt modulok

| Modul | Típus | Leírás | Állapot |
|---|---|---|---|
| Rétegszabályok ([LayerRulesTests.cs](../../tests/KamraApp.Unit.Tests/LayerRulesTests.cs)) | Unit | A Domain és az Application assembly-hivatkozásai és projektfájl-hivatkozásai az [ADR-0004](../02_architecture/adr/0004-clean-architecture-retegek.md) szerint | ✅ 4/4 |

## Lefedetlen területek

- Minden üzleti modul – az implementáció még nem kezdődött el.

## Ismert hiányosságok

- A rétegszabály-tesztek jelenleg üres rétegeken futnak: az assembly-alapú tesztek csak a kódban ténylegesen használt hivatkozást látják, ezért a valódi bizonyító erejük a Domain és az Application kódjával együtt nő. A deklarált, de nem használt hivatkozást a projektfájl-alapú tesztek már most is kiszűrik (kézzel igazolva: egy ideiglenes `FrameworkReference` mindkét projektfájl-tesztet elbuktatta).
- Még nincs CI: a tesztek egyelőre csak helyben futnak; a CI a walking skeleton S5 szakaszában készül.
- Még nincs lefedettségmérés: a lefedettség-gyűjtő eszköz a CI-val együtt kerül be.
- Élő AI API-hívás nem futhat CI-ban (`[Trait("Category","LiveAI")]` jelöléssel különítendő el).

## Flaky / instabil tesztek

- Nincs ismert flaky teszt.
