# 0013 - Saját vizuális rendszer: tokenek CSS-változókban, saját tárhelyű betűk

Dátum: 2026-10-31
Státusz: Accepted

## Context

- A fejlesztő a „Kamra stílusirányok” design-vásznon a C2 (mobil, keskeny) és a C3 (asztali) táblákat választotta: olívazöld fejsáv, rózsa kiemelés, Zilla Slab címek, IBM Plex Sans szöveg, név – pontozott vezetővonal – mennyiség sorok. Eddig az app rendszerbetűs, 28rem széles egyetlen oszlop volt.
- Kényszerek: új npm-függőség csak jóváhagyással (AGENTS.md 2. pont); az [ux_flows](../../01_product/ux_flows.md) akadálymentességi minimuma (4,5:1 kontraszt, 44 px célok, látható fókusz); a magyar ő és ű; adatvédelem: az app nem küldhet kérést külső betűszolgáltatónak.

## Decision

- A színek, a betűk, a térközök és a lekerekítés CSS custom propertyként a `src/frontend/src/index.css` `:root` blokkjában vannak; a Zilla Slab és az IBM Plex Sans woff2-ben (latin + latin-ext, OFL 1.1) a `src/frontend/public/fonts` alatt; asztalon két oszlop, 760 px alatt egymás alatt, a sorok container queryvel igazodnak.
- **Módosítás, 2026-11-17 (kinézet és kezelhetőség, `v0.8.0`):** (1) **Sötét paletta** ugyanazokkal a tokenekkel `:root[data-theme='dark']` alatt; a téma (Rendszer szerint / Világos / Sötét) a böngészőben marad meg, a „Rendszer szerint”-et a `theme.ts` oldja fel a `<html data-theme>`-re, így egy sötét blokk van; a `<meta name="color-scheme" content="light dark">` a szkript előtt is sötét alapot ad sötét rendszernél. A `tokens.test.ts` mindkét palettán ugyanazt a 17 + 5 párt ellenőrzi, és hogy a sötét blokkban minden token megvan. Nyomtatáskor mindkét témában sötét szöveg kerül fehér papírra. (2) **`--c-estimated`** (a becsült lejárat csíkja) díszítő token, 3:1-es pár nélkül: a jelentést a „(becsült)” szöveg hordozza (WCAG 1.4.11 csak a megértéshez szükséges grafikára kér 3:1-et); a teszt rögzíti. (3) **Ikonok** saját inline SVG-ként (`Icon.tsx`), függőség nélkül, a szöveg színével, `aria-hidden`. (4) **Elrendezés:** ragadós fejsáv; 1024 px alatt a menü alsó sáv a teljes nevekkel (WCAG 2.5.3); a görgetési margó a fejsáv és a sáv magassága (WCAG 2.4.11). (5) A Kérdezz oldal szándékosan lekerekített (`--radius-chat`, `--radius-bubble`), hogy kicsit elváljon a többi oldaltól; az új elemek (összegző gombok, eszközcímkék, avatar) a meglévő színtokeneket használják, új szín nélkül.

## Alternatives

1) CSS-keretrendszer (például Tailwind) vagy komponenskönyvtár – előny: kész rácsrendszer és komponensek; hátrány: új függőség és build-lépés, a C2/C3 egyedi kinézetét úgyis felül kellene írni.
2) Google Fonts CDN – előny: nincs fájl a repóban, automatikus frissítés; hátrány: minden oldalbetöltés harmadik félhez megy (a felhasználó IP-címe), és belső hálózaton, internet nélkül nem töltődik be.
3) Maradnak a rendszerbetűk – előny: nincs letöltés; hátrány: elvész a tervezett karakter, és az ő/ű megjelenése platformfüggő.

## Consequences

- **Pozitív:** a kinézet egy helyen változik; a kontrasztot teszt őrzi; futás közben nincs külső kérés.
- **Negatív / kockázatok:** kb. 121 KB betű (négy fájl) (`font-display: swap`, a böngésző csak a szükséges `unicode-range` tartományt tölti le); a betűket és a `unicode-range` értékeket kézzel kell frissíteni. Az olívazöld fejsávon a kék fókuszkeret csak kb. 2:1-es, ezért ott a fejsáv világos szövegszíne a fókuszkeret.
- **Figyelni kell:** új szín csak tokenként kerülhet be, a kontrasztpárjával a `tokens.test.ts`-ben.

## Verification

- [tokens.test.ts](../../../src/frontend/src/tokens.test.ts): 17 szöveg–háttér pár legalább 4,5:1, 5 nem-szöveges pár legalább 3:1, a fejsáv fókuszszínével ([V-63](../../07_ai/verification_log.md)).
- E2E [pantry.spec.ts](../../../tests/e2e/pantry.spec.ts) „loads the fonts for ő and ű…”: a latin-ext betűk betöltődnek, asztalon két oszlop, 360 px-en egymás alatt; két mutációval ellenőrizve ([V-64](../../07_ai/verification_log.md)).
- E2E „the Profilom button keeps its contrast on the band when hovered” (`v0.8.0` előtt a kijelentkezés gombja): a fejsáv hover-állapota (a `/code-review` lelete, regressziós teszt).
- Képernyőképek 1280 és 390 px-en, vízszintes görgetés nélkül; evidence: [test_report.md](../../04_quality/test_report.md).
- **`v0.8.0`:** `tokens.test.ts` – mindkét paletta; e2e [frame.spec.ts](../../../tests/e2e/frame.spec.ts) (ragadós fejsáv, alsó sáv 360 és 900 px-en, görgetési margó, `color-scheme`), [theme.spec.ts](../../../tests/e2e/theme.spec.ts) (sötét rendszer, felülírás, újratöltés), [lists.spec.ts](../../../tests/e2e/lists.spec.ts) (nyomtatás mindkét témában, csökkentett mozgás); mutációval ellenőrizve ([V-90](../../07_ai/verification_log.md), [V-96](../../07_ai/verification_log.md)); zöld CI: [run 44160245178](https://github.com/toldor/kamra/actions/runs/44160245178) (#131, `ded39b8`).
