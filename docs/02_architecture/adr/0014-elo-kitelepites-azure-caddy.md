# 0014 - Élő kitelepítés: Azure VM, ugyanaz a Docker Compose, Caddy TLS-proxy, meghívókódos regisztráció

Dátum: 2026-11-22
Státusz: Accepted

## Context

- A témavezető 2026-11-21-i levele szerint dec. 12-re az alkalmazásnak publikus, élő HTTPS-címen is kipróbálhatónak kell lennie a `tomi@pelda.hu` tesztfiókkal, a dec. 12–19-i témavezetői kipróbáláshoz. A fejlesztő döntése: a példány 2027. jan. 22-ig fut (a záróvizsga miatt).
- A kurzus-kiírás 3. pontja telepített, kipróbálható alkalmazást és ténylegesen végigpróbált telepítési útmutatót kér; a scope_contract ütemezése a dec. 4-ig tartó szakaszra „ellenőrzött telepítés másik gépen”-t ír. A korábbi telepítések (MT-02, MT-04) a fejlesztő gépén, helyben futottak; a [c4_context_container.md](../c4_context_container.md) „Tervezett célkörnyezet”-e: ugyanaz a Compose egy Linux VM-en, TLS-proxy konténerrel.
- Kényszerek a kódból: a munkamenet-süti `Secure` és `SameSite=Strict` ([ADR-0006](0006-cookie-auth-identity.md)), ezért interneten TLS-végződtetés kell; az API maga szolgálja ki a SPA-t (egy konténer); a bejelentkezés és a regisztráció rate limitje IP-címenként számol, egy proxy mögött ez közös keret lenne ([api.md](../../03_design/api.md)); a mondatos bevitel és a chat a fizetős Gemini-keretet használja (előre fizetett 5 dollár, havi korlát 5 dollár, [privacy_licensing.md](../../05_security_ops/privacy_licensing.md)).
- Erőforrás: az Azure for Students előfizetés (a GitHub Student Developer Packen át aktiválva) – engedélyezett régiók: denmarkeast, austriaeast, francecentral, swedencentral, switzerlandnorth; fizetős infrastruktúra nem elvárás (kurzus-kiírás 3. pont).

## Decision

- **Gép:** Azure Linux VM, **Austria East**, zóna nélkül (az 1-es zónában a B-méretek nem érhetők el), **Standard_B2ats_v2** (2 vCPU, a rendszerben 836 MiB RAM) **4 GB swappal**, Ubuntu Server 24.04 LTS; a diák-ajánlat ingyenes órakeretes mérete (listaár 7,88 USD/hó, a kredit fedezi). Ha a build nem fér el, ugyanabban a régióban ideiglenesen B2als_v2 (4 GB), a lemez és a DNS-név megmarad. Az MT-05-ben a build swappal lefutott.
- **Telepítés:** ugyanaz a Docker Compose, mint helyben, egy felülíró fájllal (`docker-compose.prod.yml`); a VM `.env`-jében `COMPOSE_FILE=docker-compose.yml:docker-compose.prod.yml`. Kézi telepítés a [deploy_runbook.md](../../05_security_ops/deploy_runbook.md) „Élő példány (Azure)” szakasza szerint, frissítés és visszaállás git tag alapján (mint az MT-04).
- **TLS:** **Caddy** (`caddy:2.11.9-alpine`, Apache-2.0) konténer a 80-as és a 443-as porton, automatikus Let's Encrypt-tanúsítvánnyal az Azure ingyenes DNS-nevére (`kamra-demo.austriaeast.cloudapp.azure.com`); az `ACME_EMAIL` megadásával a ZeroSSL a tartalék kiadó. HSTS 1 év, `includeSubDomains` és `preload` nélkül (a szülő domain nem a miénk). `trusted_proxies` nincs: a Caddy a kliens `X-Forwarded-*` fejléceit eldobja, és a kapcsolatból írja újra.
- **Kitettség:** az `api` portja a gépen nem publikált (`ports: !reset []`), a `db` csak `127.0.0.1`-en; az Azure NSG kívülről a 80-as és a 443-as portot engedi, a 22-est csak a fejlesztő IP-címéről; SSH csak kulccsal (ed25519, jelmondattal).
- **Továbbított fejlécek:** az API a `ForwardedHeaders:KnownNetworks` (a Compose-hálózat rögzített alhálózata, `172.30.0.0/24`) felől fogadja el az `X-Forwarded-For` és az `X-Forwarded-Proto` fejlécet, egy ugrással; beállítás nélkül, illetve érvényes hálózat nélkül a middleware ki van kapcsolva ([V-105](../../07_ai/verification_log.md)). Így a per-IP auth rate limit kliensenként számol, és az antiforgery-süti `Secure` lesz ([V-106](../../07_ai/verification_log.md)).
- **Regisztráció:** **statikus meghívókód** (`Auth:InviteCode`, legalább 16 karakter, a fejlesztő javaslata). Üresen nincs kód (helyben, CI-ben, e2e-ben a régi viselkedés); beállítva a `RegisterUser` a fiók létrehozása előtt SHA-256-hashek állandó idejű összehasonlításával ellenőrzi; a felület a `GET /api/v1/auth/registration` válaszából tudja, mutassa-e a „Meghívókód” mezőt. A kódot a témavezető levélben, az UAT-résztvevők személyesen kapják.
- **LLM:** az élő példányon is bekapcsolva, külön (élő szerveres) Gemini-kulccsal, ugyanazon az előre fizetett kereten, a meglévő háztartásonkénti korlátokkal.
- **Tesztfiók és mentés:** a `tomi@pelda.hu` fiókot a `tests/perf/demo-seed.mjs` tölti fel (jelszó és kód környezeti változóból); napi `pg_dump` cronnal, 7 napos megőrzéssel ugyanazon a VM-en.

## Alternatives

1) **GitHub Pages / Codespaces** – előny: a meglévő GitHub-fiókkal, ingyen; hátrány: a Pages csak statikus oldalt szolgál ki (a .NET API és a PostgreSQL nem fut rajta), a Codespace legfeljebb 4 óra tétlenség után leáll, a beérkező látogatás nem számít aktivitásnak.
2) **Heroku (a Student Pack 13 dolláros havi kreditje)** – előny: a TLS és a domain a platformé; hátrány: a Compose nem használható, a cookie-kulcsok fájlban vannak (`DataProtection__KeysPath`), a Heroku fájlrendszere nem tartós, így minden napi újraindítás kiléptetne mindenkit – kódváltozás és új függőség kellett volna a freeze után; a telepítés eltérne a dokumentálttól.
3) **Fizetős VPS (például Hetzner) saját domainnel** – előny: bőven elég erőforrás; hátrány: havi díj bankkártyával és domainregisztráció.
4) **nginx + certbot** – előny: elterjedt; hátrány: két komponens, kézi megújítás-ütemezés, több konfiguráció, mint a Caddy három sora.
5) **Nyitott regisztráció** (az eredeti javaslat) – előny: nincs kódváltozás; hátrány: bárki fiókot nyithat, és minden új háztartás saját LLM-keretet kap; a meghívókód ezt kis kódváltozással zárja ([P-36](../../07_ai/prompt_log.md)).
6) **Kapcsolóval zárható regisztráció, csak a tesztfiók** – előny: a legszűkebb; hátrány: az UAT-résztvevők és a témavezető nem hozhatnának saját fiókot.

## Consequences

- **Pozitív:** az élő példány ugyanazzal a Compose-zal és ugyanazokkal a lépésekkel indul, mint a helyi (a runbook mindkettőt fedi); valódi telefonról is be lehet lépni (az 1. UAT-kör korlátja megszűnt, [uat.md](../../04_quality/uat.md)); a rate limit és a sütik a proxy mögött is helyesen működnek; a telepítés „másik gépen” igazolt (MT-05, MT-06).
- **Negatív / kockázatok (elfogadva):**
  - A `GET /api/v1/auth/registration` elárulja, hogy a példány kér-e meghívókódot; a felületnek kell ez az információ, a kód tartalmáról semmit nem mond (Antigravity-review #5).
  - A meghívókód statikus és többször használható: aki megkapja, továbbadhatja; a hatás egy újabb háztartás az LLM-kerettel együtt, összesen legfeljebb 5 dollár (#15). Ha visszaélés történik, a kód a `.env`-ben cserélhető, és az API újraindul.
  - Egy túl tág `ForwardedHeaders:KnownNetworks` (például `0.0.0.0/0`) mindenkinek megengedné a cím hamisítását; az érték nem a `.env`-ből jön, hanem a commitolt `docker-compose.prod.yml`-ből, ezért nincs külön validáció (#10).
  - A `cloudapp.azure.com` nevekre a Let's Encrypt közös, alacsony korlátja vonatkozhat; tartalék a ZeroSSL, végső esetben saját domain CNAME-mel. A `caddy_data` volume nem törölhető (hetente legfeljebb 5 azonos tanúsítvány).
  - Egyetlen VM, egyetlen zóna, a mentés ugyanazon a gépen: ha a VM elvész, a bemutató adat a `demo-seed.mjs`-sel pótolható; ez bemutató példánynál elfogadott.
  - A `tomi@pelda.hu` jelszava a témavezetőnek írt levélben megy (a levél kéri); a fiókban csak bemutató adat van, a jelszó a kipróbálás után cserélhető.
  - Ha a fejlesztő otthoni IP-címe változik, az SSH-szabályt a Portalon frissíteni kell.
- **Figyelni kell:** a VM 2027. jan. 22. után törlendő (a runbook szerint); a Gemini-keret és az Azure-kredit fogyása; a Caddy-image verziója kézzel frissül.

## Verification

- Tesztek: [ForwardedHeadersTests.cs](../../../tests/KamraApp.Integration.Tests/ForwardedHeadersTests.cs) (külön keret a proxy mögött, hamisítás kívülről, konfig nélkül, `" , "`, loopback, IPv4-be ágyazott IPv6, `Secure` antiforgery-süti), [InviteCodeTests.cs](../../../tests/KamraApp.Integration.Tests/InviteCodeTests.cs), [AdversarialProxyAndInviteTests.cs](../../../tests/KamraApp.Integration.Tests/AdversarialProxyAndInviteTests.cs) (Antigravity), [StartupTests.cs](../../../tests/KamraApp.Integration.Tests/StartupTests.cs) (hibás hálózat, rövid kód), [AuthUseCaseTests.cs](../../../tests/KamraApp.Unit.Tests/AuthUseCaseTests.cs), a Vitest regisztrációs esetei ([App.test.tsx](../../../src/frontend/src/App.test.tsx)); mutációval ellenőrizve ([V-105](../../07_ai/verification_log.md) – [V-107](../../07_ai/verification_log.md)); zöld CI: [run 44963175210](https://github.com/toldor/kamra/actions/runs/44963175210) (`434cb4d`).
- Élő próba: [manual_tests.md](../../04_quality/manual_tests.md) MT-05 (telepítés a VM-re, mért idővel és memóriával, kívülről: HSTS, 308, `Secure` süti, zárt 8080 és 5432, böngésző laptopon és telefonon) és MT-06 (mentés, törlés, visszaállítás); a rate limit élőben, két hálózatról ([V-108](../../07_ai/verification_log.md)).
