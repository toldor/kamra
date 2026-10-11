# Kamra

Egy ember által vezetett háztartási készlet- és receptkezelés, amelynek célja az otthoni élelmiszer-pazarlás csökkentése.

## Language

**Háztartás**:
A készlet tulajdonosa; ebben a scope-ban pontosan egy felhasználói fiókhoz tartozik, és egy ember vezeti.
_Avoid_: család, közös kamra

**Aktív háztartás**:
Olyan háztartás, amely egy adott héten legalább egy készletbevitelt vagy főzést végzett.

**Hozzávaló**:
Egy élelmiszer kanonikus megnevezése (pl. „tej”), amelyre a készlettételek és a receptek hivatkoznak; ezen keresztül illeszthető a készlet a receptekhez. Vagy a rendszer induló listájából származik, vagy egy háztartás saját, csak általa látható hozzávalója; a hozzávalók között nincs hierarchia.
_Avoid_: termék, alapanyag

**Készlettétel**:
Egy hozzávaló konkrét, a készletben lévő bejegyzése mennyiséggel, mértékegységgel és lejárattal (pl. „tej, 2 l, okt. 3.”); egy hozzávalóhoz több készlettétel is tartozhat.
_Avoid_: kamraelem, termék

**Minimumszint**:
Egy hozzávalóhoz opcionálisan megadott mennyiség, amely alá csökkenve a hozzávaló összes készlettételének összege bevásárlójavaslatot vált ki.
_Avoid_: küszöb, riasztási szint

**Bevásárlójavaslat**:
A rendszer által felajánlott, még el nem fogadott bevásárlólista-tétel; csak a felhasználó elfogadásával kerül a bevásárlólistára.
_Avoid_: automatikus bevásárlólista-tétel

**Tételjavaslat**:
Az AI által szabad szövegből kinyert, még nem végleges készlettétel, amely csak a felhasználó jóváhagyásával (vagy javításával) válik a készlet részévé.
_Avoid_: parse eredmény, AI tétel

**Készletbevitel**:
Egy sikeresen mentett beküldés, amellyel a felhasználó készletet rögzít (egy form-mentés vagy egy jóváhagyott mondatos bevitel), függetlenül attól, hány tételt tartalmaz; az el nem fogadott mondat nem készletbevitel.
_Avoid_: rögzítés, felvitel

**Kategória**:
A készlettétel rendszer által adott, fix típusa (pl. tejtermék, konzerv), amelyhez alapértelmezett eltarthatóság tartozik; a felhasználó nem bővíti.
_Avoid_: címke, csoport

**Mértékegység**:
A mennyiség fix, átváltható egysége: tömeg (g, dkg, kg), térfogat (ml, dl, l) vagy darab (db); tömeg és térfogat között nincs átváltás. Minden hozzávalónak fix dimenziója van (tömeg, térfogat vagy darab), és egysége csak ebből választható; a mennyiség alapegységben (g, ml, db) tárolódik, így a 30 dkg és a 300 g ugyanaz.
_Avoid_: csomag, fej, kiszerelés

**Becsült lejárat**:
A kategória alapértelmezett eltarthatóságából számolt lejárati dátum, ha a felhasználó nem adott meg lejáratot; becsültként jelölt és szerkeszthető.
_Avoid_: automatikus lejárat

**Hamarosan lejáró**:
Olyan készlettétel, amelynek lejárati dátuma ma, holnap vagy holnapután van (a mai naptól számított 2 napon belül, a mai napot is beleértve); a már lejárt tétel nem hamarosan lejáró.
_Avoid_: lejáró, lejárat közeli

**Csökkenési ok**:
A készletcsökkenés felhasználó által megadott oka: *elfogyott* (fogyasztás), *kidobtam* (pazarlás) vagy *hibás rögzítés* (javítás); a főzés utáni levonás mindig *elfogyott*.
_Avoid_: törlés

**Pazarolt mennyiség**:
Egy tétel *kidobtam* okú csökkenéseinek összege, plusz a lejárat után a mérési ablak végén még készleten lévő mennyiség.
_Avoid_: veszteség, hulladék

**Készletmozgás-napló**:
A készlettételek minden mennyiségváltozásának visszakereshető nyilvántartása mennyiséggel, időponttal és okkal (bevitel, főzés, csökkenési ok).
_Avoid_: audit log, history

**Recept**:
Mentett elkészítési leírás adagszámmal és hozzávalókkal (mennyiséggel és mértékegységgel); forrása az induló receptkészlet, a felhasználó kézi felvitele vagy egy jóváhagyott AI-receptötlet.

**Szükséges mennyiség**:
Főzésnél egy hozzávalóból a recept szerint kellő mennyiség, a megadott adagszámmal skálázva és kerekítve.
_Avoid_: receptigény, levonandó mennyiség

**Felhasznált mennyiség**:
A főzés megerősítésekor a felhasználó által jóváhagyott, ténylegesen felhasznált mennyiség; a készletből és a készletmozgás-naplóba csak ez kerül, és nem lehet több a készletnél.
_Avoid_: levont mennyiség

**Főzés**:
Az az esemény, amikor a felhasználó jelzi, hogy egy receptet adott adagszámmal megfőzött; a megerősítés után a felhasznált mennyiségek *elfogyott* okkal levonódnak a készletből.
_Avoid_: receptfelhasználás

**Elkészíthető**:
Olyan recept, amelynek minden hozzávalója (az alaphozzávalók kivételével) megvan a készletben a szükséges mennyiségben. A lejárt készlettétel is beleszámít.

**Hiány**:
Egy hozzávalóból hiányzó mennyiség: ajánlásnál a szükséges és a készleten lévő, főzésnél a szükséges és a felhasznált mennyiség különbsége, de legalább 0. Nem könyvelődik, de egy kattintással bevásárlólistára tehető.
_Avoid_: hiánycikk, maradék

**Majdnem elkészíthető**:
Olyan recept, amelynek legfeljebb 2 hozzávalója hiányzik a készletből vagy van belőle kevesebb a szükségesnél.

**Megmentett főzés**:
Olyan főzés, amely legalább egy hamarosan lejáró tételt felhasznál.

**Alaphozzávaló**:
A fix „mindig otthon van” hozzávalók (víz, só, bors), amelyeket az ajánlás és a főzés utáni levonás figyelmen kívül hagy.
_Avoid_: alapkészlet, fűszer

**Ajánlás**:
A mentett receptek determinisztikus, a hamarosan lejáró tételeket előre soroló listája a jelenlegi készlet alapján.
_Avoid_: receptjavaslat

**AI-receptötlet**:
Az AI által generált recept, amely csak a felhasználó jóváhagyásával és mentésével válik Receptté; mentés után „AI-javasolt” forrásjelölést kap.
_Avoid_: receptjavaslat, AI recept
