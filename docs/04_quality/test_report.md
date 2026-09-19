# Tesztjelentés

Frissítsd minden CI futás után, amelyik releváns változást hoz.
Cél: 30+ automatizált teszt (≥18 unit, ≥6 integrációs, ≥6 e2e/contract), ebből ≥5 negatív eset.

## Összesítés

| Kategória | Cél | Jelenlegi | Átmegy |
|---|---|---|---|
| Unit | ≥ 18 | 0 | – |
| Integrációs | ≥ 6 | 0 | – |
| E2E | ≥ 6 | 0 | – |
| Negatív esetek | ≥ 5 | 0 | – |
| **Összesen** | **≥ 30** | **0** | **–** |

## Tesztelt modulok

| Modul | Típus | Leírás | Állapot |
|---|---|---|---|
| – | – | Még nincs teszt | – |

## Lefedetlen területek

- Minden modul – implementáció még nem kezdődött el.

## Ismert hiányosságok

- Tesztek az implementációval párhuzamosan készülnek.
- Élő AI API-hívás nem futhat CI-ban (`[Trait("Category","LiveAI")]` jelöléssel különítendő el).
