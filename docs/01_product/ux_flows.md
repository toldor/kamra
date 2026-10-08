# UX flow-k

A három fő felhasználói útvonal, a hibahelyzetek, az üres állapotok és az akadálymentességi minimum. A flow-k Tomi személyére épülnek ([vision.md](vision.md)), és együtt adják a demó fő flow-ját: bevitel → ajánlás → főzés → bevásárlólista. A szabályok forrása: [scope_contract.md](scope_contract.md).

## 1. Első indulás és bevásárlás rögzítése (US-1, US-2)

- **Cél:** Tomi a bevásárlás után egy mondattal rögzíti, amit vett.
- **Előfeltétel:** regisztrált, bejelentkezett felhasználó.
- **Lépések:**
  1. Bejelentkezés után a kezdőképernyő a készlet. Üres készletnél ezt látja: *„Még üres a kamrád. Írd le egy mondatban, mit vettél – például: »vettem 2 liter tejet, 10 tojást és fél kiló paradicsomot, a tej pénteken jár le«.”* Alatta a mondatos beviteli mező, és egy másodlagos gomb: „Inkább tételenként rögzítem”.
  2. Tomi beírja a mondatot és elküldi. A rendszer tételjavaslatokat mutat szerkeszthető sorokban (hozzávaló, mennyiség, egység, lejárat; a becsült lejárat jelölve).
  3. Ha egy hozzávalóból már van készlet, a sor alatt tájékoztató jelenik meg: *„Már van otthon: tej 0,5 l (okt. 2-án jár le).”*
  4. Tomi szükség esetén javít, majd a „Jóváhagyás” gombbal menti. Új készlettételek jönnek létre.
- **Sikerkritérium:** a jóváhagyott tételek a készletben vannak; a hamarosan lejárók a kezdőképernyő tetején látszanak.
- **Edge case – dupla jóváhagyás:** a gomb a kérés idejére letiltódik, és a jóváhagyás egy tételjavaslatra csak egyszer fut le (idempotens), így dupla kattintás vagy újraküldés nem hoz létre duplikált készletet.
- **Edge case – másik fülön módosított tétel:** ha Tomi egy készlettételt szerkeszt, amelyet közben egy másik fülön módosított, a mentés nem fut le; a képernyő a friss adatokat mutatja a változás kiemelésével, és Tomi újra dönthet.

## 2. „Mit főzzek?” – főzés és a készlet frissítése (US-3, US-4)

- **Cél:** Tomi este gyorsan kiválaszt egy receptet abból, ami otthon van, elsősorban a hamarosan lejáró alapanyagokból.
- **Előfeltétel:** van készlet.
- **Lépések:**
  1. Tomi megnyitja a „Mit főzzek?” oldalt. Elöl az elkészíthető receptek állnak, mindegyik egy mondatos indoklással (*„2 hamarosan lejáró hozzávalót használ fel: tejföl, paradicsom”*), alattuk a majdnem elkészíthetők a hiányzó mennyiségekkel (*„tejföl: 50 g hiányzik (150 g van, 200 g kell)”*).
  2. Kiválaszt egy receptet, és a „Megfőztem” gombbal megadja az adagszámot (Betti négyfős családnál nagyobb adagot ad meg).
  3. A megerősítő képernyő hozzávalónként mutatja a szükséges és a felhasznált mennyiséget, és azt, hogy melyik készlettételből vonódik le (a legkorábban lejárótól). A felhasznált mennyiség itt módosítható; ha valamiből kevesebb van, a hiány külön látszik, és egy kattintással a bevásárlólistára tehető.
  4. A megerősítés után a készlet frissül. Ha közben valami elfogyott, egy összegző sor jelzi: *„Elfogyott: tej, tojás. Felvettem őket a bevásárlójavaslatok közé.”*, „Megnézem” linkkel.
- **Sikerkritérium:** a felhasznált mennyiségek levonódtak, a főzés rögzült, és Tomi tudja, mi fogyott el.
- **Edge case – közben változott készlet:** ha a megerősítéskor valamiből már kevesebb van, mint a megadott felhasznált mennyiség, a főzés nem mentődik; a képernyő a friss készlettel újraszámol, kiemeli az eltérést (*„tejföl: már csak 150 g van, 200 g helyett”*), és újra megerősítést kér.
- **Edge case – dupla kattintás vagy újraküldés:** a főzés megerősítése egyszer fut le; az ismételt kérés nem von le újra, hanem az eredeti főzés összegzését mutatja.

## 3. Bevásárlójavaslattól a listáig (US-5)

- **Cél:** a bevásárlólista abból álljon, ami tényleg kell, és Tomi döntsön róla.
- **Előfeltétel:** egy hozzávaló elfogyott, vagy a minimumszintje alá esett.
- **Lépések:**
  1. A főzés vagy a kézi levonás megerősítése után Tomi az összegző sorban látja, mi került a javaslatok közé, és a „Megnézem” linkkel a bevásárlólistára lép.
  2. A lista fölött a *„Javaslatok (2)”* blokk soronként „Hozzáadom” és „Nem kell” gombbal.
  3. A „Hozzáadom” a listára teszi a tételt; a „Nem kell” eltünteti, és addig nem jön vissza, amíg Tomi újra nem vesz belőle.
  4. A „Majdnem elkészíthető” receptnél a „Bevásárlólistára” gomb a hiányzó mennyiségeket közvetlenül a listára teszi.
  5. A boltban Tomi kipipálja a megvett tételeket.
- **Sikerkritérium:** a lista csak a Tomi által elfogadott vagy kézzel felvett tételeket tartalmazza, és egy hozzávaló nem szerepel rajta kétszer.

## Üres állapotok

| Hol | Helyzet | Amit a felhasználó lát | Következő lépés |
|---|---|---|---|
| Készlet | Üres készlet | *„Még üres a kamrád. Írd le egy mondatban, mit vettél – …”* | Mondatos bevitel vagy „Inkább tételenként rögzítem” |
| „Mit főzzek?” | Üres készlet | *„Még nincs mit ajánlanom, mert üres a kamrád. Rögzítsd, mit vettél, és megmutatom, mit főzhetsz belőle.”* | „Készlet rögzítése” |
| „Mit főzzek?” | Csak majdnem elkészíthető recept van | *„Most egyik receptedhez sincs meg minden hozzávaló.”* – alatta a majdnem elkészíthetők | „Bevásárlólistára” |
| „Mit főzzek?” | Semmi nem illeszkedik | *„A kamrád alapján most nem találtam receptet. Vegyél fel saját receptet, vagy rögzíts további hozzávalókat.”* | „Új recept”, „Bevásárlás rögzítése” |

## Hibakezelés

A hibaüzenetek magyarok, nem tartalmaznak technikai kifejezést vagy HTTP-kódot, és megmondják, mi a következő lépés.

| # | Helyzet | Mikrocopy | Helyreállítás |
|---|---|---|---|
| H1 | Az AI nem elérhető vagy időtúllépés | *„Most nem sikerült értelmeznem a mondatot. A szövegedet megtartottam, rögzítheted tételenként is.”* | A beírt szöveg megmarad; „Rögzítés tételenként” gomb |
| H2 | Hiányzó vagy nem átváltható egység | *„Mennyi van a csomagban? Add meg grammban vagy darabban.”* | A jóváhagyás addig nem engedett, amíg meg nem adja |
| H3 | Hálózati hiba mentéskor | *„Nem sikerült menteni, mert megszakadt a kapcsolat. Semmi nem veszett el – próbáld újra.”* | A kitöltött adatok megmaradnak; „Újra” gomb |
| H4 | Hibás bejelentkezési adatok | *„Hibás e-mail-cím vagy jelszó.”* (nem árulja el, melyik volt rossz) | Az e-mail mező kitöltve marad |
| H5 | Zárolt fiók (5 sikertelen bejelentkezés után) | *„Túl sok sikertelen próbálkozás. Várj 5 percet, és próbáld újra.”* | Az e-mail mező kitöltve marad; 5 perc múlva újra próbálható |

**További edge case-ek:**

- **Lejárt munkamenet:** *„Biztonsági okból kiléptettünk. Jelentkezz be újra, és folytathatod.”* Bejelentkezés után ugyanarra az oldalra kerül vissza.
- **Közben törölt tétel:** *„Ez a tétel már nincs a kamrádban. Frissítettem a listát.”*

## Akadálymentesség (minimum)

- Kontraszt legalább 4,5:1 normál szövegnél (WCAG 2.1 AA).
- Minden beviteli mezőnek látható címkéje van.
- A három fő flow csak billentyűzettel is végigvihető, és a fókusz mindig látható.
- A hibaüzenetek képernyőolvasóval is felolvasódnak (`aria-live`), és a hibás mezőhöz kapcsolódnak.
- Az ikonos gomboknak szöveges neve van.
- Mobilon legalább 16 px-es alap betűméret, és legalább 44×44 px-es érintési felület.

**Ellenőrzés:** a Chrome Lighthouse akadálymentességi auditja a készlet, a „Mit főzzek?” és a bevásárlólista oldalon (legalább 90 pont), valamint egy kézi billentyűzetes bejárás. Opcionális bővítés: automatizált axe-core ellenőrzés az e2e tesztekben (új függőség, jóváhagyás kell).

## Bizonyítékok

| Flow | Screenshot / GIF | E2E teszt |
|---|---|---|
| 1. Első indulás és bevásárlás rögzítése | `docs/assets/` alá, az implementációval | Playwright, az 1. és 2. lépcsőben |
| 2. „Mit főzzek?” – főzés | `docs/assets/` alá, az implementációval | Playwright, az 1. lépcsőben |
| 3. Bevásárlójavaslattól a listáig | `docs/assets/` alá, az implementációval | Playwright, az 1. lépcsőben |
| Hibakezelés (H1, H3) | Screenshot a hibaüzenettel és a helyreállítással | Playwright: AI-kiesés mockkal, hálózati hiba |

## Ismert hiányosságok

- A három fő flow felülete még nincs implementálva, ezért screenshot és e2e teszt sincs hozzájuk; a flow-k a scope_contract story-jain alapuló tervek. A walking skeletonban a bejelentkezés, a regisztráció, az üres készlet és a H4 hibaüzenet már él, Playwright e2e teszttel ([auth.spec.ts](../../tests/e2e/auth.spec.ts)).
