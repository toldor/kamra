# Minőségi attribútumok

A rendszer nemfunkcionális elvárásai mérhető célértékkel és tervezett bizonyítékkal. Alapja a [scope_contract.md](../01_product/scope_contract.md) és a [metrics.md](../01_product/metrics.md); a célértékek igazolása a megvalósítás és a minőségbiztosítás során készül el, és ide kerül hivatkozással.
Az akadálymentesség a [ux_flows.md](../01_product/ux_flows.md), a naplózás és a health check az [observability.md](../05_security_ops/observability.md) része, ezért itt nem szerepelnek.

## 1. Attribútumok

| # | Attribútum | Mérhető elvárás | Tervezett bizonyíték | Forrás |
|---|---|---|---|---|
| QA-1 | **Biztonság – adatelkülönítés** | Bejelentkezés nélkül egyetlen adatvégpont sem érhető el (401). Az [api.md](../03_design/api.md) és a [mcp_tools.md](../03_design/mcp_tools.md) szerinti, háztartáshoz kötött végpontok és toolok 100%-a más háztartás erőforrására 404-et ad, a nem létező azonosítóval megkülönböztethetetlenül. | Paraméterezett integrációs teszt (S-2) | Scope contract, keresztmetszeti követelmény; US-6 |
| QA-2 | **Helyesség – determinizmus** | Azonos készletre és receptkészletre azonos eredmény: ajánlás és rangsor, FEFO-levonás, adagskálázás és kerekítés, becsült lejárat, relatív dátumok, bevásárlójavaslat-szabályok. | Unit tesztek a Domain és az Application rétegben | US-1–US-5 |
| QA-3 | **Hibatűrés – LLM-kiesés** | Próbálkozásonként 15 mp időkorlát; időtúllépésnél vagy kiesésnél egy azonnali újrapróbálás; utána magyar hibaüzenet, semmi nem mentődik, a nem AI-alapú funkciók működnek tovább. Sémahibás válasznál nincs újrapróbálás. | Mockolt kiesés-teszt (S-1) | US-2, US-6; Scope contract, Külső API |
| QA-4 | **Adatintegritás** | A készlet soha nem negatív; egy tételjavaslat jóváhagyása egyszer fut le; párhuzamos levonásnál nem vész el levonás; minden mennyiségváltozás a készletmozgás-naplóba kerül. | Integrációs tesztek valódi Postgresszel (Testcontainers); a konkurenciakezelés módja külön ADR-ben | US-1, US-2, US-4 |
| QA-5 | **Teljesítmény** | LLM válaszidő p95 ≤ 10 mp; ajánlás-végpont p95 ≤ 300 ms (S-3). | LLM-PoC és a backend strukturált logja; terheléses mérés | Metrics G3 (LLM); az ajánlás célértéke ebben a dokumentumban rögzített új elvárás |
| QA-6 | **Modularitás és tesztelhetőség** | A rétegszabályok ([AGENTS.md](../../AGENTS.md) 3. pont) sértetlenek; minden LLM-hívás az Application rétegben definiált interfész mögött van, és tesztben mockolható; a Domain és az Application réteg sorlefedettsége ≥ 80%. | Projekthivatkozások + reflexiós rétegszabály-teszt; lefedettségi riport a CI-ban | Scope contract, DoD |
| QA-7 | **Telepíthetőség** | Tiszta gépen, a README alapján, `docker compose up` paranccsal 15 percen belül elindul; hiányzó vagy érvénytelen konfiguráció esetén az alkalmazás induláskor hibával leáll. | Friss klónból végzett, időmért indítási próba; integrációs teszt: hiányzó kötelező konfigurációval az alkalmazás nem indul el | Scope contract, DoD; v1.2 futtathatósági kapufeltétel |

## 2. Scenariók

### S-1 – LLM-kiesés mondatos bevitel közben (QA-3)

| Elem | Leírás |
|---|---|
| Forrás | Külső LLM-szolgáltató |
| Stimulus | 15 mp-en belül nem válaszol, hálózati vagy 5xx hibát ad, vagy a válasza nem felel meg a sémának |
| Környezet | Degradált üzem: az LLM elérhetetlen, a többi komponens működik |
| Artefakt | A tételjavaslat-készítés use case-e és az LLM-adapter |
| Válasz | Próbálkozásonként 15 mp időkorlát; időtúllépésnél vagy kiesésnél egy azonnali újrapróbálás, sémahibánál nincs. Ezután magyar nyelvű hibaüzenet, a beírt szöveg megmarad, a kézi form egy kattintással elérhető, és semmi nem mentődik. |
| Mérőszám | A hibaüzenet a beküldéstől számítva legkésőbb 31 mp alatt (2 × 15 mp próbálkozás + 1 mp feldolgozás) megjelenik; 0 készlettétel mentődik; a kiesés alatt a készletlista- és az ajánláskérések 100%-a sikeres. |
| Igazolás | Integrációs teszt mockolt LLM-adapterrel és hamis órával, valamint e2e teszt a megmaradó szövegre és a kézi form elérésére; a 2. lépcsőben (US-2) készül. |

### S-2 – Más háztartás adatának lekérése (QA-1)

| Elem | Leírás |
|---|---|
| Forrás | Bejelentkezett felhasználó, aki egy másik háztartás erőforrásának azonosítóját ismeri vagy kitalálja |
| Stimulus | Olvasó vagy módosító kérés egy idegen készlettételre, saját receptre, saját hozzávalóra, bevásárlólista-tételre vagy tételjavaslatra |
| Környezet | Normál üzem |
| Artefakt | Az API és az Application réteg háztartás-szerinti szűrése; a chat MCP-toolai |
| Válasz | 404, ugyanazzal a hibaobjektummal és hibakóddal, mint egy nem létező azonosítóra; adat nem változik |
| Mérőszám | Az [api.md](../03_design/api.md) és a [mcp_tools.md](../03_design/mcp_tools.md) szerinti, háztartáshoz kötött végpontok és toolok 100%-a 404-et ad; a válasz (státusz és hibakód) azonos a nem létező azonosítóra adott válasszal; 0 adatsor változik. |
| Igazolás | Paraméterezett integrációs teszt valódi Postgresszel (Testcontainers), az 1. lépcsőtől; az MCP-toolok a 3. lépcsőben (US-6) kerülnek bele. |

### S-3 – Ajánlás lekérése párhuzamos terhelés alatt (QA-5)

| Elem | Leírás |
|---|---|
| Forrás | 10 párhuzamosan bejelentkezett felhasználó, mindegyik a saját háztartásával |
| Stimulus | A „Mit főzzek?” ajánlás lekérése, bemelegítés után összesen 500 kérés |
| Környezet | Normál üzem, lokális Docker Compose környezet; seed-adat: kb. 40 recept és háztartásonként kb. 50 készlettétel |
| Artefakt | Az ajánlás-végpont: Application use case és a Postgres-lekérdezés |
| Válasz | A helyesen rangsorolt elkészíthető és majdnem elkészíthető receptek listája indoklással |
| Mérőszám | p95 válaszidő ≤ 300 ms; 0% hibás válasz |
| Igazolás | Terheléses mérés a minőségbiztosítási szakaszban. A mérőeszközt a mérés előtt választjuk ki; az eredmény a teljesítménymérési dokumentumba és a [verification_log.md](../07_ai/verification_log.md)-ba kerül. |

## Ismert korlátok

- **Mérési környezet:** minden mérés lokális Docker Compose környezetben készül, nem éles infrastruktúrán; az eredmény a hardverrel együtt értelmezhető.
- **Rendelkezésre állás:** nincs vállalt érték, mert a deploy platformról még nincs döntés.
- **Adatméret:** a teljesítmény-célérték a seed-adatra vonatkozik; nagy háztartásra (több száz recept vagy készlettétel) nincs vállalt érték.
