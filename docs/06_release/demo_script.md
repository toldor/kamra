# Demóforgatókönyv (5–7 perc)

A fő folyamat (bevitel → ajánlás → főzés → bevásárlólista) és két hibahelyzet, stabilan végigvihető lépésekkel. Ugyanezt az utat a végpontok közti tesztek is bejárják ([shopping.spec.ts](../../tests/e2e/shopping.spec.ts), [pantry.spec.ts](../../tests/e2e/pantry.spec.ts)), és ez a [deploy_runbook.md](../05_security_ops/deploy_runbook.md) füstpróbája (P13) is.

## Előkészítés (a demó előtt, nem számít bele az időbe)

1. A stack fut: `docker compose up --build -d`, a `curl http://localhost:8080/health` válasza `Healthy` ([deploy_runbook.md](../05_security_ops/deploy_runbook.md)).
2. Az AI-részhez (3. és 7. lépés) a fejlesztő saját kulcsa kell: `Llm__Enabled=true` és `Llm__ApiKey` a környezetben, a README szerint. Kulcs nélkül ez a két lépés kimarad, a demó többi része változatlan: a mondatos bevitel panelje ilyenkor meg sem jelenik.
3. Két böngészőfül, mindkettő a http://localhost:8080 címen; egy kitalált fiók, például `tomi@pelda.hu`, legalább 15 karakteres jelszóval. A fiókot a demó elején regisztráld, így üres kamrából indulsz.

## Forgatókönyv

| # | Idő | Lépés | Mit mutat | Mit kell látni |
|---|---|---|---|---|
| 1 | 0:00 | Regisztráció a `tomi@pelda.hu` címmel | Bejelentkezés, háztartás | *„Még üres a kamrád.”* |
| 2 | 0:30 | Az „Új tétel” kártyán: paprika 500 g, lejárat holnapután; paradicsom 200 g, lejárat holnap; vöröshagyma 300 g, lejárat nélkül; étolaj 1 l | Kézi rögzítés, becsült lejárat | A vöröshagymánál a dátum mellett *„(becsült)”*; a paprikánál és a paradicsomnál *„Hamarosan lejár”* |
| 3 | 1:30 | *(AI, ha van kulcs)* „Mit vettél?”: *„Vettem 2 liter tejet, a tej péntekig jó.”* → „Feldolgozom” → a kártya átnézése → „Jóváhagyom (1)” | Mondatos bevitel jóváhagyással | Egy szerkeszthető kártya (tej, 2 l, péntek); a készletbe csak a jóváhagyás után kerül |
| 4 | 2:15 | „Mit főzzek?” | Ajánlás indoklással | A Lecsónál *„paradicsom: 100 g hiányzik (200 g van, 300 g kell)”* |
| 5 | 2:45 | Lecsó → „Hiány a bevásárlólistára”, majd „Recept megtekintése” | Hiány a listára; a főzés előnézete | Hozzávalónként „Kell / Van”, a levonás tételenként, a legkorábban lejárótól |
| 6 | 3:30 | „Főzés megerősítése” | Levonás, idempotencia | *„Elkészült: Lecsó, 4 adag.”*; *„Felvettem a bevásárlójavaslatok közé: paprika.”* |
| 7 | 4:00 | *(AI, ha van kulcs)* Kérdezz: „Mi jár le hamarosan?” | Csak olvasó chat MCP-eszközzel | A válasz a valós készletből, alatta a „Megnéztem” sor |
| 8 | 4:30 | „Megnézem” → a Bevásárlólista oldalon a paprika-javaslat „Hozzáadom” 500 g-mal; a paradicsom kipipálása | Javaslat és döntés | A listán paprika 500 g és paradicsom 100 g; a javaslat megmondja az okát (*„Elfogyott … főzés: Lecsó”*) |
| 9 | 5:15 | **1. hibahelyzet – érvénytelen bevitel:** az „Új tétel” kártyán tej, mennyiség 0 → „Hozzáadás” | Validáció, érthető üzenet | A mező alatt: *„A mennyiség 0,001 és 100 000 között legyen, legfeljebb 3 tizedesjeggyel.”*; semmi nem mentődik, a kitöltött mezők megmaradnak |
| 10 | 5:45 | **2. hibahelyzet – egyidejű módosítás:** a két fülön ugyanannak a tételnek (vöröshagyma) a „Módosítás” párbeszédablaka; az 1. fülön 200 g, mentés; a 2. fülön 250 g, mentés | Optimista zárolás (ADR-0008) | A 2. fülön: *„Ezt a tételt közben máshol módosították. Nézd át a friss adatokat, és mentsd újra.”*; a friss mennyiség (200 g) látszik, semmi nem íródott felül csendben |
| 11 | 6:30 | Zárás: a Készlet oldal összegző sávja és a Profilom → „Kijelentkezés” | Kör bezárva; a kijelentkezés minden eszközön | A paprika eltűnt a készletből, a paradicsom 100 g-mal a listán |

## Ha valami nem a terv szerint megy

- **Az AI-lépés hibát ad** (a szolgáltató lassú vagy nem érhető el): ez maga is bemutatható hibahelyzet – *„Most nem tudom feldolgozni a mondatot, mert a szövegfeldolgozó nem válaszol. A szöveged megmaradt…”*, és a kézi űrlap egy kattintásra nyílik (H1, [ux_flows.md](../01_product/ux_flows.md)). Utána a demó a 4. lépéssel folytatódik.
- **A „Mit főzzek?” nem a Lecsót mutatja:** ellenőrizd a 2. lépés mennyiségeit és lejáratait; a rangsor determinisztikus (QA-2), azonos készletre azonos.
- **Az API újraindítása után az első oldalbetöltés lassú** (hideg indulás, [test_report.md](../04_quality/test_report.md) „Flaky / instabil tesztek”): a demó előtt egyszer töltsd be az oldalt.

## Ismert korlátok

- Az AI-lépésekhez valódi kulcs és internetkapcsolat kell; a CI és a tesztek csak csonkkal futnak, ezért a demóban látott AI-válasz szövege futásonként eltérhet.
- A lépéseket a végpontok közti tesztek bejárják (az AI-lépéseket LLM-csonkkal); kézi, időmért végigjátszás a leadás előtt esedékes, és az eredménye ide kerül.
