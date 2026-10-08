# 0008 - Konkurenciakezelés és idempotencia: optimista zárolás xmin-nel, kérésazonosító és adatbázis-kényszerek

Dátum: 2026-10-08
Státusz: Accepted

## Context

- **Probléma:** a készletet ugyanaz a felhasználó több böngészőfülből is módosíthatja, a kérések pedig ismétlődhetnek (dupla kattintás, újraküldés, frissítés). A témavezető (2026. okt.) és a scope_contract szerint egy ismételt főzési kérés nem vonhat le kétszer, két fülből indított műveletnél nem veszhet el változás, és a rendszer nem könyvelhet el mást, mint amit a felhasználó megerősített (US-1 utolsó pont, US-2, US-4, US-5).
- **Kényszerek:** egy API-példány, egy PostgreSQL 18-adatbázis, EF Core 10 Npgsql-providerrel ([ADR-0005](0005-postgresql-ef-core-migraciok.md)); az Application nem függ az EF-től ([ADR-0004](0004-clean-architecture-retegek.md)); ismételt sikeres művelet → 200 azonos eredménnyel, ellentmondó művelet → 409 ([ADR-0007](0007-rest-api-hibamodell.md)); minden mennyiségváltozás egy tranzakcióban kerül a készlettételre és a készletmozgás-naplóba ([data_model.md](../../03_design/data_model.md)); kb. 200 óra, a mechanizmusnak integrációs teszttel bizonyíthatónak kell lennie.
- **Érintett minőségi attribútumok:** QA-4 adatintegritás, QA-2 determinizmus, QA-6 tesztelhetőség ([quality_attributes.md](../quality_attributes.md)).

## Decision

- A készlettételek minden módosítása (szerkesztés, csökkentés, törlés és a főzés levonásai) optimista zárolással fut a PostgreSQL `xmin` oszlopán; a főzés idempotenciáját a kliens által küldött kérésazonosító egyedi kényszere, az állapotváltásokat feltételes frissítés, a bevásárlólista egyediségét adatbázis-kényszer és upsert biztosítja, READ COMMITTED izolációs szinten.

Részletek:

1. **Készlettétel (abszolút módosítás):** a verzió az `xmin` (`uint` property, `IsRowVersion()`); a lekérdezés visszaadja, a módosító kérés törzsében kötelező `version` mezőként érkezik vissza. Eltérésnél a mentés nem fut le: 409 `PANTRY_ITEM_MODIFIED`, a kliens újratölti a tételt.
2. **Főzés:** egy tranzakcióban a szerver beolvassa az érintett hozzávalók készlettételeit, a megerősített felhasznált mennyiségeket FEFO szerint felosztja, és ha valamelyikből nincs elég, 409 `INSUFFICIENT_STOCK` a friss mennyiségekkel (nincs csendes levágás). A mentést az `xmin` védi: ha közben bármelyik érintett tétel változott, 409 `PANTRY_ITEM_MODIFIED`, a felület újraszámol és újra megerősítést kér (US-4). A kliens hozzávalónként erősít meg felhasznált mennyiséget, nem készlettételt: a felosztás a megerősítés pillanatának friss készletén a FEFO-szabály szerint történik, a válasz a tényleges felosztást mutatja, ezért a kérés nem tartalmazza a tételek verzióját (a versenyhelyzetet a szerveroldali olvasás és az `xmin` kezeli).
3. **Főzés idempotenciája:** a `requestId` (uuid) a kérés törzsében érkezik; a `Cooking` tábla (`HouseholdId`, `RequestId`) egyedi. Ha a kérésazonosító már létezik: azonos recept és adagszám esetén 200 a tárolt adatokból újraépített eredménnyel (az első kérés 201), eltérő tartalomnál 409 `IDEMPOTENCY_CONFLICT`. **Bármely mentési hiba (egyedikulcs-sértés vagy verzióütközés) után a szerver először a kérésazonosítót keresi**, mert egyidejű duplikátumnál a második kérés a verzióütközésbe is belefuthat előbb; csak ha nincs ilyen főzés, akkor adja vissza az eredeti hibát.
4. **Állapotváltások:** a tételjavaslat (US-2, `Pending`) és a bevásárlójavaslat (US-5, `Open`) jóváhagyása vagy elutasítása feltételes frissítéssel (`ExecuteUpdateAsync`, `WHERE State = 'Pending'`, illetve `WHERE State = 'Open'`), ugyanabban a tranzakcióban a következményével (új készlettételek, illetve a listára vétel upserttel); 0 érintett sornál újraolvasás: azonos célállapot → 200 azonos eredmény, ellentétes → 409 `ITEM_PROPOSAL_STATE_CONFLICT`, illetve `SHOPPING_SUGGESTION_STATE_CONFLICT`.
5. **Bevásárlólista:** (`HouseholdId`, `IngredientId`) egyedi, a felvétel `INSERT … ON CONFLICT DO UPDATE` (mennyiség-összeadás) paraméterezett SQL-lel; a bevásárlójavaslatnál részleges egyedi index (`Open`, vagy `Rejected` és `ClearedAt` null) és `ON CONFLICT DO NOTHING`.

## Alternatives

1) **Saját `Version` oszlop (`Guid`, `[ConcurrencyCheck]`)** (a vak trianguláció javaslata) – előny: az adatbázis belső mechanizmusától független; hátrány: minden módosításnál kézzel kell léptetni, és ugyanakkor változik, mint az `xmin`. A reviewer érve, hogy az `xmin` háttérműveletekre is változik, a PostgreSQL 9.4 óta nem áll: a fagyasztás az eredeti `xmin`-t megőrzi ([V-19](../../07_ai/verification_log.md)).
2) **Pesszimista zárolás a főzésnél (`SELECT … FOR UPDATE`, Id szerinti sorrendben)** (a saját eredeti javaslatom) – előny: nincs felesleges 409, ha egy érintett tétel nem mennyiségi mezője változik; hátrány: nyers SQL és explicit tranzakció, holtpont-kockázat rossz zárolási sorrendnél, és egy második mechanizmus az optimista zárolás mellett. Háztartási forgalomnál a felesleges 409 ritka, és a felület ezt az utat úgyis kezeli.
3) **Serializable izolációs szint újrapróbálással** – előny: az adatbázis garantálja a sorosíthatóságot; hátrány: sorosítási hibáknál újrapróbálási logika kell, és a viselkedés nehezebben tesztelhető, mint a kifejezett verzióellenőrzés.
4) **Csak feltételes `UPDATE … WHERE Quantity >= @delta` a főzésnél** – előny: nincs verzió; hátrány: az olvasáskor számolt FEFO-felosztás elavulhat, és az abszolút szerkesztést nem védi.
5) **`Idempotency-Key` fejléc tárolt válasszal** – előny: az IETF-tervezet szerinti forma, gyors ismétlés; hátrány: külön tábla és válasz-szerializálás; a főzés eredménye a tárolt adatokból újraépíthető, a generált OpenAPI-típusokhoz pedig a törzsmező egyszerűbb.
6) **Csak kliensoldali gombtiltás / utolsó mentés nyer** – előny: nincs szerveroldali kód; hátrány: a hálózati újraküldést és a két fület nem védi, a változás csendben elveszhet.
7) **A főzéskérés a látott készlettételek verzióját is elküldi** (a kapu-review javaslata) – előny: a kliens által látott állapothoz köt; hátrány: a közben felvett új készlettételt nem fogja meg, egy nem mennyiségi mező szerkesztésére feleslegesen 409-et ad, és a felhasználó amúgy is hozzávalónként, nem csomagonként erősít meg; az elveszett módosítást a szerveroldali olvasás és az `xmin` már kizárja ([V-21](../../07_ai/verification_log.md)).

## Consequences

- **Pozitív:** egyetlen konkurenciamechanizmus a készlettételeken, nyers SQL csak az upsertnél; a „nem vész el változás”, a „nem negatív készlet” és a „nem von le kétszer” integrációs teszttel bizonyítható; az ADR-0007 200/409 szabálya változatlanul érvényes.
- **Negatív / kockázatok:** a főzés felesleges 409-et kaphat, ha közben egy érintett tétel lejáratát vagy kategóriáját szerkesztették; ha a kliens nem küldi a `version`-t, a védelem kiesik, ezért az kötelező mező; a paraméterezett upsert nem cserélhető string-összefűzésre (SQL-injekció).
- **Figyelni kell a megvalósítás során:** a `DbUpdateConcurrencyException` és az egyedikulcs-sértés (23505) leképezése `AppException`-re az Infrastructure rétegben történik, az Application nem látja az EF-et; az Application a 409-es ágat a QA-4 szerint tesztelve kezeli.

## Verification

- **Hogyan ellenőrizzük?** Integrációs tesztek (Testcontainers, valódi PostgreSQL, párhuzamos kérésekkel):
  - elavult `version`-nel küldött szerkesztés → 409 `PANTRY_ITEM_MODIFIED`, a tétel változatlan (a „másik fül” közvetlen SQL-módosítással szimulálva);
  - két egyidejű főzés, amelyek együtt meghaladják a készletet → az egyik 201, a másik 409; a készlet nem negatív, a napló-invariáns teljesül;
  - ugyanaz a `requestId` kétszer, egymás után és egyszerre → egy főzés, egy levonás, azonos választörzs; eltérő tartalommal → 409 `IDEMPOTENCY_CONFLICT`;
  - egyidejű jóváhagyás és elvetés → az egyik 200, a másik 409 (tételjavaslat a US-2-vel, bevásárlójavaslat a US-5-tel);
  - két fülről egyszerre felvett azonos hozzávaló → egy listasor, összeadott mennyiség.
  - Tervezési validáció: P-14 (Gemini), vak trianguláció; egyezés: 3–5. pont; eltérés: a verzió forrása (Alternatives 1, V-19) és a főzés zárolása (Alternatives 2, a reviewer javaslata elfogadva). Kapu-review (P-14): a bevásárlójavaslat állapotváltása pótolva (4. pont); a tételverziók küldése elutasítva (Alternatives 7, V-21).
- **Evidence link:** az 1. lépcső (US-1, US-4, US-5) és a 2. lépcső (US-2) integrációs tesztjei; a link az implementációval együtt kerül ide.
