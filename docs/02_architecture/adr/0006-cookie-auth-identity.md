# 0006 - Cookie-alapú authentikáció ASP.NET Core Identity-vel, azonos originről kiszolgált SPA-val

Dátum: 2026-10-02
Státusz: Accepted

## Context

- **Probléma:** minden adatvégpont csak bejelentkezett felhasználónak érhető el, és csak a saját háztartása adatait látja ([QA-1, S-2](../quality_attributes.md)). Az [ADR-0004](0004-clean-architecture-retegek.md) szerint a használati esetek a háztartás azonosítóját az `ICurrentHousehold` interfészből veszik; ezt valahonnan hitelesen fel kell tölteni, a chat és az MCP útvonalán is.
- **Kényszerek:** e-mail + jelszó alapú regisztráció és bejelentkezés; egy fiók = egy háztartás; jelszó-visszaállítás és e-mail-megerősítés hatókörön kívül ([scope_contract.md](../../01_product/scope_contract.md)). Egy kliens van (React SPA), harmadik fél kliens, mobilapp vagy SSO nincs. A biztonságkritikus kódot lehetőleg a keretrendszer adja, ne saját (AI-generált) implementáció. Tiszta gépen 15 perces indítás (QA-7). Kb. 200 óra.
- **Érintett minőségi attribútumok:** QA-1 adatelkülönítés (security), QA-7 telepíthetőség, használhatóság (a G2 megtartási metrika).

## Decision

- A felhasználót ASP.NET Core Identity-vel kezelt, `HttpOnly`, `Secure`, `SameSite=Strict` session-cookie azonosítja; az Identity az Application rétegben definiált port mögött van, a SPA-t az Api szolgálja ki azonos originről, a módosító kéréseket antiforgery token védi, a háztartás azonosítója a cookie claimjéből jön, és soha nem az LLM kimenetéből vagy tool-argumentumból.

## Alternatives

**Mechanizmus**

1) **JWT access + refresh token, a SPA tárolja** – előny: állapotmentes, több kliensnél előnyös; hátrány: egy kliensünk van; a böngészős tokentárolás vagy XSS-kockázatos (localStorage), vagy úgyis cookie; a refresh-rotáció, a visszavonás és a lejárat biztonságkritikus saját kód.
2) **Külső identity provider (pl. Keycloak, Auth0)** – előny: MFA, SSO; hátrány: plusz konténer vagy külső szolgáltatás, a 15 perces indítást és az időkeretet terheli, a scope-hoz túlméretezett.
3) **`MapIdentityApi` beépített végpontjai** – előny: kész regisztrációs és bejelentkezési végpontok; hátrány: saját végpontokat (jelszó-visszaállítással) és bearer tokent is ad, és a végpontok megkerülik az Application réteget.
4) **Csak cookie middleware és az Identity jelszó-hashelője, saját felhasználókezeléssel** (a vak trianguláció javaslata) – előny: lazább kötés az Identityhez; hátrány: a lockoutot, az e-mail-egyediséget és a security stampet saját kóddal kellene megírni.

**Origin**

5) **Reverse proxy konténer (nginx / Caddy)** – előny: külön frontend- és backend-image, HTTPS-terminálás; hátrány: plusz konténer és konfiguráció.
6) **Külön origin CORS-szal** – előny: rugalmas; hátrány: `SameSite=None` cookie és hitelesítő adatokkal működő CORS kell, ami a CSRF-kockázatot növeli.

**CSRF**

7) **Csak `SameSite=Strict`** – előny: nincs kód; hátrány: az OWASP szerint kiegészítő védelem, azonos site-on belüli támadás ellen nem véd.
8) **Kötelező saját fejléc** – előny: olcsó; hátrány: saját middleware, gyengébb, mint a keretrendszer beépített mechanizmusa.

**Jelszó, brute-force és session**

9) **Az Identity alapértelmezett jelszószabálya (6 karakter + összetételi szabályok)** – előny: nincs konfiguráció; hátrány: ellentmond a NIST SP 800-63B-4-nek.
10) **Argon2id hashelés külső csomaggal** – előny: erősebb algoritmus; hátrány: új függőség, a beépített PBKDF2 a követelményeknek megfelel.
11) **Csak IP-alapú rate limit, fiókzárolás nélkül** (a vak trianguláció javaslata) – előny: a zárolás nem használható legitim felhasználók kizárására (DoS); hátrány: elosztott, sok IP-ről érkező próbálkozás ellen nincs fiókszintű védelem.
12) **Böngésző-session cookie 8 órás lejárattal** – előny: kisebb kockázat megosztott eszközön; hátrány: mobilon gyakori újra-bejelentkezés, ami a megtartást rontja.
13) **„Emlékezz rám” választás** – előny: a felhasználó dönt; hátrány: új UX-elem és tesztág.

**Regisztráció**

14) **Általános hibaüzenet foglalt e-mailnél** – előny: nehezebb a fiók-felderítés; hátrány: a felhasználó nem tudja, mit tegyen, ami ellentmond az AGENTS.md hibaüzenet-szabályának.
15) **Lusta háztartás-létrehozás az első kérésnél** – előny: egyszerűbb regisztráció; hátrány: köztes állapot és ellenőrzés minden kérésnél.

## Consequences

- **Pozitív:**
  - A jelszó-hashelés, a lockout, a cookie-titkosítás és az antiforgery a keretrendszerből jön, nem saját kódból.
  - A regisztráció és a bejelentkezés Application használati eset; az Identity (`UserManager`, `SignInManager`) az Application-ben definiált port mögött, az Infrastructure-ben van.
  - A session-token JavaScriptből nem érhető el (`HttpOnly`), azonos originről nincs CORS-konfiguráció.
  - A háztartás azonosítója a szerver által titkosított és aláírt cookie claimjéből jön, kérésenkénti lekérdezés nélkül; az MCP-toolok input sémájában nincs háztartás-azonosító, így prompt injection nem tud másik háztartást célozni.
  - A bejelentkezés és a regisztráció rate limitje a v1.2 „Rate limiting / abuse prevention” bónuszelemét is teljesíti.
- **Negatív / kockázatok (maradó kockázat):**
  - Egy ellopott cookie a lejáratig (legfeljebb 14 nap csúszó lejárattal), illetve a felhasználó következő kijelentkezéséig érvényes. *(Módosítva 2026-10-03, a walking skeleton S3 ellenséges tesztje után: a kijelentkezés új security stampet ad, és a cookie-t minden kérésnél ellenőrizzük, így a kijelentkezés minden eszközön érvényteleníti a sessiont; ára kérésenként egy felhasználó-lekérdezés – [V-11](../../07_ai/verification_log.md).)*
  - Aki ismeri egy felhasználó e-mail-címét, a fiókzárolással legfeljebb 5 percre, ismételt próbálkozással tartósabban is kizárhatja (DoS).
  - A regisztráció válaszából kideríthető, hogy egy e-mail-cím regisztrált-e; e-mail-megerősítés nélkül ez nem rejthető el, a rate limit csak lassítja.
  - A jelszó nincs feketelistával összevetve, amit a NIST SP 800-63B-4 előír.
  - A frontend és a backend egy image-ben deployolódik.
- **Figyelni kell a megvalósítás során:**
  - Új függőség: `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (MIT); a `MapIdentityApi` nem használható.
  - Jelszó: legalább 15, legfeljebb 128 karakter, összetételi szabályok nélkül ([NIST SP 800-63B-4](https://pages.nist.gov/800-63-4/sp800-63b.html)); a jelszómezőbe a beillesztés engedélyezett (jelszókezelők); `RequireUniqueEmail = true`.
  - Lockout bekapcsolva: 5 sikertelen próbálkozás után 5 perc; a jelszó-ellenőrzés folyamaton belül sorba rendezett, mert az Identity hibaszámlálója párhuzamos kérésnél növeléseket veszíthet ([V-10](../../07_ai/verification_log.md)) ([LockoutOptions](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.lockoutoptions?view=aspnetcore-10.0)); a bejelentkezésnél a lockoutot külön engedélyezni kell.
  - A beépített rate limiter IP-címenként a bejelentkezés és a regisztráció végpontján, 429 válasszal.
  - Rossz e-mailre és rossz jelszóra egységes hibaüzenet.
  - Session: 14 nap, csúszó lejárat ([cookie authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0)); a kijelentkezés a cookie-t törli.
  - A Data Protection kulcsok (a cookie és az antiforgery token titkosítása) Docker volume-on vannak (`PersistKeysToFileSystem`), különben minden újraindítás, deploy és rollback kijelentkezteti a felhasználókat.
  - Regisztráció: a fiók és a háztartás egy tranzakcióban jön létre, utána automatikus bejelentkezés; a háztartás Domain-entitás, amely a felhasználó azonosítójára hivatkozik, a Domain nem függ az Identitytől.
  - Az Identity-táblák ugyanabban az adatbázisban és migrációkban vannak ([ADR-0005](0005-postgresql-ef-core-migraciok.md)).
  - XSS: `dangerouslySetInnerHTML` tilos; az LLM-válaszok mindig sima szövegként jelennek meg; az Api `Content-Security-Policy: default-src 'self'` fejlécet küld.
  - A SPA a Vite-buildből az Api `wwwroot`-jában van; fejlesztésben a Vite dev server proxyzza az `/api` kéréseket.
  - Az Api és a külön MCP-host közötti háztartás-kontextus átadása az MCP-integráció ADR-jének döntése (jelöltek: belső hálózaton küldött fejléc vagy rövid életű, aláírt belső token).

## Verification

- **Hogyan ellenőrizzük?** Integrációs tesztek valódi PostgreSQL-lel:
  - cookie nélkül minden adatvégpont 401;
  - hamisított vagy hibás cookie esetén 401, nem 500 és nem stack trace;
  - más háztartás erőforrása 404 (S-2);
  - antiforgery token nélküli módosító kérés elutasítva;
  - rossz jelszóra és ismeretlen e-mailre azonos válasz;
  - 5 sikertelen bejelentkezés után zárolt fiók;
  - a rate limit túllépésére 429;
  - 15 karakternél rövidebb jelszó elutasítva;
  - a regisztráció fiókot és háztartást is létrehoz;
  - foglalt e-mail elutasítva.
  - A deploy runbook újraindítási és rollback-próbája: az api konténer újraindítása után a bejelentkezett session érvényes marad (Data Protection kulcsok).
  - Tervezési validáció: [P-12](../../07_ai/prompt_log.md), vak trianguláció; az eltérések az Alternatives 4. és 11. pontjában, a reviewer elavult jelszóhossz-állítása a [V-06](../../07_ai/verification_log.md)-ban.
- **Evidence link:** a walking skeleton auth-tesztjei; a link az implementációval együtt kerül ide.
