# Kézi tesztjegyzőkönyv

A kurzus-kiírás 5. pontja szerinti kézi tesztek: ki, mikor, melyik verzión, milyen környezetben, milyen lépésekkel, mit várt és mit kapott. A fejlesztő saját kipróbálása fejlesztői kézi teszt, nem felhasználói kipróbálás (UAT); az UAT terve: [uat.md](uat.md).

| ID | Tárgy | Dátum | Verzió | Végezte | Eredmény |
|---|---|---|---|---|---|
| MT-01 | A fő folyamat a fejlesztő gépén | 2026-10-17 | `v0.2.0` (`develop @ 2deb55e`) | a fejlesztő | PASS |
| MT-02 | Telepítés tiszta környezetben a runbook szerint, mért idővel | 2026-10-23 | `develop @ 7b6a484` | a fejlesztő | PASS, 2 runbook-eltéréssel (javítva) |
| MT-03 | Az UAT-forgatókönyvek fejlesztői végigpróbálása | a `v0.3.0` után, az UAT előtt (okt. 25.) | `v0.3.0` | a fejlesztő | tervezett |
| MT-04 | Visszaállás az előző kiadásra (rollback) a runbook szerint | a `v0.3.0` után | `v0.3.0` → `v0.2.0` → `v0.3.0` | a fejlesztő | tervezett |

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

Tervezett a `v0.3.0` kiadás után, az UAT első köre (okt. 26.) előtt: az [uat.md](uat.md) öt forgatókönyve asztali gépen és 360 px-en, a képernyőolvasós próbával (az élő régiók felolvasása, V-45, V-47). A jegyzőkönyv ide kerül.

## MT-04 – Visszaállás az előző kiadásra

Tervezett a `v0.3.0` kiadás után, a [deploy_runbook.md](../05_security_ops/deploy_runbook.md) „Visszaállás az előző verzióra” része szerint: mentés (`pg_dump`), `git checkout v0.2.0`, `docker compose up --build -d`, `/health` és bejelentkezés egy `v0.3.0`-ban létrehozott fiókkal, majd vissza a `v0.3.0`-ra ugyanígy. Elvárt: a régi kiadás elindul az új sémán, az adatok megmaradnak, és a visszatérés után a `v0.3.0` funkciói (bevásárlólista, saját recept) ugyanazokkal az adatokkal működnek. A jegyzőkönyv ide kerül.
