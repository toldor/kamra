# Kamra

Egyszemélyes háztartási készlet- és receptkezelés, amelynek célja az otthoni élelmiszer-pazarlás csökkentése.

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
Egy beküldés, amellyel a felhasználó készletet rögzít (egy form-mentés vagy egy mondat), függetlenül attól, hány tételt tartalmaz.
_Avoid_: rögzítés, felvitel

**Kategória**:
A készlettétel rendszer által adott, fix típusa (pl. tejtermék, konzerv), amelyhez alapértelmezett eltarthatóság tartozik; a felhasználó nem bővíti.
_Avoid_: címke, csoport

**Mértékegység**:
A mennyiség fix, átváltható egysége: tömeg (g, dkg, kg), térfogat (ml, dl, l) vagy darab (db); tömeg és térfogat között nincs átváltás.
_Avoid_: csomag, fej, kiszerelés

**Becsült lejárat**:
A kategória alapértelmezett eltarthatóságából számolt lejárati dátum, ha a felhasználó nem adott meg lejáratot; becsültként jelölt és szerkeszthető.
_Avoid_: automatikus lejárat

**Hamarosan lejáró**:
Olyan készlettétel, amelynek lejárati dátuma a mai naptól számított 2 napon belül van; a már lejárt tétel nem hamarosan lejáró.
_Avoid_: lejáró, lejárat közeli

**Csökkenési ok**:
A készletcsökkenés felhasználó által megadott oka: *elfogyott* (fogyasztás), *kidobtam* (pazarlás) vagy *hibás rögzítés* (javítás); a főzés utáni levonás mindig *elfogyott*.
_Avoid_: törlés

**Pazarolt mennyiség**:
Egy tétel *kidobtam* okú csökkenéseinek összege, plusz a lejárat után a mérési ablak végén még készleten lévő mennyiség.
_Avoid_: veszteség, hulladék

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
