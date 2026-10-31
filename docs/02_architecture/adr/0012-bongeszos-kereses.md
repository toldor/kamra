# 0012 - Böngészőben szűrő kereső a Receptek és a Bevásárlólista oldalon

Dátum: 2026-10-31
Státusz: Accepted

## Context

- A fejlesztő az UAT előtti módosítási listájában ([P-23](../../07_ai/prompt_log.md)) keresőt kért a Receptek és a Bevásárlólista oldalra. A vállalás eddig csak a készletnél írt elő keresést (US-1), az a szerveren fut (`GET /pantry-items?search=`).
- Kényszer: másnap UAT, a backend és az API-szerződés nem változhat.
- A két oldal minden tételt egyszerre tölt be, lapozás nincs (`GET /recipes`, `GET /shopping-list`); 20 induló recept van, plusz a háztartás saját receptjei.

## Decision

- A két új kereső a már betöltött listát a böngészőben szűri (`matchesSearch`, `src/frontend/src/format.ts`), a készletkeresés szabályával: kis- és nagybetű és fölös szóköz nem számít, az ékezet igen.

## Alternatives

1) `search` paraméter a `GET /recipes` és a `GET /shopping-list` végponton, mint a készletnél – előny: egy szabály egy helyen, nagy listán is működik; hátrány: API- és OpenAPI-változás, új integrációs tesztek, nem fér bele az UAT előtti napba.
2) Közös kereső a fejlécben, találati oldallal – előny: egy helyen mindenre keres; hátrány: új végpont vagy három kérés, új oldal, átfed a chat asszisztenssel (US-6).
3) Nincs kereső, döntés az UAT után – előny: nincs változás; hátrány: a fejlesztő kifejezetten kérte.

## Consequences

- **Pozitív:** nincs backend- és API-változás; gépelés közben, kérés nélkül szűr.
- **Negatív / kockázatok:** a szabály két helyen él (`Ingredient.Normalize` a backendben és a frontend `normalize`); egy komment és egy negatív teszt (az ékezet) köti össze, de eltérhetnek. Sok száz receptnél a teljes lista betöltése lassulhat.
- **Figyelni kell:** ha a receptek vagy a lista lapozást kap, a keresés a szerverre kerül (1. alternatíva).

## Verification

- Vitest: [RecipesPage.test.tsx](../../../src/frontend/src/RecipesPage.test.tsx), [ShoppingListPage.test.tsx](../../../src/frontend/src/ShoppingListPage.test.tsx) (8 új teszt, ebből 4 negatív); mutációk ([V-62](../../07_ai/verification_log.md)); e2e 18/18.
- Evidence link: [test_report.md](../../04_quality/test_report.md) (2026-10-31, `v0.3.1`); CI: [run 41231148667](https://github.com/toldor/kamra/actions/runs/41231148667).
