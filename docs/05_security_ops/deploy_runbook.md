# Telepítési útmutató (deploy runbook)

A Kamra egy gépen fut Docker Compose-zal: PostgreSQL 18, egy egyszer lefutó migrátor, és az API, amely a React-felületet is kiszolgálja ugyanarról a címről. Ez az útmutató a telepítést, az ellenőrzést, a mentést, a frissítést és a leállítást írja le. Az első, tiszta környezetű telepítés jegyzőkönyve: [manual_tests.md](../04_quality/manual_tests.md) (MT-02).

## Előfeltételek

| Mi | Verzió (kipróbálva) | Honnan |
|---|---|---|
| Git | bármely 2.x | Linux: `sudo apt-get install git`; Windows: https://git-scm.com |
| Docker Engine + Compose v2 (Linux) vagy Docker Desktop (Windows, macOS) | Engine 29.x, Compose v5.x | https://docs.docker.com/engine/install/ , https://www.docker.com/products/docker-desktop/ |
| Szabad port | 8080 (az alkalmazás), 5432 (az adatbázis) | a `.env`-ben átállítható |
| Lemezhely, memória | kb. 3 GB az image-eknek, 2 GB szabad memória | – |

Más nem kell: a .NET és a Node.js a build-konténerben fut.

## Telepítés

1. Klónozás:
   `git clone https://github.com/toldor/kamra.git && cd kamra`
   (Egy adott kiadás: `git checkout v0.3.0`.)
2. Konfiguráció: `cp .env.example .env` (Windows PowerShell: `Copy-Item .env.example .env`). Helyi kipróbáláshoz a helykitöltő értékek elegendők; más környezetben a `POSTGRES_PASSWORD`-ot cseréld le, és a `ConnectionStrings__Default`-ban is. A `.env` soha nem kerül a repóba.
3. Build és indítás: `docker compose up --build -d`
   A sorrend: `db` → `migrator` (EF Core-migrációk, egyszer fut le, utána kilép) → `api`. Az első build az image-ek letöltésével tiszta gépen kb. 1,5 perc volt a klóntól a `/health`-ig ([MT-02](../04_quality/manual_tests.md)); lassabb hálózaton néhány perc.
4. Állapot: `docker compose ps` – a `db` és az `api` fut, a `migrator` `Exited (0)` állapotú.

## Ellenőrzés

1. `curl http://localhost:8080/health` → `Healthy` (a folyamat fut, az adatbázis elérhető).
2. Böngészőben: http://localhost:8080 → *Regisztrálj* → e-mail-cím és legalább 15 karakteres jelszó → „Még üres a kamrád.”
3. Rövid füstpróba (P13): rögzíts paprika 500 g (2 nap), paradicsom 200 g (1 nap), vöröshagyma 300 g, étolaj 1 l → „Mit főzzek?”: a Lecsónál „paradicsom: 100 g hiányzik (200 g van, 300 g kell)” → „Megfőztem” → „Hiány a bevásárlólistára” → „Főzés megerősítése” → *„Felvettem a bevásárlójavaslatok közé: paprika.”* → „Megnézem”: a paradicsom 100 g a listán, a paprika javaslatként.

## Napló és hibakeresés

- `docker compose logs api` (JSON-sorok), `docker compose logs migrator`; élőben: `docker compose logs -f api`.
- Egy hibaválasz `correlationId`-je alapján: `docker compose logs api | grep <correlationId>` ([observability.md](observability.md)).
- Gyakori hibák: README, „Gyakori hibák”.

## Mentés és visszaállítás

- Mentés (a konténer saját környezeti változóival, a gépre):
  `docker compose exec -T db sh -c 'pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Fc' > kamra-$(date +%F).dump`
- Visszaállítás ugyanabba a verzióba (a meglévő adatok helyére):
  `docker compose exec -T db sh -c 'pg_restore -U "$POSTGRES_USER" -d "$POSTGRES_DB" --clean --if-exists' < kamra-2026-10-23.dump`
- A cookie-kulcsok a `dpkeys` volume-ban vannak; ha elvesznek, a felhasználóknak újra be kell jelentkezniük, adat nem vész el.

## Frissítés

1. Mentés (fent).
2. `git pull` (vagy `git checkout <új kiadás>`).
3. `docker compose up --build -d` – a migrátor lefuttatja az új migrációkat, az API csak a sikeres migráció után indul.
4. Ellenőrzés: `/health` és egy bejelentkezés.

A migrációk csak hozzáadnak (táblát, oszlopot, seed-sort); adatot törlő vagy átalakító migráció külön jóváhagyással és mentéssel jár (AGENTS.md 11.).

## Leállítás és törlés

- Leállítás, az adatok megmaradnak: `docker compose down`
- Minden adat törlése (adatbázis és cookie-kulcsok): `docker compose down -v`

## Linux: Docker Engine telepítése (Ubuntu 24.04)

A Docker hivatalos leírása szerint (https://docs.docker.com/engine/install/ubuntu/ – ha eltér, az a mérvadó):

    sudo apt-get update
    sudo apt-get install -y ca-certificates curl git
    sudo install -m 0755 -d /etc/apt/keyrings
    sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
    sudo chmod a+r /etc/apt/keyrings/docker.asc
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "${UBUNTU_CODENAME:-$VERSION_CODENAME}") stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
    sudo apt-get update
    sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
    sudo usermod -aG docker $USER

Utána jelentkezz ki és be (WSL-ben: `exit`, majd újra megnyitod a disztrót), és ellenőrizd: `docker version`, `docker compose version`. Ha a Docker szolgáltatás nem indul el magától: `sudo service docker start`.

## Windows: Docker Desktop

Telepítsd a Docker Desktopot (WSL 2 háttérrel), indítsd el, és a fenti „Telepítés” lépéseit PowerShellben futtasd (a `cp` helyett `Copy-Item`). A `curl` PowerShellben a `curl.exe`.

## Tiszta próbakörnyezet Windowson (WSL)

Így készült az első telepítési próba ([MT-02](../04_quality/manual_tests.md)): egy új, üres Linux a Windows-gépen, a Docker Desktoptól függetlenül. Rendszergazdai jog nem kell, ha a WSL már telepítve van.

1. A Docker Desktopot a próba idejére állítsd le (tálcaikon → jobb klikk → Quit Docker Desktop), hogy ne foglalja a 8080-as portot, és a `docker` parancsa se szivárogjon be. A „WSL integration” beállításokhoz ilyenkor nem kell nyúlni: az új disztró nem lesz alapértelmezett, és alapból nincs bekapcsolva.
2. Nyiss egy PowerShell-ablakot (Start menü → „PowerShell”), és futtasd: `wsl --install Ubuntu-24.04 --name kamra-clean`. Felhasználónevet és jelszót kér; a jelszó a `sudo`-hoz kell majd. Utána ugyanez az ablak már a disztró parancssora (a prompt: `felhasználó@gépnév:…$`), innentől a parancsok ott futnak. Később PowerShellből lépsz vissza: `wsl -d kamra-clean` (a Linuxon belül a `wsl` parancs nem létezik).
3. A WSL alapból hozzáfűzi a Windows-`PATH`-ot, így a Docker Desktop `docker` parancsa is látszana. Kapcsold ki. A `/etc/wsl.conf`-ban már van beállítás (`[boot] systemd=true`, `[user]`), ezért hozzáfűzni kell, nem felülírni:
   `printf '\n[interop]\nappendWindowsPath=false\n' | sudo tee -a /etc/wsl.conf`, majd `exit`, és PowerShellben `wsl --terminate kamra-clean`, `wsl -d kamra-clean`.
4. Ellenőrzés: `command -v docker || echo "nincs docker"` → „nincs docker”. Ha egy `/mnt/c/...` útvonalat ír, a 3. lépés nem lépett érvénybe.
5. Lépj a home-mappába (`cd ~`); a disztró a Windows-mappában (`/mnt/c/...`) indul, ott a build lassú.
6. Utána a „Linux: Docker Engine telepítése” rész (a parancsokat egyben is bemásolhatod), majd `exit` és PowerShellben `wsl -d kamra-clean`, hogy a `docker` csoport érvénybe lépjen. A `docker version` kimenetében a „Server” résznek is meg kell jelennie.
7. Utána a „Telepítés” rész; a felületet a Windows-böngészőből éred el (http://localhost:8080).
8. A végén: `docker compose down -v`, `exit`, majd PowerShellben `wsl --unregister kamra-clean`, és a Docker Desktop újraindítható.

## CI-titkok (GitHub Actions)

| Titok | Mire kell | Rotáció |
|---|---|---|
| `DOCKERHUB_USERNAME`, `DOCKERHUB_TOKEN` | A CI bejelentkezve tölti le a Docker Hub-image-eket, hogy a névtelen letöltési korlát ne állítsa meg | Docker Hub → Account settings → Personal access tokens: új, csak olvasási jogú token, a GitHub repó Settings → Secrets and variables → Actions-ben csere, a régi visszavonása |

Az alkalmazásnak más titka nincs: az adatbázis jelszava a gép `.env`-jében, a cookie-kulcsok a volume-ban vannak.
