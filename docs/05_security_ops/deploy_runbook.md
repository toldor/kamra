# Telepítési útmutató (deploy runbook)

A Kamra egy gépen fut Docker Compose-zal: PostgreSQL 18, egy egyszer lefutó migrátor, és az API, amely a React-felületet is kiszolgálja ugyanarról a címről. Ez az útmutató a telepítést, az ellenőrzést, a mentést, a frissítést és a leállítást írja le. Az első, tiszta környezetű telepítés jegyzőkönyve: [manual_tests.md](../04_quality/manual_tests.md) (MT-02).

## Környezetek és verziók

- **Környezet:** egy van, a helyi gép (fejlesztés, bemutató, UAT); staging és éles környezet nincs. A CI minden pushnál a Compose-stacket is elindítja, és ez ellen futnak az e2e tesztek.
- **Verziózás:** kiadás = annotált git tag a `main` ágon (`v0.1.0`, `v0.2.0`, `v0.3.0`), a változások a [changelog.md](../06_release/changelog.md)-ben. A futó verzió: `git describe --tags` a telepítés mappájában.

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
   A sorrend: `db` → `migrator` (EF Core-migrációk, egyszer fut le, utána kilép) → `api`. Az első build az image-ek letöltésével tiszta gépen kb. 1,5 perc volt a klóntól a `/health`-ig ([MT-02](../04_quality/manual_tests.md)); lassabb hálózaton néhány perc. Az első build közben a migrációs bundle lépése (`Building bundle...`) lassú hálózaton percekig nem ír ki semmit (MT-03: kb. 4,5 perc); ne szakítsd meg.
4. Állapot: `docker compose ps -a` – a `db` és az `api` fut, a `migrator` `Exited (0)` állapotú.

## Ellenőrzés

1. `curl http://localhost:8080/health` → `Healthy` (a folyamat fut, az adatbázis elérhető).
2. Böngészőben: http://localhost:8080 → *Regisztrálj* → e-mail-cím és legalább 15 karakteres jelszó → „Még üres a kamrád.”
3. Rövid füstpróba (P13): rögzíts paprika 500 g (2 nap), paradicsom 200 g (1 nap), vöröshagyma 300 g, étolaj 1 l → „Mit főzzek?”: a Lecsónál „paradicsom: 100 g hiányzik (200 g van, 300 g kell)” → „Recept megtekintése” → „Hiány a bevásárlólistára” → „Főzés megerősítése” → *„Felvettem a bevásárlójavaslatok közé: paprika.”* → „Megnézem”: a paradicsom 100 g a listán, a paprika javaslatként.

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

## Visszaállás az előző verzióra (rollback)

Ha egy frissítés után az alkalmazás nem indul, vagy hibásan működik:

1. Vissza az előző kiadásra: `git checkout <előző tag>` és `docker compose up --build -d`. A migrációk eddig csak hozzáadtak (táblát, oszlopot, seed-sort), ezért a régi kód az új sémán is fut.
2. Ha a frissítés adatot rontott el, vagy a régi kód mégsem fut az új sémán: állítsd vissza a frissítés előtti mentést (lásd „Mentés és visszaállítás”), utána az 1. lépés.

Kipróbálva 2026-10-25-én: `v0.3.0` → `v0.2.0` → `v0.3.0`, adatvesztés nélkül ([manual_tests.md](../04_quality/manual_tests.md), MT-04). A `v0.2.0` alatt az archivált receptek újra látszanak, és nincs bevásárlólista-oldal, mert ezek a `v0.3.0`-ban jöttek; az adatok megmaradnak, és a visszatérés után újra működnek.

**A `v0.3.0` előtti kiadásokban** az adatbázis portja minden hálózati címre kötött (`0.0.0.0:5432`, MT-04, E3). Ha a `.env`-ben a példajelszó van, a visszaállás idejére cseréld le, vagy ne legyen a gép megosztott hálózaton.

## Incidensek

### 1. Az alkalmazás nem válaszol, vagy tömegesen 500-as hibát ad

- **Tünet:** a felhasználó „Váratlan hiba történt. Próbáld újra később.” üzenetet lát, vagy a lap nem töltődik be; a `/health` nem `Healthy`.
- **Gyors diagnózis:**
  1. `docker compose ps -a` – fut-e az `api` és a `db`? A `migrator` `Exited (0)`?
  2. `curl http://localhost:8080/health` – 503 `Unhealthy`: az adatbázis nem érhető el (2. incidens).
  3. `docker compose logs --tail 100 api` – az `Error` szintű sorok és a `correlationId`; egy felhasználói hibához: `docker compose logs api | grep <correlationId>`.
  4. Ha az `api` nem indult el: `docker compose logs migrator` – a sikertelen migráció megállítja az indulást.
- **Ideiglenes megoldás:** `docker compose restart api`; sikertelen migráció vagy frissítés után rollback (lásd fent).

### 2. Megszakad az adatbázis-kapcsolat

- **Tünet:** minden adatkérés 500, a `/health` 503 `Unhealthy`; a naplóban Npgsql-kapcsolódási hiba.
- **Gyors diagnózis:**
  1. `docker compose ps db` – fut és `healthy`?
  2. `docker compose logs --tail 50 db` – leállt, elfogyott a hely, vagy hibás a jelszó?
  3. `df -h` – van-e szabad lemezhely (a volume a Docker adatkönyvtárában).
- **Ideiglenes megoldás:** `docker compose up -d db` (az API újra kapcsolódik, újraindítás nem kell); helyhiánynál `docker system prune` a régi image-ekre (a volume-ok maradnak). Ha a `.env` jelszava változott, a meglévő adatbázis a régi jelszóval maradt: vissza kell írni a régit.

### 3. A felhasználók nem tudnak bejelentkezni

- **Tünet:** „Túl sok kérés érkezett…” (429 `RATE_LIMITED`), a fiók zárolva (429 `LOGIN_LOCKED_OUT`), vagy újraindítás után mindenkit kiléptetett.
- **Gyors diagnózis:** a naplóban a 429-es sorok; ha mindenki egy címről jön (proxy, porttovábbítás), a rate limit közös. Ha mindenkit kiléptetett: megvan-e a `dpkeys` volume (`docker volume ls`).
- **Ideiglenes megoldás:** a zárolás 5 perc után magától feloldódik; a rate limit a `.env`-ben emelhető (`RateLimiting__Auth__PermitLimit`), majd `docker compose up -d`. Elveszett cookie-kulcsoknál a felhasználók egyszer újra bejelentkeznek, adat nem vész el.

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
