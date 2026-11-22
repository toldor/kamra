# Felhasználói kipróbálás (UAT) – terv

Azt vizsgálja, hogy a célcsoportba tartozó felhasználó a valós feladatát el tudja-e végezni a Kamrával, egyedül, vezetés nélkül. A fejlesztő saját végigpróbálása nem UAT, hanem fejlesztői kézi teszt ([manual_tests.md](manual_tests.md), MT-03).

## Körök

| Kör | Időszak | Verzió | Tartalom | Résztvevők |
|---|---|---|---|---|
| 1. | 2026. okt. 26. – nov. 1. | `v0.4.0` | A determinisztikus út: készlet, „Mit főzzek?”, főzés, bevásárlólista, saját recept (US-1, US-3, US-4, US-5) | 4 fő (U1–U4) |
| 2. | 2026. nov. 25–27. | a `v0.8.0` funkciói az élő példányon (`feature/live-deploy`, a `v0.9.0` jelöltje) | Az 1. kör forgatókönyvei (S1–S5) az 1. kör javításainak és az új felületnek az ellenőrzésére, és a beépített MI-funkciók: chat (S6), mondatos bevitel (S7), receptötlet (S8) (US-2, US-6, Stretch 1) | 3–4 fő ugyanezek közül |
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

- **Hol:** az 1. körben személyesen a fejlesztő laptopján (Docker Compose, `v0.4.0`), vagy képernyőmegosztással, ahol a résztvevő vezérel. A 2. kör az **élő példányon** fut (https://kamra-demo.austriaeast.cloudapp.azure.com, [ADR-0014](../02_architecture/adr/0014-elo-kitelepites-azure-caddy.md)), bekapcsolt MI-funkciókkal: a résztvevő a saját eszközén nyitja meg, a moderátor mellette ül vagy hívásban van. A tervben szereplő tartalék (laptop, ha az élő példány nov. 25-ig nem kész) nem kellett ([manual_tests.md](manual_tests.md) MT-05).
- **Mobil:** az 1. körben az S5 a Chrome telefonnézetében (360 × 740 px) futott, mert a `Secure` süti miatt valódi telefonról a laptop helyi címén, `http`-n nem lehetett bejelentkezni. A 2. körben az S5 a résztvevő **saját telefonján** fut, HTTPS-en (az MT-05-ben kipróbálva).
- **Tesztfiók:** minden résztvevő maga regisztrál egy előre kiadott, kitalált címmel (`u1@example.com` …), nem a saját e-mail-címével. A 2. körben a regisztrációhoz a meghívókód is kell: a moderátor kártyán adja oda.
- **Időtartam:** kb. 40 perc: 5 perc bevezetés és hozzájárulás, 5 × 5–7 perc feladat, 5 perc kérdések. A 2. körben a három új forgatókönyvvel kb. 50 perc.
- **Hangos gondolkodás:** a résztvevőt arra kérjük, hogy mondja ki, mit keres és mit gondol.
- **A moderátor (a fejlesztő) szabályai:** a feladatot felolvassa és kártyán is odaadja, a képernyőt nem mutatja meg előre, és nem magyaráz. Ha a résztvevő 2 percig nem halad, egy rövid segítséget ad, és ezt feljegyzi. Kérdésre ezt válaszolja: „Mit gondolsz, hol lehet?”
- **Előtte:** az 1. körben a fejlesztő friss adatbázissal indította a stacket (`docker compose down -v`, `docker compose up -d`). A 2. körben az élő példány fut (a `/health` ellenőrzése a kör előtt); a résztvevő új fiókja üres háztartással indul, a `tomi@pelda.hu` bemutató adatai nem keverednek vele.

## Forgatókönyvek

A feladatok célt adnak, nem lépéseket. Mindegyik a résztvevő saját fiókjában, az előző feladat állapotára épül.

| # | Feladat (amit a résztvevő kap) | Kezdőállapot és adatok | Siker | Lefedi |
|---|---|---|---|---|
| S1 | „Most jöttél haza a boltból. Vedd fel a kamrádba, amit vettél.” Kártya: 1 l tej, 6 tojás, 50 dkg paradicsom, 2 paprika (kb. 30 dkg), egy kis darab vaj (2 dkg). „A paprikán nincs dátum.” | Regisztráció után üres készlet | Mind az öt tétel a készletben, a paprikánál becsült lejárattal | US-1, ux_flows 1. flow (kézi ág; a 2. körben a résztvevő a mondatos bevitelt is választhatja, a jegyzőkönyv rögzíti, melyiket) |
| S2 | „Este van, nincs kedved boltba menni. Mit tudsz főzni abból, ami otthon van? Főzz valamit, de csak két adagot.” | Az S1 készlete | Egy elkészíthető vagy majdnem elkészíthető recept megfőzve 2 adagra, a készlet csökkent | US-3, US-4, 2. flow |
| S3 | „Holnap vásárolsz. Rakd össze a bevásárlólistát: ami az előbb elfogyott, kell egy kenyér is, és a tej soha ne fogyjon el teljesen.” | Az S2 utáni állapot: ha a résztvevő a Rántottát főzte, a vaj elfogyott, és javaslat lett belőle | A kenyér a listán; a tejnek van minimumszintje; ha volt javaslat, döntött róla (elfogadta vagy elutasította) | US-5, 3. flow |
| S4 | „A nagymamád zabtejes kakaóját szereted. Vedd fel receptként, hogy a Kamra is ajánlhassa.” Kártya: 1 adag – 250 ml zabtej, 1 dkg kakaópor, 1 teáskanál (5 g) cukor. | A zabtej nincs a listán | A recept mentve a zabtejjel mint saját hozzávalóval | US-3 saját recept, 4. flow |
| S5 | Telefonnézetben: „Vegyél fel még egy receptet, a kedvenc szendvicsedet.” Félúton a moderátor: „Most hívtak, tedd le a telefont.” – frissíti az oldalt –, „Folytasd.” Utána: „Az egyik receptet sosem főznéd meg. Vedd ki, hogy ne ajánlja.” | 360 px; az S4 utáni állapot | A félbehagyott recept visszatöltve és mentve; egy recept archiválva | M14, US-3 archiválás, a 4. levél „mobil, megszakadt művelet” kérése |
| S6 | Csak a 2. körben: „Kérdezd meg a Kamrát, mi jár le hamarosan, és mit főzhetsz belőle.” | Az S1–S3 utáni készlet, bekapcsolt chat | A válasz megnevez egy hamarosan lejáró tételt és egy receptet a saját adatokból; semmi nem módosul | US-6, [ux_flows.md](../01_product/ux_flows.md) 5. |
| S7 | Csak a 2. körben: „Hazafelé beugrottál a boltba. Rögzítsd gyorsan, egy mondatban, amit vettél.” Kártya: 1 kg finomliszt, 20 dkg trappista sajt, 2 dl tejföl. „A tejföl jövő pénteken jár le.” | Az S1–S6 utáni állapot, bekapcsolt mondatos bevitel | Mind a három tétel a készletben a helyes mennyiséggel; a tejföl lejárata a jövő péntek | US-2, [ux_flows.md](../01_product/ux_flows.md) 1. (mondatos ág) |
| S8 | Csak a 2. körben: „Kérj a Kamrától egy receptötletet abból, ami otthon van, és ha tetszik, tedd el a receptjeid közé.” | Az S7 utáni készlet, bekapcsolt chat | Egy receptötlet mentve, „AI-javasolt” címkével a Receptek között | Stretch 1, [ux_flows.md](../01_product/ux_flows.md) 5. (5. lépés) |

## Mérés

- **Feladatonként:** siker (önállóan / segítséggel / nem sikerült), idő (a feladat kiadásától a sikerig vagy a feladásig), elakadások (hol, mennyi ideig, mit keresett), a képernyőn látott hibaüzenetek.
- **A végén három kérdés:**
  1. Mi volt a legkönnyebb?
  2. Hol akadtál el, vagy mi volt zavaró?
  3. Használnád-e a saját háztartásodban, és mi hiányzik hozzá?
- **A 2. körben az MI-funkcióknál (S6–S8) azt is:** helyes volt-e az MI kimenete (a chat válasza a saját adatokból, a mondatból kinyert tételek, a receptötlet hozzávalói), és mit kellett a résztvevőnek kézzel javítania a javaslatkártyán vagy az „Új recept” űrlapon. Ez a beépített MI külön értékelése ([test_report.md](test_report.md) „Chat-mérés” mellé).
- **Jegyzőkönyv:** résztvevőnként egy táblázat (forgatókönyv, siker, idő, elakadás, idézet), anonimizálva.

## Adatkezelés

- **Hozzájárulás:** szóban vagy üzenetben, a kipróbálás előtt. A résztvevő tudja, hogy szakdolgozathoz készül, a jegyzetek névtelenek, és bármikor abbahagyhatja.
- **Adatok:** csak kitalált tesztadat és tesztfiók. Az 1. kör után a fejlesztő törölte az adatbázist (`docker compose down -v`); a 2. kör után az élő példányról a résztvevők fiókjait egyenként törli (a háztartás minden adatával, [privacy_licensing.md](../05_security_ops/privacy_licensing.md)). Képernyőfelvétel nem készül; a jegyzetekben nincs név és elérhetőség.
- **MI-funkciók (2. kör):** a mondatos bevitelbe beírt mondat, a chat kérdései és a saját, kitalált készletadat a Google Gemini API fizetős szolgáltatásához kerül ([privacy_licensing.md](../05_security_ops/privacy_licensing.md)). A résztvevő ezt a hozzájáruláskor megtudja, és azt kérjük tőle, hogy ne írjon be személyes adatot.

## Eredmények, javítás és újratesztelés

- Az 1. kör után (nov. 1.) az elakadások egy listába kerülnek súlyosság szerint: **blokkoló** (a feladat nem sikerült), **zavaró** (sikerült, de segítséggel vagy hosszú elakadással), **kozmetikai**.
- A blokkoló és a zavaró elakadások a `v0.6.0`-ban (nov. 12.) javultak; mindegyikhez teszt készül, és a javítás a 2. körben vagy a dec. 4. előtti újrateszteléskor ugyanazzal a forgatókönyvvel ellenőrződik.
- Az eredmények és a javítások ebbe a fájlba kerülnek („Eredmények – 1. kör”), és a dolgozat Értékelés fejezetébe. Ha valamelyik kör kevesebb résztvevővel zajlik, ezt itt jelezzük.

## Eredmények – 1. kör

- **Időpont, verzió:** 2026. nov. 1., `v0.4.0`, a fejlesztő laptopján, Docker Compose-zal.
- **Résztvevők:** 4 fő (U1–U4), mind az öt forgatókönyv (S1–S5).
- **Eredmény:** mind a 4 résztvevő mind az 5 forgatókönyvet önállóan, segítség nélkül teljesítette, résztvevőnként kb. 15 perc alatt (az öt forgatókönyv együtt). Elakadás nem volt, így blokkoló és zavaró elakadás sincs.
- **Visszajelzés:** a felület tetszett. Kívánság (egy résztvevő): a Receptek oldalon kis címke mutassa, hogy minden hozzávaló megvan-e.
- **Korlát:** résztvevőnkénti, forgatókönyvenkénti jegyzőkönyv (idő, elakadás, idézet) és a három záró kérdés válaszai nem készültek; az eredmény a moderátor összesítése. Az L1 és az L2 megfigyelését külön nem rögzítettük; mindkettő a `v0.6.0`-ban javult, teszttel. A 2. körben a jegyzőkönyv résztvevőnként készül.
- **Döntés a kívánságról:** nem elakadás, ezért nem kerül a `v0.6.0` javításai közé; a „Csak az elkészíthetők” szűrővel együtt későbbi továbbfejlesztési lehetőség ([scope_contract.md](../01_product/scope_contract.md), Későbbi továbbfejlesztési lehetőségek).
