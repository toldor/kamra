# 0009 - LLM-szolgáltató: Google Gemini API (`gemini-3.1-flash-lite`), közvetlen REST-hívással és kötött JSON-sémával

Dátum: 2026-11-08
Státusz: Accepted

## Context

- A US-2 ([scope_contract.md](../../01_product/scope_contract.md)) egy magyar bevásárlási mondatból szerkeszthető tételjavaslatokat kér; jóváhagyás nélkül semmi nem kerül a készletbe. A „Külső API” korlátai: egy szolgáltató az Application rétegben definiált interfész mögött, cserélhetően; a CI-ban és a tesztekben nincs valódi hívás; az LLM csak a beírt szöveget és a válaszhoz szükséges adatot kapja, fiókadatot soha; havi keret legfeljebb 10 000 Ft; próbálkozásonként 15 mp, egy újrapróbálással.
- Kényszerek: új NuGet-csomag csak jóváhagyással ([AGENTS.md](../../../AGENTS.md) 2.); a fejlesztő egyetemi hallgató, kártyás előfizetés nélkül indulna; határidő nov. 13.; a felhasználók az EGT-ben vannak.
- Érintett minőségi attribútumok: hibatűrés ([QA-3](../quality_attributes.md), S-1), teljesítmény (QA-5: p95 ≤ 10 mp; [metrics.md](../../01_product/metrics.md) G3), biztonság (prompt injection, [threat_model.md](../../05_security_ops/threat_model.md)), adatvédelem ([privacy_licensing.md](../../05_security_ops/privacy_licensing.md)).

## Decision

- A tételjavaslatokat a **Google Gemini API `gemini-3.1-flash-lite`** modellje készíti, `generateContent` REST-hívással (`HttpClient`, SDK nélkül), kötött JSON-sémával (`generationConfig.responseFormat.text`: `mimeType: APPLICATION_JSON` és `schema`, [API-referencia](https://aistudio.google.com/api-reference/generate-content), 2026-11-08) és `thinkingLevel: minimal` beállítással, a verziózott `quick-entry.v3` prompttal. Az LLM csak kinyer: a felhasználó szövegét, a hozzávalót a háztartás listájáról (vagy „-”, ha nincs rajta), a mennyiséget, az egység szavát és a lejárat kifejezését szó szerint; a dátumot és az egységet a Domain számolja ki determinisztikusan. A funkciót az `Llm:Enabled` kapcsoló kapcsolja (alapból kikapcsolva); valódi felhasználó előtt a fizetős keret kötelező.

## Alternatives

1) **`gemini-3.6-flash`** – előny: a válaszolt hívásokon hibátlan volt (v2, 16 hívás); hátrány: 20 hívásból 3-ra 503 („high demand”), p95 17,2 mp, háromszoros ár ($0.75 / $3.75), az ingyenes keret napi 20 kérés (mérve, a dokumentációban nem szerepel).
2) **Azure OpenAI (Azure for Students)** – előny: $100 kredit kártya nélkül, EU-s adathely („Data Zone”), ami adatvédelmi szempontból erősebb (a vak trianguláció ezt döntést igénylő pontként jelölte); hátrány: diákelőfizetésen a modelltelepítés több 2026-os bejelentés szerint „Quota insufficient” hibával elakad, a próba a határidő miatt elmaradt; nem mért.
3) **Anthropic Claude Haiku 5.5** – előny: a legalacsonyabb tokenár ($0.10 / $0.50), kötött kimenet; hátrány: előre feltöltött egyenleg kell, nem mért. (A reviewer szerint a magyarországi elérhetőség bizonytalan – ez téves, Magyarország támogatott ország.) Érintettség: a javaslatot író Claude Code Anthropic-modell.
4) **OpenAI (például gpt-5-mini)** – előny: kötött kimenet, széles körben használt; hátrány: kártya kell, a hivatalos árlista nem volt elérhető (csak harmadik feles forrás), nem mért.
5) **Helyben futó modell (Ollama)** – előny: nincs API-költség, az adat nem hagyja el a gépet; hátrány: hardverfüggő válaszidő, a magyar pontosság nem mért, és a Docker Compose-ba építése a határidőig kockázatos (a reviewer is ezt jelezte).
6) **Hivatalos SDK a REST helyett** – előny: kevesebb kézi kérésépítés; hátrány: új NuGet-függőség, a REST-alak a mérésben működött.
7) **Az LLM számolja ki a lejárati dátumot** – előny: kevesebb kód; hátrány: a US-2 pontos dátumszabályait (ma / holnap / holnapután / a hét napja) nem determinisztikus kód teljesítené, és nem lehetne unit teszttel rögzíteni.
8) **Szabad JSON utólagos validációval, kötött séma nélkül** – előny: szolgáltatófüggetlenebb kérés; hátrány: a modell bármilyen szerkezetet visszaadhat, a hozzávaló nincs a listára korlátozva; a kötött séma mellett a mérésben 0 érvénytelen JSON volt.

## Consequences

- **Pozitív:** olcsó (mért átlag kb. 580 bemenő és 110 kimenő token hívásonként, kb. 0,03 cent); a medián válaszidő kb. 1 mp; a prompt injection kísérletet minden futásban kivédte (A10, v1–v3, 9/9); a dátum- és egységszabály determinisztikus és unit tesztelhető; a szolgáltató az interfész mögött cserélhető.
- **Negatív / kockázatok:**
  - **Válaszidő-ingadozás:** nov. 7-én 19:20 és 19:28 között a p95 44 mp volt (30 hívásból 6 15 mp felett), húsz perccel később (19:37–19:46) 2 mp alatt; a Flash-Lite 150 hívásából 2 lógott 60 mp-ig. A 15 mp-es korláttal és egy újrapróbálással ilyenkor a felhasználó hibaüzenetet kap, a szöveg megmarad, a kézi form elérhető (QA-3).
  - **Az EGT-szabály:** a Gemini API feltételei szerint EGT-beli felhasználóknak elérhetővé tett alkalmazás csak a fizetős szolgáltatást használhatja → valódi felhasználó (UAT 2. kör) előtt fizetős keret kell (legalább $5 előfizetés, havi költéskorláttal). A fizetős szolgáltatásnál a Google a promptokat nem használja a termékei fejlesztésére, adatfeldolgozási kiegészítés (DPA) szerint kezeli, és csak visszaélés-felderítésre naplózza korlátozott ideig.
  - **A pontosság egy része hangolás utáni szám:** a v3-at mind a 20 mondaton hangoltuk (A 97,9%, B 100%); a torzítatlan szám a v2 a B készleten (**94,2%**).
  - **Csak egy prompt injection mondatot mértünk;** a védelem többrétegű: a prompt az üzenetet adatként kezeli; a kimenet kötött séma, a hozzávaló csak a háztartás listájáról jöhet; a mennyiség a kézi form szabályán megy át (a nem pozitív érték üres lesz), a fel nem ismert lejárat-szöveg legfeljebb 100 karakter; az eredmény csak javaslat, ami jóváhagyás nélkül nem ír adatot, a jóváhagyás pedig a kézi felvétel validációján megy át. Ellenséges tesztek: [AdversarialItemProposalApiTests.cs](../../../tests/KamraApp.Integration.Tests/AdversarialItemProposalApiTests.cs).
  - **Hibák leképezése:** időtúllépés, hálózati hiba, 5xx és 429 → egy újrapróbálás, utána 503 `LLM_UNAVAILABLE`; a Gemini által elutasított kérés (400, 401, 403: kulcs-, kvóta- vagy kéréshiba) újra is elbukna, ezért újrapróbálás nélkül szintén 503 – a felhasználónak a „próbáld újra később, vagy rögzítsd tételenként” a helyes üzenet, a 502-es „fogalmazd át” félrevezetne; a sémán kívüli válasz → 502 `LLM_INVALID_RESPONSE`, újrapróbálás nélkül ([error_handling.md](../../03_design/error_handling.md)).
  - Az ingyenes keretre nincs SLA; a modellnév idővel kivezethető.
- **Figyelni kell a megvalósítás során:** a modell neve és a prompt verziója konfiguráció / verziózott fájl, és a naplóba kerül (a szöveg nem); a „-” „nincs illesztve” lesz; az egységszavak („kiló”, „deka”, „darab”) leképezése a Domainben; a dimenzióhoz nem illő egység (például „3 db paprika”) jóváhagyás előtt javítandó.

## Verification

- **PoC-mérés** ([QuickEntryPocTests.cs](../../../tests/KamraApp.Integration.Tests/LiveAI/QuickEntryPocTests.cs), `LiveAI`, csak kézzel): 20 magyar mondat, 36 várt tétel, tételenként 4 mező, mondatonként 3 futás. Eredmények (mezőhelyesség): A készlet v1 67,7% → v2 93,8%; **B készlet v2 94,2%** (torzítatlan); v3 A 97,9%, B 100%; a gemini-3.6-flash v2 84,4% (3 HTTP 503). Részletes táblázat: [test_report.md](../../04_quality/test_report.md) „LLM-mérés” ([V-66](../../07_ai/verification_log.md) – V-70).
- **Tervezési validáció:** [P-26](../../07_ai/prompt_log.md), vak trianguláció (Antigravity, Gemini – az érintettséget jelezte); egyezés: a modell, a REST, a kötött séma, a `minimal` szint, a „-” jelölés; eltérés nincs; téves állítások: az Anthropic magyarországi elérhetősége, a „1500 kérés/nap” (az a keresés-alapú válaszok korlátja); döntést igénylő: EU-s adathely (Alternatives 2), a fizetési mód (Consequences).
- **Megvalósítási evidence:**
  - az adapter unit tesztjei hamis `HttpMessageHandler`-rel ([GeminiIngredientParserTests.cs](../../../tests/KamraApp.Unit.Tests/GeminiIngredientParserTests.cs)): 15 mp, egy újrapróbálás időtúllépésre, kiesésre és hálózati hibára, elutasított kérés újrapróbálás nélkül, sémán kívüli válasz, 20 tételnél több, a napló tartalma;
  - a dátum- és egységszabály unit tesztjei ([ItemProposalTests.cs](../../../tests/KamraApp.Unit.Tests/ItemProposalTests.cs));
  - az API integrációs tesztjei ([ItemProposalApiTests.cs](../../../tests/KamraApp.Integration.Tests/ItemProposalApiTests.cs)): kikapcsolt kapcsoló, 503 és 502, idempotens és egyidejű jóváhagyás, idegen háztartás, rate limit;
  - e2e az LLM-csonkkal ([quick-entry.spec.ts](../../../tests/e2e/quick-entry.spec.ts), [llm-stub.mjs](../../../tests/e2e/llm-stub.mjs)): siker, kiesés, hibás válasz;
  - zöld CI (`19cddfc`): [42500324628](https://github.com/toldor/kamra/actions/runs/42500324628).
