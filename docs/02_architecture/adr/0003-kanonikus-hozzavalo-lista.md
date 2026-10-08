# 0003 - Kanonikus hozzávaló-lista hierarchia nélkül, háztartásonkénti bővítéssel

Dátum: 2026-09-27
Státusz: Accepted

## Context

- **Probléma:** az ajánlás (US-3), a főzés utáni levonás (US-4) és a bevásárlójavaslatok (US-5) azon múlnak, hogy a készlettétel és a recept hozzávalója ugyanarra a dologra hivatkozik-e. A felhasználó viszont szabadon fogalmaz: a készletben „trappista sajt”, a receptben „reszelt sajt” vagy „sajt” szerepelhet. Hibás egyeztetésnél az elkészíthetőség téves, és a levonás rossz tételt érint ([vision.md](../../01_product/vision.md) R4 kockázat).
- **Kényszerek:** kb. 200 óra fejlesztési idő; az illesztésnek determinisztikusnak és tesztelhetőnek kell lennie; a bevitel nem akadhat el hiányzó hozzávaló miatt; egy fiók = egy háztartás, a háztartások adatai elkülönülnek.
- **Érintett minőségi attribútumok:** helyesség (illesztés és levonás), tesztelhetőség, használhatóság, adatelkülönítés (security).

## Decision

- A készlettételek és a recepthozzávalók egy kanonikus hozzávalóra hivatkoznak, amely vagy a rendszer induló listájából származik, vagy a háztartás saját, csak általa látható hozzávalója, és a hozzávalók között nincs hierarchia.

## Alternatives

1) **Csak zárt, seedelt lista** – előny: tökéletes, determinisztikus illesztés; hátrány: hiányzó hozzávalót nem lehet rögzíteni, ami megtöri a bevitelt.
2) **Szabad szöveg automatikus (AI-os vagy fuzzy) egyeztetéssel** – előny: a felhasználónak nem kell választania; hátrány: nem determinisztikus, a hibás egyeztetés rejtett és nehezen tesztelhető.
3) **Hozzávaló-hierarchia („trappista sajt” ⊂ „sajt”)** – előny: a rokon hozzávalók is illeszkednek; hátrány: taxonómia-karbantartást és illesztési szabályokat igényel, ami nem fér bele a scope-ba.

## Consequences

- **Pozitív:** az illesztés determinisztikus (két tétel akkor ugyanaz, ha ugyanarra a hozzávalóra hivatkozik); a bevitel nem akad el; a minimumszint és a bevásárlójavaslat hozzávaló szinten működik, így a több készlettételből álló készlet összesítve kezelhető.
- **Negatív / kockázatok:** a háztartás saját hozzávalója kezdetben egyetlen induló recepthez sem illeszkedik, csak a saját receptjeihez; hierarchia nélkül a rokon hozzávalók nem helyettesítik egymást, ezért a felhasználónak rögzítéskor kell a megfelelőt választania.
- **Figyelni kell a megvalósítás során:** az induló lista (kb. 150–250 elem) tartalmazza az induló receptkészlet összes hozzávalóját, alapértelmezett kategóriával; az AI-bevitel a tételjavaslatban a meglévő listára próbál illeszteni, és jelzi, ha új hozzávalót hozna létre, a döntés a felhasználóé; a háztartás saját hozzávalója más háztartásban nem látszik. A hierarchia vagy a helyettesíthetőség később ráépíthető (például az AI-os helyettesítés stretch funkcióval) a kanonikus lista megváltoztatása nélkül.

## Verification

- **Hogyan ellenőrizzük?** Unit tesztek: azonos hozzávalóra hivatkozó tételek összesítése; a „trappista sajt” nem illeszkedik a „sajt”-ot kérő recepthez. Integrációs tesztek: a háztartás saját hozzávalója más háztartásban nem látszik; az induló receptkészlet minden hozzávalója szerepel az induló hozzávaló-listán.
- **Evidence link:** az 1. lépcső (US-1, US-3) tesztjei; a link az implementációval együtt kerül ide.
