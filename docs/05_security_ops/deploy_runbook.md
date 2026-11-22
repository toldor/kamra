# Telepítési útmutató (deploy runbook)

A Kamra egy gépen fut Docker Compose-zal: PostgreSQL 18, egy egyszer lefutó migrátor, és az API, amely a React-felületet is kiszolgálja ugyanarról a címről. Ez az útmutató a telepítést, az ellenőrzést, a mentést, a frissítést és a leállítást írja le. Az első, tiszta környezetű telepítés jegyzőkönyve: [manual_tests.md](../04_quality/manual_tests.md) (MT-02). A nyilvános, HTTPS-es élő példány (Azure VM, Caddy) telepítése lent, az „Élő példány (Azure)” szakaszban ([ADR-0014](../02_architecture/adr/0014-elo-kitelepites-azure-caddy.md), MT-05, MT-06).

## Környezetek és verziók

- **Környezetek:** (1) a helyi gép (fejlesztés, helyi bemutató); (2) az **élő példány**: https://kamra-demo.austriaeast.cloudapp.azure.com – Azure VM, ugyanaz a Compose a `docker-compose.prod.yml` felülíró fájllal, Caddy TLS-proxyval, 2027. jan. 22-ig (bemutató, UAT 2. kör, témavezetői kipróbálás). Staging nincs. A CI minden pushnál a Compose-stacket is elindítja, és ez ellen futnak az e2e tesztek.
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
- Minden adat törlése (adatbázis és cookie-kulcsok): `docker compose down -v` – **az élő példányon soha** (a `-v` a tanúsítványokat is törölné, lásd lent).

## Élő példány (Azure)

A döntés és az alternatívák: [ADR-0014](../02_architecture/adr/0014-elo-kitelepites-azure-caddy.md). Kipróbálva 2026-11-22-én ([manual_tests.md](../04_quality/manual_tests.md) MT-05, MT-06).

**Felépítés:** Azure for Students, erőforráscsoport `kamra-rg`, VM `kamra-vm` (Austria East, zóna nélkül, Standard_B2ats_v2: 2 vCPU, 836 MiB használható RAM; Ubuntu Server 24.04 LTS, 30 GB lemez), DNS-név `kamra-demo.austriaeast.cloudapp.azure.com`. A stack: `db`, `migrator`, `api` (kívülről nem érhető el) és `caddy` (80, 443; Let's Encrypt, tartalék a ZeroSSL).

### Első telepítés

1. **VM az Azure Portalon:** Virtuális gépek → Létrehozás → a fenti régió és méret, „Nincs szükség infrastruktúra-redundanciára”, SSH nyilvános kulcs (a saját gépen: `ssh-keygen -t ed25519 -f ~/.ssh/kamra_azure`, jelmondattal), nyilvános bejövő portok: 22, 80, 443; az automatikus leállítás **ki** (különben esténként leáll). A VM áttekintésében: DNS-név → `kamra-demo`.
2. **SSH szűkítése:** Hálózatkezelés → Hálózati beállítások → az SSH-szabály → Forrás: **Saját IP-cím** (My IP address). Ha az otthoni IP-cím változik, ugyanígy frissíteni kell, különben az SSH nem enged be.
3. **Belépés:** `ssh -i ~/.ssh/kamra_azure kamra@kamra-demo.austriaeast.cloudapp.azure.com`
4. **Frissítés, swap, Docker** (a VM-en): `sudo apt-get update && sudo apt-get -y upgrade`; 4 GB swap (a build 1 GB alatti memóriával csak így fut le):
   `sudo fallocate -l 4G /swapfile && sudo chmod 600 /swapfile && sudo mkswap /swapfile && sudo swapon /swapfile && echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab`;
   Docker a lenti „Linux: Docker Engine telepítése” szerint, utána `sudo reboot` (így a swap és a Docker újraindítás utáni indulása is ellenőrizve). Kipróbálva: Docker 29.10.0, Compose v5.8.0 (a `!reset` a v2.24-nél újabb Compose-t kér).
5. **Klón:** `git clone --branch <kiadás vagy ág> https://github.com/toldor/kamra.git && cd kamra`
6. **`.env`** (a titkok a VM-en keletkeznek, a chatbe, a repóba és a dolgozatba nem kerülnek):
   ```bash
   cp .env.example .env && chmod 600 .env
   sed -i "s|^POSTGRES_PASSWORD=.*|POSTGRES_PASSWORD=$(openssl rand -hex 24)|" .env
   sed -i '/^ConnectionStrings__Default=/d' .env
   sed -i "s|^Auth__InviteCode=.*|Auth__InviteCode=$(openssl rand -hex 10)|" .env
   sed -i "s|^KAMRA_DOMAIN=.*|KAMRA_DOMAIN=kamra-demo.austriaeast.cloudapp.azure.com|" .env
   sed -i "s|^Llm__Enabled=.*|Llm__Enabled=true|" .env
   read -r -p 'ACME e-mail-cím: ' ACME && sed -i "s|^ACME_EMAIL=.*|ACME_EMAIL=$ACME|" .env && unset ACME
   read -r -s -p 'Gemini-kulcs (élő): ' KEY && echo && sed -i "s|^Llm__ApiKey=.*|Llm__ApiKey=$KEY|" .env && unset KEY
   echo 'COMPOSE_FILE=docker-compose.yml:docker-compose.prod.yml' >> .env
   ```
   Az élő példány külön Gemini-kulcsot kap (ugyanazon az előre fizetett kereten), hogy a helyitől függetlenül visszavonható legyen. A meghívókódot (`grep '^Auth__InviteCode=' .env`) a jelszókezelőbe kell menteni. A `COMPOSE_FILE` miatt minden további `docker compose` parancs a prod felülíró fájllal együtt fut.
7. **Indítás:** `docker compose up -d --build` (MT-05: 3 p 12 mp, ebből a két image buildje kb. 169 mp; futás közben kb. 380 MiB RAM és 150 MiB swap).
8. **Ellenőrzés:** a VM-en `curl -sS https://kamra-demo.austriaeast.cloudapp.azure.com/health` → `Healthy`; a tanúsítvány: `docker compose logs caddy | grep -iE "certificate obtained|error"`. Kívülről (PowerShell): `curl.exe -s -D - -o NUL https://kamra-demo.austriaeast.cloudapp.azure.com/api/v1/auth/antiforgery` → `200`, `Strict-Transport-Security`, a süti `secure`; `curl.exe -sI http://kamra-demo.austriaeast.cloudapp.azure.com` → `308`; a 8080-as és az 5432-es port zárt (`Test-NetConnection … -Port 8080`).
9. **Tesztfiók** (a saját gépről, a repó gyökerében, Node 24):
   ```powershell
   $env:BASE_URL = "https://kamra-demo.austriaeast.cloudapp.azure.com"
   $env:DEMO_PASSWORD = Read-Host "tomi@pelda.hu jelszava" -MaskInput
   $env:INVITE_CODE = Read-Host "Meghívókód" -MaskInput
   node tests/perf/demo-seed.mjs
   Remove-Item Env:DEMO_PASSWORD, Env:INVITE_CODE, Env:BASE_URL
   ```
   Kilépési kód: 0 betöltve, 2 a fiók már létezik (előbb az adatbázis visszaállítása kell), 1 minden más hiba.

### Napi mentés

A `~/backup.sh` a konténer saját környezeti változóival ment (a jelszó nem kerül a crontabba), és 7 napig őrzi a mentéseket; a `~/backups` mappa `chmod 700` (a mentésben e-mail-címek és jelszó-hashek vannak):

```bash
#!/bin/sh
set -e
cd "$HOME/kamra"
docker compose exec -T db sh -c 'pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Fc' > "$HOME/backups/kamra-$(date +%F).dump"
find "$HOME/backups" -name 'kamra-*.dump' -mtime +7 -delete
```

Ütemezés (`crontab -e`, 03:30 UTC): `30 3 * * * $HOME/backup.sh >> $HOME/backups/backup.log 2>&1`. A visszaállítás a fenti „Mentés és visszaállítás” szerint, a fájl: `~/backups/kamra-<dátum>.dump`. Kipróbálva: MT-06 (az adatbázis-volume törlése után 1 mp alatt, adatvesztés nélkül).

### Frissítés és visszaállás

Mentés (`~/backup.sh`), `git fetch --tags && git checkout <új kiadás>`, `docker compose up -d --build`, ellenőrzés (8. lépés). Visszaállás: `git checkout <előző kiadás>` és `docker compose up -d --build`, a fenti „Visszaállás az előző verzióra” szerint. Az adatbázis-volume-ot csak név szerint szabad törölni (`docker volume rm kamra_pgdata`), a **`caddy_data`-t soha**: abban vannak a tanúsítványok, és a Let's Encrypt hetente legfeljebb 5 azonos tanúsítványt ad ki.

### Hibakeresés

- A tanúsítvány nem jön meg: `docker compose logs caddy`; a 80-as portnak kívülről elérhetőnek kell lennie (NSG). Ha a Let's Encrypt korlátba fut, a Caddy az `ACME_EMAIL` miatt a ZeroSSL-lel próbálkozik.
- Az SSH nem enged be: a 22-es NSG-szabály forrása a régi IP-cím (2. lépés).
- A meghívókód cseréje: új érték a `.env`-ben (`Auth__InviteCode`, legalább 16 karakter), utána `docker compose up -d` (az API újraindul, a meglévő fiókok megmaradnak).
- A regisztráció „Túl sok kérés” üzenetet ad: IP-címenként 10 kérés/perc; a proxy mögött az `X-Forwarded-For` alapján kliensenként számol (V-108).

### Leállítás (2027. jan. 22. után)

Az Azure Portalon az erőforráscsoport (`kamra-rg`) törlése: a VM, a lemez, a nyilvános IP, az NSG és a mentések együtt törlődnek. Az élő Gemini-kulcsot az AI Studióban vissza kell vonni.


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
