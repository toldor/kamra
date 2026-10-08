# Vision – Kamra (Smart Pantry & Recipe Manager)

## 1. Probléma

Egy magyar háztartásban fejenként évente 60,7 kg élelmiszer kerül a szemétbe, ebből 20,8 kg elkerülhető lett volna. A kidobás leggyakoribb okai, hogy **megfeledkezünk az otthon lévő élelmiszerről, és túl sokat vásárolunk** ([NÉBIH Maradék nélkül, 2025](https://portal.nebih.gov.hu/-/mennyi-elelmiszerhulladek-keletkezik-a-haztartasokban-a-nebih-felmeresebol-kiderul)). Az uniós háztartási átlag 69 kg/fő ([Eurostat, 2023](https://ec.europa.eu/eurostat/web/products-eurostat-news/w/ddn-20251016-2)). Mindkét okra ugyanaz a válasz: tudni kellene, mi van otthon, és mi jár le hamarosan. Készletnyilvántartó alkalmazások léteznek, de a gyakorlatban elavulnak, mert minden tétel kézi felvitele több munka, mint amennyi hasznot hoz. Az elavult készletadatra pedig nem lehet sem bevásárlást, sem főzést tervezni. A tudás önmagában sem elég: sokan akkor is nehezen döntik el, mit főzzenek abból, ami otthon van, ezért inkább új alapanyagot vesznek vagy rendelnek, a meglévő pedig tovább áll. **Miért most:** a nagy nyelvi modellek mára megbízhatóan alakítanak át egy hétköznapi mondatot („vettem 2 liter tejet, 10 tojást, a tej pénteken jár le”) strukturált készletadattá. Ez először teszi olcsóvá a naprakész nyilvántartást, feltéve, hogy az AI csak javasol, és a végleges adatot a felhasználó hagyja jóvá.

## 2. Célfelhasználók

### Elsődleges persona – Tomi, 28, egyedül él

Irodai munkát végez, hetente egyszer-kétszer vásárol, hétköznap este főz magának.

- **Célok:** ne dobjon ki ételt; munka után gyorsan eldöntse, mit főzzön abból, ami otthon van; ne költsön feleslegesen.
- **Frusztrációk:**
  - A kiszerelés nagyobb, mint amit egyedül elfogyaszt. A fél kiló paradicsom, a megkezdett tejföl vagy a fél káposzta a hűtő hátuljában felejtődik, és csak akkor kerül elő, amikor már megromlott.
  - Este nincs energiája kitalálni, mit főzzön. Ilyenkor inkább rendel, pedig otthon lenne alapanyag.
  - Kipróbált már kamra-appot, de néhány hét után abbahagyta a vezetését, mert a tételek egyenkénti felvitele több időbe telt, mint amennyi hasznot hozott.
- **A termék számára:** a készletet egyedül vezeti, így a nyilvántartás pontossága csak rajta múlik. Egy fiók = egy háztartás.

### Másodlagos persona – Betti, 42, négyfős család háztartásvezetője

Hetente egyszer nagybevásárol, és ő tervezi a heti menüt.

- **Célok:** a nagyobb készletből kevesebb menjen kárba; a bevásárlólista abból készüljön, ami ténylegesen hiányzik.
- **Frusztrációk:** nehéz egyszerre fejben tartani, mi van otthon, és azt, hogy milyen étel lesz mindenkinek jó. Emiatt nehéz olyan receptet találni, ami a készletből elkészíthető, és a családnak is megfelel.
- **A termék számára:** a termék a készletből elkészíthető receptekre szűkíti a választékot, előnyben a hamarosan lejáró alapanyagokkal. Hogy ezek közül mi jó a családnak, azt Betti dönti el.

## 3. Értékajánlat

**A felhasználó mostani helyzetéhez képest** (fejben tartás, cetli, egy abbahagyott app):

- **Tomi** egy mondattal rögzíti a bevásárlást („vettem 2 liter tejet, 10 tojást és fél kiló paradicsomot, a tej pénteken jár le”). A rendszer ebből tételjavaslatokat készít, amelyeket mentés előtt jóváhagy vagy javít. Ha nem ad meg lejáratot, a rendszer a kategória alapján becsül. A nyilvántartás így annyiba kerül, mint egy üzenet megírása.
- **Tomi és Betti** a „mit főzzek?” kérdésre a készletből ténylegesen elkészíthető recepteket kapják, elöl azokkal, amelyek a hamarosan lejáró tételeket használják fel.
- **Zárt kör:** főzés után a felhasznált mennyiségek levonódnak, a hiányzó vagy fogyóban lévő hozzávalókból bevásárlójavaslat lesz, amit a felhasználó egy kattintással a listára tehet. A készlet így külön adminisztráció nélkül naprakész marad.

## 4. Siker definíció

Részletes mérési terv és célértékek: [metrics.md](metrics.md).

**North Star – Megmentett főzések:** aktív háztartásonként hetente megfőzött receptek átlagos száma, amelyek legalább egy *hamarosan lejáró* tételt felhasználnak. Hamarosan lejáró az a tétel, amelynek lejárati dátuma ma, holnap vagy holnapután van; a már lejárt tétel nem ilyen. A metrika azt méri, hogy a termék a lejárat előtt a fazékba juttatja-e az élelmiszert.

**Guardrailek:**

| # | Metrika | Mitől véd |
|---|---|---|
| G1 | **Pazarolt arány:** a lejárt tételek eredeti mennyiségének átlagosan hány százaléka ment pazarlásba (kidobva, vagy lejártan a készletben maradt) | A North Star nőhet úgy is, hogy közben ugyanannyi étel romlik meg (többet vásárol, többet főz). A G1 tartja a mérést a pazarlásnál. |
| G2 | **Korai megtartás:** a regisztrált felhasználók hány százaléka végzett legalább 3 készletbevitelt a regisztrációt követő 14 napban. Bármely csatornán (form vagy egy mondatos bevitel) történt, sikeresen mentett beküldés egy bevitelnek számít; az el nem fogadott mondat nem. | Ha a felhasználó abbahagyja a vezetést, az adat elavul, és a North Star értelmét veszti. Ez Tomi „abbahagytam” frusztrációja. |
| G3 | **Bevitel ideje:** a szöveg beküldésétől a jóváhagyott mentésig eltelt idő (p50/p90), benne az LLM válaszidejével | Egy lassú AI-bevitel rosszabb lehet, mint a kézi form, és elveszne az egy mondatos bevitel előnye. |

**Mérési korlát:** minden metrika a felhasználó által rögzített adatból számol. Ha a fogyasztást nem jelzi (például kézi levonás nélkül eszik meg valamit), a G1 túlbecsüli a pazarlást.

## 5. Non-goals

- **Táplálkozás- és kalóriakövetés:** nincs tápérték-, kalória- vagy diétakövetés, sem egészségügyi ajánlás. A termék a pazarlás csökkentésére fókuszál.
- **Többfelhasználós háztartás:** egy fiók = egy háztartás; a készletet egy ember vezeti. Közös háztartás több fiókkal, jogosultságkezelés és valós idejű szinkron nincs.
- **Egyéni ízlés- és allergiaprofilok:** a családtagok preferenciáit a termék nem tárolja; a receptválasztás a felhasználó döntése.
- **Natív mobilalkalmazás:** nincs iOS- vagy Android-app; a termék reszponzív, mobilról is használható webalkalmazás.
- **Bolti árak, akciók és online rendelés:** nincs árösszehasonlítás, akciókövetés, sem bolti vagy webshop-integráció; a bevásárlólista a felhasználó saját listája.

## 6. Kockázatok és bizonytalanságok

| # | Kockázat | Hatás | Mitigáció |
|---|---|---|---|
| R1 | **Hibás AI-kimenet:** rossz mennyiség vagy egység („fél kiló”, „egy csomag”), rosszul értelmezett relatív dátum („pénteken jár le”), kitalált tétel; AI-receptötletnél hibás hozzávaló vagy mennyiség | Hibás készlet, téves lejárat, használhatatlan recept | AI-kimenet jóváhagyás nélkül nem kerül az adatbázisba: a tételjavaslat és az AI-receptötlet is szerkeszthető űrlapon jelenik meg mentés előtt; séma-validáció; rögzített magyar mondatokból álló tesztkészlet, az eredmény a [verification_log](../07_ai/verification_log.md)-ba kerül |
| R2 | **Pontatlan készlet:** a felhasználó nem jelzi a főzésen kívüli fogyasztást | A készlet és minden metrika torzul, az ajánlás olyat is elkészíthetőnek mutat, ami már nincs meg | Főzés utáni automatikus levonás; gyors kézi levonás vagy „elfogyott” jelölés; mérési korlát kimondva a 4. szekcióban |
| R3 | **Hideg indulás a receptekkel:** új felhasználónak nincs saját receptje | Üres ajánlás, a North Star 0 marad | Induló receptkészlet: 20–40 egyszerű, magyar hétköznapi recept strukturált hozzávalókkal, saját szöveggel (nincs licenckérdés). Ezt a felhasználó kézi felvitellel és mentett AI-receptötletekkel (stretch) bővíti. Az AI-receptötlet mentés után „AI-javasolt” forrásjelölést kap. |
| R4 | **Hozzávaló-egyeztetés:** a készletben „trappista sajt 20 dkg”, a receptben „reszelt sajt 100 g” | Téves elkészíthetőség, rossz levonás főzés után | Kanonikus hozzávaló-lista és mértékegység-átváltás; az illesztés unit tesztekkel lefedve |