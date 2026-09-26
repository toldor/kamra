# Kamra

Egyszemélyes háztartási készlet- és receptkezelés, amelynek célja az otthoni élelmiszer-pazarlás csökkentése.

## Language

**Háztartás**:
A készlet tulajdonosa; ebben a scope-ban pontosan egy felhasználói fiókhoz tartozik, és egy ember vezeti.
_Avoid_: család, közös kamra

**Aktív háztartás**:
Olyan háztartás, amely egy adott héten legalább egy készletbevitelt vagy főzést végzett.

**Tételjavaslat**:
Az AI által szabad szövegből kinyert, még nem végleges készlettétel, amely csak a felhasználó jóváhagyásával (vagy javításával) válik a készlet részévé.
_Avoid_: parse eredmény, AI tétel

**Készletbevitel**:
Egy beküldés, amellyel a felhasználó készletet rögzít (egy form-mentés vagy egy mondat), függetlenül attól, hány tételt tartalmaz.
_Avoid_: rögzítés, felvitel

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

**Ajánlás**:
A mentett receptek determinisztikus, a hamarosan lejáró tételeket előre soroló listája a jelenlegi készlet alapján.
_Avoid_: receptjavaslat

**AI-receptötlet**:
Az AI által generált recept, amely csak a felhasználó jóváhagyásával és mentésével válik Receptté; mentés után „AI-javasolt” forrásjelölést kap.
_Avoid_: receptjavaslat, AI recept
