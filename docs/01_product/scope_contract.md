# Scope Contract

Ez a dokumentum rögzíti, mi épül meg a projekt keretén belül, és mi nem. A termék célja és személyei: [vision.md](vision.md).
Hatókörön kívüli fejlesztés csak explicit kérésre indulhat (lásd `AGENTS.md` 11. pont).

## 1. MVP user story-k

A story-k prioritási sorrendben, három lépcsőben készülnek. Minden lépcső végén a rendszer önmagában működő és demózható.

| Lépcső | Story-k |
|---|---|
| 1. Determinisztikus mag | US-1, US-3, US-4, US-5 |
| 2. AI-bevitel | US-2 |
| 3. Chat | US-6 |

**Keresztmetszeti követelmény – fiók és adatelkülönítés:** e-mail + jelszó alapú regisztráció és bejelentkezés; egy fiók = egy háztartás. Minden story feltételezi a bejelentkezett felhasználót.

- Bejelentkezés nélkül egyetlen adatvégpont sem érhető el (401).
- Egy felhasználó más háztartás adatait nem látja és nem módosíthatja, akkor sem, ha az azonosítót ismeri (404). Integrációs teszt ellenőrzi.

### US-1 – Készlet kezelése

*Felhasználóként rögzíteni, szerkeszteni és törölni akarom a készlettételeimet, és látni akarom, mi jár le hamarosan, hogy ne felejtsek el semmit.*

- Készlettétel mezői: hozzávaló, mennyiség, mértékegység (g, dkg, kg, ml, dl, l, db), kategória (fix lista), lejárat (opcionális).
- Ha nincs megadva lejárat, a rendszer a kategória alapértelmezett eltarthatóságából **becsült lejáratot** számol, amely becsültként jelölve látszik és szerkeszthető. Az „egyéb” kategóriánál a lejárat kötelező.
- Érvénytelen bevitel (0 vagy negatív mennyiség, ismeretlen egység, hiányzó hozzávaló) nem mentődik, és a hibaüzenet megmondja, mit kell javítani.
- A lista kereshető név szerint, szűrhető kategória szerint, és van **„hamarosan lejáró”** szűrő (lejárat ma, holnap vagy holnapután). A lejárt tételek külön jelölve látszanak.
- Kézi készletcsökkentésnél a **csökkenési ok** kötelező (elfogyott / kidobtam / hibás rögzítés), és minden változás a készletmozgás-naplóba kerül. Ez a törlésre és a mennyiség szerkesztéssel történő csökkentésére is vonatkozik: a törlés egy 0-ra csökkentés csökkenési okkal.

### US-2 – Egy mondatos bevitel

*Felhasználóként egy mondattal akarom rögzíteni a bevásárlásomat, hogy ne kelljen tételenként kitöltenem a formot.*

- A beküldött szövegből a rendszer **tételjavaslatokat** készít, amelyek szerkeszthető formában jelennek meg. Jóváhagyás nélkül semmi nem kerül a készletbe.
- Relatív dátumok a beküldés napjához képest: „ma”, „holnap”, „holnapután”; a hét napja a következő ilyen napot jelenti, és ha ma van, a mait. Abszolút dátum („okt. 3.”, „10.03”) is elfogadott. Minden más kifejezésnél becsült lejárat jön, becsültként jelölve.
- Nem átváltható egység („egy csomag tejföl”) esetén a tételjavaslatból hiányzik az egység, és jóváhagyás előtt meg kell adni.
- LLM-időtúllépés (15 mp próbálkozásonként) vagy kiesés esetén egy automatikus újrapróbálás történik. Ha az is sikertelen, vagy a válasz nem felel meg a sémának, vagy egyetlen tétel sem nyerhető ki: magyar nyelvű hibaüzenet jelenik meg, a beírt szöveg megmarad, egy kattintással elérhető a kézi form, és semmi nem mentődik.
- Ha egy hozzávalóból már van készlet, a tételjavaslat tájékoztat róla („Már van otthon: tej 0,5 l, okt. 2-án jár le”), és jóváhagyáskor mindig új készlettétel jön létre (nincs összevonás).
- A jóváhagyás egy tételjavaslatra csak egyszer fut le (idempotens): dupla kattintás vagy a kérés újraküldése nem hoz létre duplikált készletet.
- A tételjavaslat beküldési és jóváhagyási időpontja tárolódik (G3 metrika, [metrics.md](metrics.md)).

### US-3 – Receptek és ajánlás

*Felhasználóként látni akarom, mit főzhetek abból, ami otthon van, elöl azzal, ami hamarosan lejáró alapanyagot használ fel.*

- Induló receptkészlet: 20–40 magyar hétköznapi recept, strukturált hozzávalókkal. Saját recept kézzel is felvihető (név, adagszám, hozzávalók mennyiséggel és egységgel, elkészítés).
- **Elkészíthető** a recept, ha minden hozzávaló megvan a szükséges mennyiségben. Rangsor: (1) a felhasznált hamarosan lejáró készlettételek száma szerint csökkenő, (2) a legkorábbi lejárat szerint, (3) név szerint.
- **Majdnem elkészíthető** a recept, ha legfeljebb 2 hozzávaló hiányzik vagy kevés. A hiányzók látszanak, és egy kattintással a bevásárlólistára tehetők.
- Minden ajánlás mellett egy mondatos indoklás áll (például „2 hamarosan lejáró hozzávalót használ fel: tejföl, paradicsom”).
- Az **alaphozzávalók** (víz, só, bors) nem számítanak bele az illesztésbe.
- Azonos készletre és receptkészletre az ajánlás sorrendje mindig ugyanaz (determinisztikus, unit teszttel rögzítve).

### US-4 – Főzés és készletcsökkenés

*Felhasználóként jelezni akarom, hogy megfőztem egy receptet, és azt akarom, hogy a készlet ehhez igazodjon.*

- Főzéskor megadható az adagszám (alapértelmezés: a recept adagszáma). A levonandó mennyiség lineárisan skálázódik; darabnál felfelé kerekítünk egészre, g-nál és ml-nél egészre.
- Levonás előtt egy megerősítő képernyő mutatja a levonandó mennyiségeket, és ott tételenként módosíthatók.
- A levonás a legkorábban lejáró készlettételből indul (FEFO). A készlet nem lehet negatív: ha nincs elég, a tétel 0-ra csökken, és a képernyő jelzi az eltérést.
- A levonandó mennyiségek a megerősítés pillanatában a friss készletből számolódnak; ha a készlet az ajánlás megnyitása óta változott, a megerősítő képernyő az eltérést jelzi.
- A megerősítés után egy összegző sor jelzi, mely hozzávalók fogytak el, és kerültek a bevásárlójavaslatok közé (US-5).
- Minden levonás *elfogyott* okkal kerül a készletmozgás-naplóba, a főzés-esemény pedig tárolódik (időpont, recept, felhasznált tételek; North Star metrika).

### US-5 – Bevásárlólista

*Felhasználóként olyan bevásárlólistát akarok, amely abból áll, ami tényleg hiányzik, és amiről én döntök.*

- Bevásárlójavaslat keletkezik, ha (a) egy hozzávaló összes készlettétele *elfogyott* okkal 0-ra csökken, vagy (b) a hozzávaló összmennyisége az opcionálisan megadott **minimumszint** alá esik. *Kidobtam* ok nem vált ki javaslatot.
- A bevásárlójavaslatok a bevásárlólista felületén jelennek meg, és ott elfogadhatók (felkerülnek a listára) vagy elutasíthatók.
- Elutasított javaslat addig nem jelenik meg újra, amíg a hozzávaló a feltétel fölé nem kerül, majd újra alá nem esik. Amíg egy elfogadott tétel a listán van, nem keletkezik új javaslat.
- A lista kézzel is bővíthető, és a „majdnem elkészíthető” receptből is (US-3). A tételek kipipálhatók és törölhetők. Ugyanaz a hozzávaló nem szerepel kétszer, a mennyiségek összeadódnak.

### US-6 – Chat asszisztens

*Felhasználóként élő nyelven akarom megkérdezni, mi van otthon, mi jár le hamarosan, és mit főzhetek.*

- A chat négy, csak olvasó MCP toolt használhat: készlet lekérdezése, hamarosan lejáró tételek, ajánlás (ugyanaz a logika, mint a US-3-ban), bevásárlólista megtekintése.
- A chat semmilyen adatot nem módosít. Módosítást kérő üzenetre elmondja, hogy ezt a felületen lehet megtenni.
- A toolok csak a bejelentkezett felhasználó háztartásának adatait érik el.
- LLM-kiesés esetén magyar hibaüzenet jelenik meg; a felület többi része működik tovább.

## 2. Stretch (ha az MVP kész és stabil)

Sorrendben, az MVP Definition of Done teljesülése után:

1. **AI-receptötlet mentése:** az AI által generált recept szerkeszthető űrlapon jelenik meg, és jóváhagyás után „AI-javasolt” forrásjelöléssel a receptek közé menthető. Mentés után az ajánlásban ugyanúgy szerepel, mint bármely más recept.
2. **AI-os helyettesítés és adagjavaslat:** egy receptnél az AI magyarázattal javasol helyettesítő hozzávalót („nincs tejföl → görög joghurt”) és adagarányt. A javaslat az ajánlás rangsorát nem befolyásolja, és a készletet nem módosítja.
3. **Kipipált tételből készletbevitel:** a bevásárlólistán kipipált tétel előre kitöltött készletbeviteli formot nyit (hozzávaló, mennyiség, egység), amit a felhasználó lejárattal kiegészítve ment.

## 3. Korlátok

### Idő

Kapacitás: kb. 20 óra/hét, összesen kb. 200 óra. Az ütemezés a témavezetővel egyeztetett (2026. okt.) mérföldkövekhez igazodik.

| Időszak | Tartalom | Mérföldkő |
|---|---|---|
| szept. 28 – okt. 4. | Alapok: repo, Docker Compose, CI, regisztráció és bejelentkezés, tervezési dokumentumok | Kész: v0.1.0 |
| okt. 5 – okt. 16. | Az 1. lépcső tervezési dokumentumai; US-1; ajánlás és főzés 3–5 recepttel (US-3, US-4 alapútja) | okt. 16.: működő készletkezelés és bemutatható ajánlás–főzés folyamat, kézzel ellenőrzött mennyiségekkel |
| okt. 17 – okt. 23. | US-5; US-3 és US-4 hibás esetei; az induló recept- és hozzávalólista bővítése | okt. 23.: a készlet–recept–főzés–bevásárlás teljes útja (1. lépcső kész) |
| okt. 24 – nov. 6. | 2. lépcső: LLM-mérés (PoC), US-2 | nov. 6.: stabil szakmai mag és kipróbálható MI-bevitel |
| nov. 7 – nov. 20. | 3. lépcső: US-6; mérések | nov. 20.: feature freeze és a dolgozat teljes első változata |
| nov. 21 – dec. 4. | Hibajavítás, telepítés kipróbálása, felhasználói próbák, a dolgozat javítása | dec. 4.: javított dolgozat és alkalmazás (saját befejezési cél) |
| dec. 5 – dec. 11. | Végső ellenőrzés | dec. 11.: végső belső ellenőrzési csomag |

- **Feature freeze: nov. 20.** Utána csak javítás és dokumentáció kerül be.
- **Hivatalos határidők** (a témavezető tájékoztatása szerint a TTIK kari naptára alapján, egyedileg ellenőrizendő): jelentkezés a januári záróvizsgára okt. 31., dolgozatbeadás dec. 19.
- **Döntési pont – okt. 23.:** az 1. lépcső eredménye alapján a témavezetővel döntünk a januári záróvizsga tarthatóságáról; csúszás esetén későbbi záróvizsga.
- A dolgozat a fejlesztéssel párhuzamosan készül: minden lépcső végén a hozzá tartozó fejezet.
- Stretch csak akkor, ha a 3. lépcső a freeze előtt kész és zöld. A jelenlegi ütemezés alapján valószínűleg nem fér bele.
- Csúszás esetén először a US-6 szűkül, **előzetesen egyeztetve a témavezetővel**. Az 1. és a 2. lépcső védett.

### Adat

- **Induló receptkészlet:** 20–40 saját szöveggel írt magyar recept, így nincs licenckérdés.
- **Induló hozzávaló-lista:** kb. 150–250 magyar hozzávaló alapértelmezett kategóriával. A felhasználó háztartásonként saját hozzávalót is létrehozhat. A hozzávalók között nincs hierarchia („trappista sajt” ≠ „sajt”): az új hozzávaló csak a saját receptekben illeszkedik.
- **Kategóriák:** fix lista, alapértelmezett eltarthatósággal; a napértékek forrása az adatmodell-dokumentációban szerepel.
- **Tesztadat:** kizárólag szintetikus. Valós személyes adat csak a tesztfelhasználók e-mail-címe.

### Platform

- Reszponzív webalkalmazás; támogatott böngészők: Chrome, Edge és Firefox aktuális verziója, asztalon és mobilon.
- Legkisebb támogatott képernyőszélesség: 360 px, vízszintes görgetés nélkül.
- A felület nyelve kizárólag magyar.
- Az e2e tesztek asztali Chrome-on és 360 px-es mobil nézetben futnak.

### Külső API

- Egy külső LLM-szolgáltató, az Application rétegben definiált interfész mögött, cserélhetően. A szolgáltató kiválasztása és indoklása külön ADR-ben szerepel.
- CI-ban és automatizált tesztekben soha nincs valódi LLM-hívás (mock).
- Az LLM csak a felhasználó beírt szövegét és a válaszhoz szükséges készletadatot kapja meg; fiókadatot (e-mail, jelszó) soha.
- Havi költségkeret a fejlesztés alatt: legfeljebb 10 000 Ft. Átlépés esetén az AI-funkciók kikapcsolhatók, a kézi form és a determinisztikus funkciók működnek tovább.
- Időtúllépés: 15 mp próbálkozásonként, egy újrapróbálással (US-2).

## 4. Kész definíciója (Definition of Done) a leadásra

A projekt akkor leadható, ha az alábbiak **mind** teljesülnek:

**Funkció**

- A US-1–US-6 minden elfogadási kritériuma teljesül. A [capability_map.md](capability_map.md) minden MVP- és Productization-sora „Done” státuszú, evidence- és tesztlinkkel.

**Tesztek és minőségi kapuk (CI-ban, zölden)**

- Build, formázás (`dotnet format`), lint (frontend) és minden teszt zöld a `main` ágon.
- Legalább 30 automatizált teszt: ≥ 18 unit, ≥ 6 integrációs (valódi Postgres, Testcontainers), ≥ 6 e2e vagy contract; ebből ≥ 5 negatív eset.
- Minden US-hez legalább egy e2e vagy integrációs teszt. A scope_contract determinisztikus szabályaihoz (becslés, relatív dátum, illesztés és rangsor, FEFO, adagskálázás, bevásárlójavaslat-szabályok) unit tesztek tartoznak.
- Sorlefedettség a Domain és az Application rétegben ≥ 80%, CI-ban mérve. A többi réteg lefedettsége riportolva, küszöb nélkül.
- Az adatelkülönítést (más háztartás adata nem érhető el) integrációs teszt ellenőrzi.
- A CI-ban nincs valódi LLM-hívás.
- A metrika-lekérdezések a szintetikus seed-adaton tesztekkel ellenőrzöttek ([metrics.md](metrics.md)).

**Futtathatóság és biztonság**

- A rendszer a README alapján, `docker compose up` paranccsal, 15 percen belül elindul egy tiszta gépen.
- A repóban nincs secret; minden környezeti változó szerepel a `.env.example`-ben, érték nélkül. A CI-ban függőség-sérülékenységi vizsgálat fut.

**Dokumentáció**

- A [00_index.md](../00_index.md) minden kötelező dokumentuma kész, TODO és üres szekció nélkül. A meg nem valósult elemek „Ismert korlátok” alatt szerepelnek.
- Legalább 8 ADR, köztük az LLM-szolgáltató és az authentikáció választása.
- AI-dokumentáció: [ai_manifest.md](../07_ai/ai_manifest.md) kitöltve; [prompt_log.md](../07_ai/prompt_log.md) 10–20 bejegyzéssel; [verification_log.md](../07_ai/verification_log.md) legalább 10 bejegyzéssel, amelyek közül legalább 3 tesztet eredményezett, és legalább 2 mérésen vagy PoC-n alapul.

**Demó**

- Egy 5–7 perces demóforgatókönyv, amely a fő flow-t (bevitel → ajánlás → főzés → bevásárlólista) és egy hibakezelési esetet stabilan végigvisz.

## Technikai hatókör

- REST API (ASP.NET Core) + React frontend
- Docker Compose-alapú fejlesztői és éles környezet
- Metrika-lekérdezések a [metrics.md](metrics.md) szerint (North Star + 3 guardrail), szintetikus seed-adaton tesztekkel ellenőrizve

## Hatókörön kívül

- Mobil natív alkalmazás (iOS / Android)
- Vonalkód-/QR-kód olvasó
- Bolti árak vagy kedvezmények integrálása
- Valós idejű többfelhasználós kollaboráció (websocket sync)
- Gépi tanulású saját modell betanítása
- E-mail / push értesítések
- Jelszó-visszaállítás és e-mail-megerősítés
- A chat asszisztens nem kezdeményez adatmódosítást (sem közvetlenül, sem javaslatként)
- Szerepkör-alapú hozzáférés-kezelés (RBAC) több felhasználó részére
- Táplálkozás- és kalóriakövetés
- Egyéni ízlés- és allergiaprofilok
- Online rendelés, bolti vagy webshop-integráció

## Későbbi továbbfejlesztési lehetőségek

Tudatosan kihagyott, de a jelenlegi tervezéssel összeegyeztethető bővítések (nem stretch, ebben a scope-ban nem készülnek el):

- **Saját „mindig otthon van” lista:** az alaphozzávalók (víz, só, bors) felhasználói bővítése.
- **Kategóriafüggő „hamarosan lejáró” küszöb:** a fix 2 nap helyett kategóriánként eltérő küszöb.
- **A kategória-eltarthatóság felhasználói felülírása** a háztartás saját szokásaihoz.
- **Hozzávaló-hierarchia és helyettesíthetőség** („trappista sajt” ⊂ „sajt”), lásd [ADR-0003](../02_architecture/adr/0003-kanonikus-hozzavalo-lista.md).
- **Tömeg–térfogat átváltás** hozzávalónkénti sűrűséggel, lásd [ADR-0002](../02_architecture/adr/0002-fix-atvalthato-mertekegysegek.md).
- **Automatizált akadálymentesség-ellenőrzés** az e2e tesztekben (axe-core); jelenleg Lighthouse-audit és kézi bejárás.
