# Kézi tesztjegyzőkönyv

A kurzus-kiírás 5. pontja szerinti kézi tesztek: ki, mikor, melyik verzión, milyen környezetben, milyen lépésekkel, mit várt és mit kapott. A fejlesztő saját kipróbálása fejlesztői kézi teszt, nem felhasználói kipróbálás (UAT); az UAT terve: [uat.md](uat.md).

| ID | Tárgy | Dátum | Verzió | Végezte | Eredmény |
|---|---|---|---|---|---|
| MT-01 | A fő folyamat a fejlesztő gépén | 2026-10-17 | `v0.2.0` (`develop @ 2deb55e`) | a fejlesztő | PASS |
| MT-02 | Telepítés tiszta környezetben a runbook szerint, mért idővel | 2026-10-23 | `develop @ 7b6a484` | a fejlesztő | PASS, 2 runbook-eltéréssel (javítva) |
| MT-03 | Az UAT-forgatókönyvek fejlesztői végigpróbálása | 2026-10-25 | `v0.3.0` (`main @ ad2d93a`) | a fejlesztő; az S2–S5 akadálymentességi pontjait a Claude Code automatikusan ellenőrizte | PASS, 2 akadálymentességi lelettel (L1, L2) |
| MT-04 | Visszaállás az előző kiadásra (rollback) a runbook szerint | 2026-10-25 | `v0.3.0` → `v0.2.0` → `v0.3.0` | a fejlesztő | PASS |

## MT-01 – A fő folyamat a fejlesztő gépén

- **Környezet:** a fejlesztő Windows-gépe, Docker Desktop, Docker Compose stack.
- **Lépések:** készlet rögzítése → „Mit főzzek?” → főzés adagszám-váltással, hiánnyal és hibás bevitellel.
- **Elvárt:** minden lépés a ux_flows 1–2. flow és a P1–P10 szerint.
- **Tényleges:** minden lépés a várt eredményt adta. A fejlesztő szerint a használat kényelmes volt.
- **Eredmény:** PASS.

## MT-02 – Telepítés tiszta környezetben

- **Cél:** a [deploy_runbook.md](../05_security_ops/deploy_runbook.md) egy olyan gépen, ahol a Kamra és a Docker még nincs telepítve; a DoD szerint a klóntól az első sikeres `/health`-ig legfeljebb 15 perc.
- **Verzió:** `develop @ 7b6a484` (a PR #16 merge-e: US-5, saját recept, 20 recept).
- **Környezet:** új WSL-disztró (`kamra-clean`), Ubuntu 24.04.5 LTS, kernel 6.18.54.1-microsoft-standard-WSL2, WSL 3.0.3.0, Windows 10.0.26200; Docker Engine 29.9.2 (containerd v2.4.2), Docker Compose v5.6.1. A Docker Desktop leállítva; a disztróban a Windows-`PATH` kikapcsolva (lásd Eltérések).

| # | Lépés | Elvárt | Tényleges | Idő |
|---|---|---|---|---|
| 1 | `wsl --install Ubuntu-24.04 --name kamra-clean` | új disztró | telepítve, felhasználó létrehozva | – |
| 2 | `command -v docker` | nincs docker | **először a Docker Desktop `docker`-e látszott** (`/mnt/c/Program Files/Docker/...`); a Windows-`PATH` kikapcsolása után „nincs docker” | – |
| 3 | Docker Engine telepítése (runbook) | `docker version` szerverrel | 29.9.2 kliens és szerver, hiba nélkül | 18:05:07 → 18:07:40 (2 p 33 mp) |
| 4 | `git clone`, `git checkout develop` | `7b6a484` | `7b6a484 Merge pull request #16` | 18:12:14 (a mérés kezdete) |
| 5 | `cp .env.example .env`, `docker compose up --build -d` | `db` egészséges, `migrator` kilép, `api` indul | a `postgres:18` letöltése 6,7 mp, a két image buildje 65,7 mp; `db` Healthy, `migrator` Exited, `api` Started | – |
| 6 | `until curl -fs …/health` | `Healthy` | `Healthy` | 18:13:40 (**1 p 26 mp a klóntól**) |
| 7 | Böngésző (Windows): regisztráció, P13 röviden | a runbook „Ellenőrzés” része | minden lépés a várt szöveggel; a végén a paprika javaslatként („Elfogyott (okt. 23., főzés: Lecsó).”), a paradicsom 100 g a listán | – |
| 8 | `docker compose down -v`, `wsl --unregister kamra-clean` | nyom nélkül eltűnik | lásd lent | – |

- **Mért idő:** a klóntól az első sikeres `/health`-ig **1 perc 26 másodperc**; a Docker Engine telepítésével együtt (18:05:07 → 18:13:40) 8 perc 33 másodperc, ebből kb. 4,5 perc a lépések közti kézi munka.
- **Bizonyíték:** [mt02-shopping-360.png](../assets/mt02-shopping-360.png) (a P13 végállapota a tiszta környezetben); a parancsok kimenetei a [V-52](../07_ai/verification_log.md)-ben összefoglalva.
- **Eltérések a runbooktól (mindkettő javítva a runbookban):**
  - **E1:** a WSL alapból hozzáfűzi a Windows-`PATH`-ot, ezért a Docker Desktop `docker` parancsa az új disztróban is látszott. Így a mérés nem a tiszta telepítést mérte volna. Javítás: `appendWindowsPath=false` a `/etc/wsl.conf`-ban, új runbook-rész „Tiszta próbakörnyezet Windowson (WSL)”.
  - **E2:** a disztró a Windows-mappában (`/mnt/c/Users/...`) indult; ott a build lassú lenne. Javítás: a runbook a home-mappát (`cd ~`) írja elő.
- **Eredmény:** **PASS.** A telepítés tiszta környezetben a runbook szerint, a 15 perces cél alatt sikerült.


## MT-03 – Az UAT-forgatókönyvek fejlesztői végigpróbálása

- **Cél:** az [uat.md](uat.md) öt forgatókönyve az UAT első köre (okt. 26.) előtt, a résztvevők verzióján: elvégezhető-e mind, és hallja-e a képernyőolvasó a visszajelzéseket (V-45, V-47).
- **Verzió:** `v0.3.0` (`main @ ad2d93a`).
- **Környezet:** a fejlesztő gépe, Windows 11 25H2 (build 26200), Docker Desktop (Engine 29.4.2, Compose 5.1.3); a repó külön klónja (`kamra-mt`), a `.env`-ben `COMPOSE_PROJECT_NAME=kamra-mt`, így saját volume-okkal és friss adatbázissal ([V-58](../07_ai/verification_log.md)); Chrome 155.0.8059.86, inkognitó ablak; Narrátor (a Windows része).
- **Lépések:** a forgatókönyvek résztvevőként, a kártyák adataival, saját fiókkal; S1–S4 asztali nézetben, S5 a Chrome eszköznézetében (360 × 740 px).

| # | Idő | Siker | Eltérés |
|---|---|---|---|
| S1 | 2 p | önállóan | nincs |
| S2 | 1 p | önállóan | nincs |
| S3 | 1 p | önállóan | nincs |
| S4 | 4 p | önállóan | nincs |
| S5 | 5 p | önállóan | nincs |

- **Akadálymentesség:** az S1-et a fejlesztő a Narrátorral próbálta. Az S2–S5 pontjait a Claude Code ellenőrizte Playwrighttal ugyanezen a stacken, külön fiókkal: rögzítette, hogy az élő régió (`role="status"`) üresen volt-e már a lapon, és a szöveg utána került-e bele, valamint hogy hova kerül a fókusz. A Narrátor felolvasását az S2–S5-nél nem hallgattuk meg ([V-59](../07_ai/verification_log.md)).

| Pont | Tényleges | Eredmény |
|---|---|---|
| S1: tétel mentése | a Narrátor semmit nem mond; a fókusz a lap törzsére esik | **L1** |
| S2: hiány a listára | „Felvettem a listára: …” élő régióban; a fókusz az üzeneten | PASS |
| S2: főzés megerősítése | a fókusz az „Elkészült: …, 4 adag.” címen | PASS |
| S2: bevásárlójavaslat | „Felvettem a bevásárlójavaslatok közé: …” nincs élő régióban | **L2** |
| S3: javaslat elfogadása | „Felvettem a listára: …” élő régióban; a fókusz az üzeneten, nem vész el (V-45) | PASS |
| S4: új hozzávaló kis űrlapja | nyitáskor a fókusz az első mezőn, záráskor a hozzávaló mezőjén; „Létrehoztam: …” élő régióban (V-47) | PASS |
| S4: mentés hibás mennyiséggel | a fókusz a mezőn, `aria-invalid`, a hibaszöveg a mezőhöz kötve; az összesítő élő régióban | PASS |
| S5: újratöltés után | „Visszatöltöttem a félbehagyott receptet.” a már meglévő élő régióba kerül (V-47) | PASS |
| S5: mentés, archiválás | mindkét üzenet élő régióban | PASS |

- **Leletek** (súlyosság az [uat.md](uat.md) szerint):
  - **L1 – zavaró (képernyőolvasóval):** a készlettétel mentésekor a „Hozzáadás” gomb a mentés idejére letiltódik, ezért a böngésző a fókuszt a lap törzsére teszi, és onnan semmi nem hozza vissza; sikerüzenet sincs. Képernyőolvasóval minden tétel után újra meg kell keresni az űrlapot. Javaslat: mentés után a fókusz a „Hozzávaló” mezőre, és egy élő üzenet („Felvettem a kamrádba: …”).
  - **L2 – kozmetikai:** a bevásárlójavaslatról szóló értesítés nincs élő régióban; a Narrátor csak akkor olvassa fel, ha a felhasználó a cím után továbbolvas.
  - Mindkettő az UAT 1. köre után, a többi elakadással együtt, a nov. 13-i változatban javul, teszttel. Az UAT-ot nem befolyásolja, a résztvevők egérrel dolgoznak.
- **Megfigyelés (telepítés):** az első `docker compose up --build` a migrációs bundle lépésénél (`Building bundle...`) kb. 4,5 percig nem írt ki semmit, ezért a fejlesztő megszakította. Újraindítva hibátlanul elkészült (a bundle-lépés 273,7 mp). A `v0.2.0` későbbi buildje 40 mp volt, tehát az első futás a csomagok letöltése miatt volt lassú. A runbook ezt most jelzi (E1).
- **Eltérések a runbooktól (mindkettő javítva a runbookban):**
  - **E1:** a runbook nem jelezte, hogy a bundle-lépés percekig kimenet nélkül futhat.
  - **E2:** a runbook szerint a `docker compose ps` a migrátort `Exited (0)` állapotban mutatja; a `ps` viszont csak a futó konténereket listázza. Helyesen: `docker compose ps -a`.
- **Eredmény:** **PASS.** Mind az öt forgatókönyv önállóan, eltérés nélkül sikerült; a V-45 és a V-47 javításai működnek; két új akadálymentességi lelet (L1, L2).

## MT-04 – Visszaállás az előző kiadásra

- **Cél:** a [deploy_runbook.md](../05_security_ops/deploy_runbook.md) „Visszaállás az előző verzióra” része: a régi kiadás elindul-e az új sémán, megmaradnak-e az adatok, és a visszatérés után működik-e minden.
- **Környezet és adatok:** az MT-03 stackje és adatai, két fiókkal (a fejlesztőé és az automatikus ellenőrzésé), saját receptekkel, archivált recepttel, bevásárlólistával és minimumszinttel.

| # | Lépés | Elvárt | Tényleges |
|---|---|---|---|
| 1 | Mentés (`pg_dump`, Git Bash) | nem üres mentés | 56 532 bájt, érvényes archívum (`PGDMP`, a `pg_restore --list` 17 tábla adatát látja) |
| 2 | Sorszámok előtte | – | fiók 2, saját recept 3, saját hozzávaló 2, archívum 2, készlettétel 10, főzés 2, listatétel 5, minimumszint 2, javaslat 3, migráció 7 |
| 3 | `git checkout v0.2.0`, `docker compose up --build -d` | a migrátor `Exited (0)`, `/health` `Healthy`, a sorszámok változatlanok | `2deb55e`; build 40,2 mp (a bundle-lépés 22,1 mp); a migrátor: „No migrations were applied. The database is already up to date.”, `Done.`, `Exited (0)`; `/health` `Healthy` 11:12:44-kor (2 p 6 mp az indítástól); mind a 10 sorszám egyezik. **Megfigyelés (E3):** az adatbázis portja `0.0.0.0:5432`-re kötve |
| 4 | Böngésző a `v0.2.0`-n | bejelentkezés; készlet és saját receptek megvannak; várt eltérés: az archivált recept látszik, bevásárlólista-oldal nincs | a fejlesztő szerint a várt eredmény, a két várt eltéréssel |
| 5 | `git checkout v0.3.0`, `docker compose up --build -d` | a migrátor `Exited (0)`, `/health` `Healthy` | `ad2d93a`; minden réteg cache-ből (1,8 mp); a migrátor `Exited (0)`, `Done.`; `/health` `Healthy` 11:14:49-kor (14 mp az indítástól); a port újra `127.0.0.1:5432` |
| 6 | Sorszámok utána | az előttivel egyezik | mind a 10 érték egyezik |
| 7 | Böngésző a `v0.3.0`-n | a lista és a minimumszint megvan; az archivált recept rejtve; a saját receptek szerkeszthetők | a fejlesztő szerint a várt eredmény |

- **Eredmény:** **PASS.** A `v0.2.0` elindult a `v0.3.0` sémáján, adat nem veszett el, és a visszatérés után a `v0.3.0` ugyanazokkal az adatokkal működik. A runbook állítása, hogy a régi kód az új sémán is fut, igazolódott ([V-57](../07_ai/verification_log.md)). A mentés visszatöltését (`pg_restore`) nem próbáltuk, mert adat nem sérült.
- **E3 – megfigyelés:** a `v0.2.0` alatt az adatbázis portja minden hálózati címen nyitva volt (`0.0.0.0:5432`), mert a `127.0.0.1`-es kötés csak a `v0.3.0`-ban került a compose-fájlba (`fdca415`, threat_model T-5). A `.env.example` jelszava nyilvános, ezért a `v0.2.0`-ra való visszaállás idejére a helyi hálózatról az adatbázis elérhető, ha a Windows tűzfala átengedi. A runbook visszaállási része erre figyelmeztet.
