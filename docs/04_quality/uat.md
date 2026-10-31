# Felhasználói kipróbálás (UAT) – terv

Azt vizsgálja, hogy a célcsoportba tartozó felhasználó a valós feladatát el tudja-e végezni a Kamrával, egyedül, vezetés nélkül. A fejlesztő saját végigpróbálása nem UAT, hanem fejlesztői kézi teszt ([manual_tests.md](manual_tests.md), MT-03).

## Körök

| Kör | Időszak | Verzió | Tartalom | Résztvevők |
|---|---|---|---|---|
| 1. | 2026. okt. 26. – nov. 1. | `v0.4.0` | A determinisztikus út: készlet, „Mit főzzek?”, főzés, bevásárlólista, saját recept (US-1, US-3, US-4, US-5) | 4 fő (U1–U4) |
| 2. | 2026. nov. 16–27. | a nov. 13-i változat | Egy mondatos bevitel és a csak olvasó chat (US-2, US-6), az 1. kör javításai | 3–4 fő ugyanezek közül |
| Újratesztelés | dec. 4. előtt | a javított változat | Az 1–2. körben talált elakadások javítása után ugyanazok a forgatókönyvek | aki vállalja |

## Résztvevők

| Jel | Háztartás | Mit képvisel | Toborzás |
|---|---|---|---|
| U1 | egyedül él | Tomi (vision.md): főz, de nem tervez előre | meglévő jelentkező |
| U2 | egyedül él vagy párban | Tomi | meglévő jelentkező |
| U3 | egyedül él | Tomi | új, okt. 24-ig |
| U4 | család gyerekkel | Betti: többeknek főz, ő vásárol | új, okt. 24-ig |

A résztvevők nevét, elérhetőségét és a hozzájárulást a fejlesztő a repón kívül tartja; a dokumentáció csak a jelet (U1–U4), a háztartás típusát és a technikai jártasságot (alap / átlagos / haladó) rögzíti. A fejlesztő nem résztvevő.

## Lebonyolítás

- **Hol:** személyesen a fejlesztő laptopján, vagy képernyőmegosztással, ahol a résztvevő vezérel (távoli vezérlés). Az alkalmazás nem kerül ki az internetre: a laptopon fut, Docker Compose-zal, a `v0.4.0`-n ([deploy_runbook.md](../05_security_ops/deploy_runbook.md)).
- **Mobil:** az S5 a Chrome telefonnézetében (360 × 740 px) fut. A bejelentkezési süti `Secure`, ezért valódi telefonról a laptop helyi címén, `http`-n nem lehet bejelentkezni; ez az 1. kör korlátja.
- **Tesztfiók:** minden résztvevő maga regisztrál egy előre kiadott, kitalált címmel (`u1@example.com` …), nem a saját e-mail-címével.
- **Időtartam:** kb. 40 perc: 5 perc bevezetés és hozzájárulás, 5 × 5–7 perc feladat, 5 perc kérdések.
- **Hangos gondolkodás:** a résztvevőt arra kérjük, hogy mondja ki, mit keres és mit gondol.
- **A moderátor (a fejlesztő) szabályai:** a feladatot felolvassa és kártyán is odaadja, a képernyőt nem mutatja meg előre, és nem magyaráz. Ha a résztvevő 2 percig nem halad, egy rövid segítséget ad, és ezt feljegyzi. Kérdésre ezt válaszolja: „Mit gondolsz, hol lehet?”
- **Előtte:** a fejlesztő friss adatbázissal indítja a stacket (`docker compose down -v`, `docker compose up -d`), és ellenőrzi, hogy a `/health` válaszol.

## Forgatókönyvek

A feladatok célt adnak, nem lépéseket. Mindegyik a résztvevő saját fiókjában, az előző feladat állapotára épül.

| # | Feladat (amit a résztvevő kap) | Kezdőállapot és adatok | Siker | Lefedi |
|---|---|---|---|---|
| S1 | „Most jöttél haza a boltból. Vedd fel a kamrádba, amit vettél.” Kártya: 1 l tej, 6 tojás, 50 dkg paradicsom, 2 paprika (kb. 30 dkg), egy kis darab vaj (2 dkg). „A paprikán nincs dátum.” | Regisztráció után üres készlet | Mind az öt tétel a készletben, a paprikánál becsült lejárattal | US-1, ux_flows 1. flow (kézi ág) |
| S2 | „Este van, nincs kedved boltba menni. Mit tudsz főzni abból, ami otthon van? Főzz valamit, de csak két adagot.” | Az S1 készlete | Egy elkészíthető vagy majdnem elkészíthető recept megfőzve 2 adagra, a készlet csökkent | US-3, US-4, 2. flow |
| S3 | „Holnap vásárolsz. Rakd össze a bevásárlólistát: ami az előbb elfogyott, kell egy kenyér is, és a tej soha ne fogyjon el teljesen.” | Az S2 utáni állapot: ha a résztvevő a Rántottát főzte, a vaj elfogyott, és javaslat lett belőle | A kenyér a listán; a tejnek van minimumszintje; ha volt javaslat, döntött róla (elfogadta vagy elutasította) | US-5, 3. flow |
| S4 | „A nagymamád zabtejes kakaóját szereted. Vedd fel receptként, hogy a Kamra is ajánlhassa.” Kártya: 1 adag – 250 ml zabtej, 1 dkg kakaópor, 1 teáskanál (5 g) cukor. | A zabtej nincs a listán | A recept mentve a zabtejjel mint saját hozzávalóval | US-3 saját recept, 4. flow |
| S5 | Telefonnézetben: „Vegyél fel még egy receptet, a kedvenc szendvicsedet.” Félúton a moderátor: „Most hívtak, tedd le a telefont.” – frissíti az oldalt –, „Folytasd.” Utána: „Az egyik receptet sosem főznéd meg. Vedd ki, hogy ne ajánlja.” | 360 px; az S4 utáni állapot | A félbehagyott recept visszatöltve és mentve; egy recept archiválva | M14, US-3 archiválás, a 4. levél „mobil, megszakadt művelet” kérése |

## Mérés

- **Feladatonként:** siker (önállóan / segítséggel / nem sikerült), idő (a feladat kiadásától a sikerig vagy a feladásig), elakadások (hol, mennyi ideig, mit keresett), a képernyőn látott hibaüzenetek.
- **A végén három kérdés:**
  1. Mi volt a legkönnyebb?
  2. Hol akadtál el, vagy mi volt zavaró?
  3. Használnád-e a saját háztartásodban, és mi hiányzik hozzá?
- **Jegyzőkönyv:** résztvevőnként egy táblázat (forgatókönyv, siker, idő, elakadás, idézet), anonimizálva.

## Adatkezelés

- **Hozzájárulás:** szóban vagy üzenetben, a kipróbálás előtt. A résztvevő tudja, hogy szakdolgozathoz készül, a jegyzetek névtelenek, és bármikor abbahagyhatja.
- **Adatok:** csak kitalált tesztadat és tesztfiók. A kipróbálás után a fejlesztő törli az adatbázist (`docker compose down -v`). Képernyőfelvétel nem készül; a jegyzetekben nincs név és elérhetőség.

## Eredmények, javítás és újratesztelés

- Az 1. kör után (nov. 1.) az elakadások egy listába kerülnek súlyosság szerint: **blokkoló** (a feladat nem sikerült), **zavaró** (sikerült, de segítséggel vagy hosszú elakadással), **kozmetikai**.
- A blokkoló és a zavaró elakadások a nov. 13-i változatig javulnak; mindegyikhez teszt készül, és a javítás a 2. körben vagy a dec. 4. előtti újrateszteléskor ugyanazzal a forgatókönyvvel ellenőrződik.
- Az eredmények és a javítások ebbe a fájlba kerülnek („Eredmények – 1. kör”), és a dolgozat Értékelés fejezetébe. Ha valamelyik kör kevesebb résztvevővel zajlik, ezt itt jelezzük.
