# 0007 - REST API controllerekkel, /api/v1 prefixszel és kivétel-alapú ProblemDetails hibamodellel

Dátum: 2026-10-02
Státusz: Accepted

## Context

- **Probléma:** rögzíteni kell, milyen stílusban kommunikál a SPA a backenddel, hogyan verziózzuk a szerződést, és hogyan jut el egy hiba a használati esettől egységes formában a klienshez, úgy, hogy a felhasználó mindig magyar, teendőt tartalmazó üzenetet lásson, és technikai részlet soha ne szivárogjon ki.
- **Kényszerek:** egy kliens (React SPA), azonos originről kiszolgálva, cookie-alapú session és antiforgery token ([ADR-0006](0006-cookie-auth-identity.md)); a frontend és a backend együtt deployolódik. Az Api és az MCP-host ugyanazokat a használati eseteket hívja ([ADR-0004](0004-clean-architecture-retegek.md)). Kb. 20 végpont, kb. 200 óra. A v1.2 3.F fejezete base URL-t és verziózást, auth módot, hibakódokat és lehetőleg OpenAPI-leírást kér. Az S-1 és az S-2 státuszkódokra épülő mérőszámot rögzít ([quality_attributes.md](../quality_attributes.md)).
- **Érintett minőségi attribútumok:** QA-1 adatelkülönítés, QA-3 hibatűrés, QA-6 modularitás és tesztelhetőség; használhatóság (hibaüzenetek).

## Decision

- Az API REST + JSON stílusú, ASP.NET controllerekkel és globális antiforgery-szűrővel, fix `/api/v1` prefixszel; a használati esetek egy közös `AppException` hierarchiával jelzik a hibát, amelyet a beépített exception handler RFC 7807 ProblemDetails-re képez le stabil `code`-dal és biztonságos magyar `title`-lel, az OpenAPI-leírás pedig build közben generálódik, és a repóban verziózott.

## Alternatives

**API-stílus**

1) **GraphQL** – előny: a kliens egy kéréssel pontosan a szükséges mezőket kapja; hátrány: új függőség és séma-réteg, a hibák HTTP 200-on belül jönnek, így a 401/404/429 kategóriák és az S-2 mérőszáma körülményesebb, egy kliensnél túlméretezett.
2) **gRPC-Web** – előny: erősen típusos szerződés; hátrány: proxy vagy gRPC-Web réteg kell a böngészőhöz, a hibamodell nem ProblemDetails.

**Végpont-technológia**

3) **Minimal API `RequireAntiforgery()`-vel** (a vak trianguláció javaslata) – előny: tömörebb, kevesebb boilerplate; hátrány: az antiforgery middleware hibás tokennél nem utasítja el a kérést, csak beállít egy validációs feature-t és továbbengedi ([AntiforgeryMiddleware.cs](https://github.com/dotnet/aspnetcore/blob/main/src/Antiforgery/src/AntiforgeryMiddleware.cs)); az elutasítást a form-kötés végzi, JSON-végpontnál saját szűrő kellene ([Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0)).
4) **FastEndpoints könyvtár** – előny: tiszta REPR-szerkezet; hátrány: külső függőség saját validációs és DI-logikával, ami ütközik az [ADR-0004](0004-clean-architecture-retegek.md) Application-rétegbeli validációjával.

**Verziózás**

5) **`Asp.Versioning` könyvtár** – előny: teljes, több verziós kezelés; hátrány: új függőség egy olyan problémára, ami együtt deployolt egyetlen kliensnél nem jön elő.
6) **Header-alapú verziózás** – előny: az URL verziómentes; hátrány: böngészőből nehezebben kipróbálható, és a cache-eléshez `Vary` fejléc kell.
7) **Verziózás nélkül (`/api`)** – előny: a legegyszerűbb; hátrány: a v1.2 a verziózás leírását kötelezőként kéri.

**Hibajelzés**

8) **Result-típus a várt hibákra, kivétel csak a váratlanokra** (a vak trianguláció javaslata; saját `Result<T>`, OneOf vagy FluentResults) – előny: a hiba lehetősége a szignatúrából látszik, nincs vezérlés kivétellel; hátrány: leképező kód minden használati esetben, controllerben és MCP-toolban, két mechanizmus, amit konzisztensen kell alkalmazni; kb. 20 használati esetnél és két hostnál a plusz kód nagyobb költség, mint a haszon.
9) **Csak Result-típus** – előny: egységes; hátrány: a váratlan hibákhoz amúgy is kell kivételkezelés.

**Hibaüzenetek**

10) **Csak a backend adja az üzenetet** – előny: egy forrás; hátrány: a ux_flows.md UI-teendői (pl. kézi form gomb) nem fejezhetők ki.
11) **Csak a frontend adja az üzenetet a `code` alapján, validációnál mezőnkénti hibakódokkal** (a vak trianguláció javaslata) – előny: a mikrocopy egy helyen; hátrány: egy katalógusból hiányzó kód technikai szöveget mutatna, és mezőnkénti hibakódrendszer kellene.

**Ismételt tételjavaslat-jóváhagyás**

12) **Mindig 409** – előny: egyszerű; hátrány: dupla kattintásnál vagy újraküldésnél a felhasználó hibát lát, pedig a művelete sikerült.
13) **Mindig azonos 200** – előny: teljes idempotencia; hátrány: az ellentmondó műveletet (elvetett javaslat jóváhagyása) is sikernek mutatja.

**LLM-hibák**

14) **Minden LLM-hiba 400** – előny: egyszerű; hátrány: nem különül el a hibás bemenet a szolgáltató kiesésétől, az LLM-hibaarány nem mérhető tisztán.

**OpenAPI**

15) **Kézzel írt frontend-típusok** – előny: eggyel kevesebb függőség; hátrány: a frontend típusai csendben elavulhatnak.
16) **Böngészős API-felület (pl. Scalar)** – előny: kényelmes kézi kipróbálás; hátrány: plusz függőség, a kipróbálást a frontend és az integrációs tesztek lefedik.
17) **Típusos fetch-wrapper könyvtár (pl. openapi-fetch)** – előny: típusos hívások; hátrány: plusz függőség, a generált típusokat a saját `src/frontend/src/api/` kliens is használhatja.

## Consequences

- **Pozitív:**
  - A már elfogadott döntések (ProblemDetails, 401/404/429, S-2 mérőszám, antiforgery) átalakítás nélkül illeszkednek.
  - Az antiforgery egyetlen globális, keretrendszer által adott szűrő ([`AutoValidateAntiforgeryToken`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.autovalidateantiforgerytokenattribute?view=aspnetcore-8.0)), amely minden nem biztonságos metódusnál tokent követel, és végpontonként nem felejthető el.
  - A hibák leképezése egy helyen van; az Api és az MCP-host ugyanazt a kivételhierarchiát képezi le.
  - A felhasználó elé soha nem kerülhet technikai szöveg: a backend `title` mindig biztonságos magyar visszaesés.
  - Az API, az `openapi.json` és a frontend típusai nem csúszhatnak szét; ez a CI-ban látható contract-ellenőrzés.
  - Az LLM-kiesés a hibás bemenettől elkülönítve logolható és mérhető (S-1).
- **Negatív / kockázatok:**
  - Kivétel a várt esetekben is; a szignatúrából nem látszik, mit dobhat egy metódus, ezt a hibakód-lista és a kódonkénti negatív teszt ellensúlyozza.
  - Az üzenet két helyen élhet (backend `title`, frontend katalógus).
  - Három új függőség: `Microsoft.AspNetCore.OpenApi`, `Microsoft.Extensions.ApiDescription.Server` (NuGet, MIT) és `openapi-typescript` (npm dev, MIT).
  - Valódi párhuzamos API-verzióra nem számítunk; a `v1` a szerződés láthatóságát szolgálja.
- **Figyelni kell a megvalósítás során:**
  - A nem CRUD műveletek (főzés, tételjavaslat jóváhagyása/elvetése, bevásárlójavaslat elfogadása/elutasítása) `POST` műveleti végpontok; az útvonalnevek a glosszárium fogalmait követik, az [api.md](../../03_design/api.md) szerint.
  - A controllerek vékonyak: csak a használati esetet hívják, üzleti logika nélkül.
  - Az alap URL (`/api/v1`) csak a frontend közös API-kliensében (`src/frontend/src/api/`) szerepel.
  - Az `AppException` kategóriái: Validation (400), Unauthorized (401), Forbidden (403), NotFound (404), Conflict (409), RateLimited (429), BadGateway (502), Unavailable (503); minden más kivétel 500, a válaszban csak a `correlationId` szerepel, a részletek a logba kerülnek.
  - Mezőnkénti validációs hibánál a ProblemDetails `errors` mezője magyar üzeneteket tartalmaz (DataAnnotations `ErrorMessage`).
  - A frontend üzenetkatalógusa csak azokat a `code`-okat írja felül, amelyekhez a [ux_flows.md](../../01_product/ux_flows.md) külön szöveget vagy teendőt rendel; minden más kódnál a `title` jelenik meg.
  - Ismételt jóváhagyás: jóváhagyott javaslat újbóli jóváhagyása 200 ugyanazzal az eredménnyel; elvetett javaslat jóváhagyása vagy jóváhagyott elvetése 409. Az egyidejű állapotváltás atomikusságát a konkurenciakezelési ADR rögzíti.
  - LLM-hibák: időtúllépés vagy kiesés 503 `LLM_UNAVAILABLE`; sémahibás válasz 502 `LLM_INVALID_RESPONSE`; tétel nélküli szöveg 400 `ITEM_PROPOSAL_NO_ITEMS`.
  - Az `openapi.json` build közben generálódik (a .NET 10-ben az alapértelmezett formátum OpenAPI 3.1), és a CI elbukik, ha a generált fájl eltér a commitolttól; a frontend típusai `openapi-typescript`-tel generálódnak.

## Verification

- **Hogyan ellenőrizzük?**
  - CI: a generált `openapi.json` egyezik a commitolttal (`git diff --exit-code`); a frontend a generált típusokkal fordul.
  - Integrációs tesztek: minden hibakódhoz legalább egy negatív teszt, amely a státuszt és a `code`-ot ellenőrzi; váratlan hibánál 500, és a válasz nem tartalmaz stack trace-t vagy belső részletet; antiforgery token nélküli módosító kérés elutasítva; ismételt jóváhagyás 200 azonos eredménnyel, ellentmondó művelet 409; egy validációs teszt azt is ellenőrzi, hogy az `errors` a hibás mezőhöz nem üres, saját (nem keretrendszer-alapértelmezett) üzenetet ad.
  - S-1 teszt: LLM-kiesésnél 503 `LLM_UNAVAILABLE`, sémahibánál 502 `LLM_INVALID_RESPONSE`.
  - Tervezési validáció: [P-12](../../07_ai/prompt_log.md), vak trianguláció; az eltérések az Alternatives 3., 8. és 11. pontjában.
- **Evidence link:** a walking skeleton hibakezelő middleware-je és az 1. lépcső negatív tesztjei; a link az implementációval együtt kerül ide.
