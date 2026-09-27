# 0002 - Fix, átváltható mértékegységek „csomag” nélkül

Dátum: 2026-09-27
Státusz: Accepted

## Context

- **Probléma:** a főzés utáni levonás (US-4), az elkészíthetőség vizsgálata (US-3) és a minimumszint figyelése (US-5) csak akkor működik, ha a készlet és a recept mennyisége egymásba átváltható. A hétköznapi nyelv viszont nem átváltható egységeket is használ („egy csomag tejföl”, „egy fej káposzta”), és az egy mondatos bevitel (US-2) pont ilyen szövegekből dolgozik. Ha ezeket egységként elfogadjuk, az adott tétel kiesik a levonásból és az illesztésből ([vision.md](../../01_product/vision.md) R4 kockázat).
- **Kényszerek:** kb. 200 óra fejlesztési idő; hozzávalónkénti sűrűség- vagy kiszerelési adat nem áll rendelkezésre; az illesztésnek determinisztikusnak és unit tesztekkel ellenőrizhetőnek kell lennie.
- **Érintett minőségi attribútumok:** helyesség (a készlet pontossága), tesztelhetőség, használhatóság (a bevitel kényelme).

## Decision

- A rendszer csak fix, átváltható mértékegységeket fogad el: tömeg (g, dkg, kg), térfogat (ml, dl, l) és darab (db), tömeg és térfogat közötti átváltás nélkül.

## Alternatives

1) **A „csomag” (és hasonló) is egység** – előny: kényelmes, szó szerinti bevitel; hátrány: a csomagos tétel nem vonható le receptből, így kiesik az ajánlásból és a levonásból.
2) **Szabad szöveges egység** – előny: bármi rögzíthető; hátrány: az egyeztetés gyakorlatilag lehetetlen, a levonás nem determinisztikus.
3) **Tömeg–térfogat átváltás hozzávalónkénti sűrűséggel** – előny: a „2 dl liszt” és a grammban rögzített liszt is illeszkedne; hátrány: hozzávalónkénti sűrűségadat kell forrással és karbantartással, ami nem fér bele a scope-ba.

## Consequences

- **Pozitív:** a levonás, az elkészíthetőség és a minimumszint-figyelés determinisztikus, egyszerű szorzással számolható és unit tesztekkel lefedhető; a rendszer nem becsüli meg, hány gramm egy csomag („ne találgass” elv).
- **Negatív / kockázatok:** a bevitel kevésbé kényelmes, a „csomag” esetén a felhasználónak utána kell néznie a mennyiségnek; a „2 dl liszt” típusú recept nem illeszkedik a grammban rögzített liszthez, ezért az induló receptkészletet ehhez igazítva kell megírni.
- **Figyelni kell a megvalósítás során:** a rendszer belül alapegységben tárol (g, ml, db); ha az AI-bevitel nem átváltható egységet talál, a tételjavaslatból hiányzik az egység, és a megadása jóváhagyás előtt kötelező; a lista később bővíthető (például evőkanál → ml), ha az átváltás egyértelmű.

## Verification

- **Hogyan ellenőrizzük?** Unit tesztek: egységátváltás (dkg → g, dl → ml), tömeg–térfogat átváltás elutasítása, adagskálázás és kerekítés (US-4). US-2 teszteset: „egy csomag tejföl” bemenetre a tételjavaslat egység nélkül jön vissza, és mentés előtt a megadása kötelező.
- **Evidence link:** az 1. lépcső (US-1, US-4) és a 2. lépcső (US-2) tesztjei; a link az implementációval együtt kerül ide.
