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
- **Edge case – másik fülön módosított tétel:** ha Tomi egy készlettételt szerkeszt, amelyet közben egy másik fülön módosított, a mentés nem fut le; a képernyő a friss adatokat mutatja a változás kiemelésével, és Tomi újra dönthet. *Megvalósítva:* *„Közben változott – mennyiség: 20 dkg → 15 dkg”* ([PantryPage.test.tsx](../../src/frontend/src/PantryPage.test.tsx)).

## 2. „Mit főzzek?” – főzés és a készlet frissítése (US-3, US-4)

- **Cél:** Tomi este gyorsan kiválaszt egy receptet abból, ami otthon van, elsősorban a hamarosan lejáró alapanyagokból.
- **Előfeltétel:** van készlet.
- **Lépések:**
  1. Tomi megnyitja a „Mit főzzek?” oldalt. Elöl az elkészíthető receptek állnak, mindegyik egy mondatos indoklással (*„2 hamarosan lejáró hozzávalót használ fel: tejföl, paradicsom”*), alattuk a majdnem elkészíthetők a hiányzó mennyiségekkel (*„tejföl: 50 g hiányzik (150 g van, 200 g kell)”*). *Megvalósítva* ([recommendations-360.png](../assets/recommendations-360.png)); a lejárt tételt felhasználó receptnél: *„Lejárt tételt is felhasznál: …”*.
  2. Kiválaszt egy receptet, és a „Megfőztem” gombbal megadja az adagszámot (Betti négyfős családnál nagyobb adagot ad meg). *Megvalósítva.*
  3. A megerősítő képernyő hozzávalónként mutatja a szükséges és a felhasznált mennyiséget, és azt, hogy melyik készlettételből vonódik le (a legkorábban lejárótól). A felhasznált mennyiség itt módosítható; ha valamiből kevesebb van, a hiány külön látszik, és egy kattintással a bevásárlólistára tehető. *Megvalósítva* ([cooking-360.png](../assets/cooking-360.png)), a hiány listára tételével együtt ([shopping.spec.ts](../../tests/e2e/shopping.spec.ts)).
  4. A megerősítés után a készlet frissül. Ha közben valami elfogyott, egy összegző sor jelzi: *„Elfogyott: tej, tojás. Felvettem a bevásárlójavaslatok közé: tej, tojás.”*, „Megnézem” linkkel. *Megvalósítva* ([cooking-done-360.png](../assets/cooking-done-360.png)); a bevásárlójavaslat-mondat és a „Megnézem” link a US-5-tel (okt. 23.), addig: *„Elfogyott: tej, tojás.”*
- **Sikerkritérium:** a felhasznált mennyiségek levonódtak, a főzés rögzült, és Tomi tudja, mi fogyott el.
- **Edge case – közben változott készlet:** ha a megerősítéskor valamiből már kevesebb van, mint a megadott felhasznált mennyiség, a főzés nem mentődik; a képernyő a friss készlettel újraszámol, kiemeli az eltérést (*„tejföl: már csak 150 g van, 200 g helyett”*), és újra megerősítést kér. Most: üzenet és „Újraszámolás” gomb; a kiemelés okt. 23-ig.
- **Edge case – dupla kattintás vagy újraküldés:** a főzés megerősítése egyszer fut le; az ismételt kérés nem von le újra, hanem az eredeti főzés összegzését mutatja. *Megvalósítva* ([cooking.spec.ts](../../tests/e2e/cooking.spec.ts)).

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
- **Megvalósítva** ([shopping-360.png](../assets/shopping-360.png), [shopping.spec.ts](../../tests/e2e/shopping.spec.ts), P13). A javaslat sora megmondja, miért és mennyit javasol (*„Elfogyott (okt. 18., főzés: Lecsó).”*, *„A minimumszint (500 ml) alá csökkent: 400 ml van.”*), és a gombok alatt azt is, mi lesz a döntés következménye. A lista fölött: *„Amíg egy hozzávaló pipa nélkül a listán van, nem javaslom újra.”* A minimumszinteket ugyanezen az oldalon lehet megadni és törölni.

## 4. Saját recept és archiválás (US-3)

- **Cél:** Tomi felveszi a saját receptjét a saját hozzávalóival, és kiveszi az ajánlásból azt, amit nem főz.
- **Előfeltétel:** bejelentkezett felhasználó.
- **Lépések:**
  1. A „Receptek” oldalon az induló és a saját receptek állnak, a saját „Saját recept” jelöléssel; mindegyiknél „Megfőztem” és „Archiválás”.
  2. Az „Új recept” űrlapon megadja a nevet, az adagszámot, a hozzávalókat mennyiséggel és egységgel, és ha akarja, az elkészítést. Ha egy hozzávaló nincs a listán, a mező alatt „Új hozzávaló: *név*”: megadja, mivel méri és melyik kategória, és a hozzávaló csak az ő háztartásában jelenik meg. Ugyanez a készletűrlapon is működik.
  3. Ha közben frissít vagy újra be kell jelentkeznie, a félbehagyott recept visszatöltődik: *„Visszatöltöttem a félbehagyott receptet.”*
  4. Mentés után: *„Elmentettem a receptet: Zabtejes kakaó.”* A recept a készletből ugyanúgy ajánlható és főzhető, mint az induló.
  5. Archiválás a böngésző megerősítő ablakával: *„Archiválod ezt a receptet: Lecsó? Nem ajánlom többé, és nem főzhető. A korábbi főzéseid megmaradnak. Ezt nem lehet visszavonni.”* Az induló recept archiválása csak az ő háztartására hat.
- **Sikerkritérium:** a saját recept megjelenik az ajánlásban, ha a készlet kiadja; az archivált nem.
- **Megvalósítva** ([recipes-360.png](../assets/recipes-360.png), [recipes-form-360.png](../assets/recipes-form-360.png), [recipes.spec.ts](../../tests/e2e/recipes.spec.ts), P17, P18).

## Üres állapotok

| Hol | Helyzet | Amit a felhasználó lát | Következő lépés |
|---|---|---|---|
| Készlet | Üres készlet | *„Még üres a kamrád. Írd le egy mondatban, mit vettél – …”* | Mondatos bevitel vagy „Inkább tételenként rögzítem” |
| „Mit főzzek?” | Üres készlet | *„Még nincs mit ajánlanom, mert üres a kamrád. Rögzítsd, mit vettél, és megmutatom, mit főzhetsz belőle.”* | „Készlet rögzítése” |
| „Mit főzzek?” | Csak majdnem elkészíthető recept van | *„Most egyik receptedhez sincs meg minden hozzávaló.”* – alatta a majdnem elkészíthetők | „Bevásárlólistára” |
| „Mit főzzek?” | Semmi nem illeszkedik | *„A kamrád alapján most nem találtam receptet. Vegyél fel saját receptet, vagy rögzíts további hozzávalókat.”* | „Új recept”, „Bevásárlás rögzítése” |
| Receptek | Minden recept archiválva | *„Nincs recepted. Vegyél fel egyet az „Új recept” gombbal.”* | „Új recept” |
| Bevásárlólista | Üres lista | *„A listád üres. Vegyél fel tételt alább.”* – ha van javaslat: *„A listád üres. Fogadj el egy javaslatot, vagy vegyél fel tételt alább.”* | Kézi felvétel vagy „Hozzáadom” |

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
- **Közben törölt tétel:** *„Ez a tétel már nincs a kamrádban. Frissítettem a listát.”* *Megvalósítva:* a szerkesztőablak bezárul, a lista frissül ([PantryPage.test.tsx](../../src/frontend/src/PantryPage.test.tsx)).
- **Közben eldöntött vagy eltűnt javaslat, törölt listatétel:** *„Erről a javaslatról már másképp döntöttél, például egy másik ablakban. Frissítettem a listát.”* (illetve *„Ez a javaslat már nem érhető el. Frissítettem a listát.”*, *„Ez a tétel már nincs a bevásárlólistán. Frissítettem a listát.”*). *Megvalósítva:* a lista magától frissül ([ShoppingListPage.test.tsx](../../src/frontend/src/ShoppingListPage.test.tsx)).
- **Közben fogyott készlet a főzésnél:** a sor a friss mennyiséget mutatja, a hibás mező alatt: *„Már csak 150 g van, 200 g helyett.”*; az „Újraszámolás” megtartja a beírt értékeket. *Megvalósítva* ([CookingPage.test.tsx](../../src/frontend/src/CookingPage.test.tsx)).
- **Közben archivált recept a főzésnél** (például egy másik ablakban): *„Ezt a receptet archiváltad, ezért már nem főzhető.”* és a „Vissza a receptekhez” link, az előnézetnél és a megerősítésnél is; a korábban elküldött főzés ismétlése ugyanazt az eredményt adja. *Megvalósítva* ([CookingPage.test.tsx](../../src/frontend/src/CookingPage.test.tsx), [RecipesApiTests.cs](../../tests/KamraApp.Integration.Tests/RecipesApiTests.cs) P18a).
- **Foglalt hozzávalónév:** *„Ilyen nevű hozzávaló már van. Válaszd ki a listából.”* A kis- és nagybetű és a szóközök nem számítanak. *Megvalósítva.*

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
| 1. Első indulás és bevásárlás rögzítése | Kézi ág: [pantry-360.png](../assets/pantry-360.png) (360 px); a mondatos ág a US-2-vel | Kézi ág: [pantry.spec.ts](../../tests/e2e/pantry.spec.ts) (rögzítés becsült lejárattal, csökkentés okkal, törlés, fókusz-visszaadás); a mondatos ág a 2. lépcsőben |
| 2. „Mit főzzek?” – főzés | Ajánlás: [recommendations-360.png](../assets/recommendations-360.png) (P7); főzés: [cooking-360.png](../assets/cooking-360.png), [cooking-done-360.png](../assets/cooking-done-360.png) (P2) | [cooking.spec.ts](../../tests/e2e/cooking.spec.ts) (a fő flow a P2-vel, dupla kattintás); [RecommendationsPage.test.tsx](../../src/frontend/src/RecommendationsPage.test.tsx), [CookingPage.test.tsx](../../src/frontend/src/CookingPage.test.tsx) |
| 3. Bevásárlójavaslattól a listáig | [shopping-360.png](../assets/shopping-360.png) (P13) | [shopping.spec.ts](../../tests/e2e/shopping.spec.ts) (P13 a teljes úton: ugyanaz a hiány az ajánlásban, az előnézeten és a listán); [ShoppingListPage.test.tsx](../../src/frontend/src/ShoppingListPage.test.tsx) |
| 4. Saját recept és archiválás | [recipes-360.png](../assets/recipes-360.png), [recipes-form-360.png](../assets/recipes-form-360.png) | [recipes.spec.ts](../../tests/e2e/recipes.spec.ts) (saját recept saját hozzávalóval, a piszkozat túléli az újratöltést, ajánlásban megjelenik, archiválás után eltűnik); [RecipesPage.test.tsx](../../src/frontend/src/RecipesPage.test.tsx) |
| Hibakezelés (H1, H3) | Screenshot a hibaüzenettel és a helyreállítással | Playwright: AI-kiesés mockkal, hálózati hiba |

## Ismert hiányosságok

- A 2., a 3. és a 4. flow kész. Lejárt munkamenet után a bejelentkezés ugyanarra az oldalra visz vissza (az útvonal az URL-ben marad), navigáláskor a fókusz az új oldal címére kerül. Az 1. flow kézi ága kész ([pantry.spec.ts](../../tests/e2e/pantry.spec.ts)), a mondatos bevitel a US-2-vel jön; addig az üres készlet szövege: *„Még üres a kamrád. Rögzítsd az első tételt az »Új tétel« űrlappal.”* A bejelentkezés, a regisztráció és a H4 hibaüzenet a walking skeleton óta él ([auth.spec.ts](../../tests/e2e/auth.spec.ts)).
- Az „Új hozzávaló” csak a készlet- és a receptűrlapon érhető el, a bevásárlólistán és a minimumszintnél nem. A saját hozzávaló és a saját recept nem szerkeszthető, az archiválás nem vonható vissza (data_model, Ismert hiányosságok).
- A „Bevásárlólistára” és a „Hiány a bevásárlólistára” gomb a sikeres felvétel után eltűnik az adott képernyőről. Ha az oldal újratöltése után újra megnyomod, a mennyiségek összeadódnak, a lista egy sor marad. Ha a főzés előnézetén a felvétel után módosítod a mennyiségeket, a különbséget kézzel veheted fel a listára.
- A szerkesztőablak bezárásakor a fókusz visszakerül a megnyitó „Módosítás” gombra ([V-28](../07_ai/verification_log.md)); törléskor a gomb megszűnik, ilyenkor a fókusz a lap elejére kerül.
