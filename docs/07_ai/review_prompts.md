# Review- és ellenőrző promptok

A kód reviewjához és ellenőrzéséhez használt promptsablonok, verziózva, hogy a review megismételhető legyen (v1.2 5.3.9). A `<…>` helyekre a szakasz adatai kerülnek. A használat a [prompt_log.md](prompt_log.md) bejegyzéseiben, az ellenőrzött állítások a [verification_log.md](verification_log.md)-ben szerepelnek.

Alapelv: aki a kódot írta, nem az reviewzza ([ai_manifest.md](ai_manifest.md) „Munkamódszer”). Az implementáló Claude Code, a független reviewer és ellenséges tesztelő az Antigravity (Gemini), a PR második reviewere a GitHub Copilot, a döntés a fejlesztőé.

## 1. Szakasz-review (Antigravity, a repo megnyitva)

```text
Szereped: független kódreviewer egy BSc szakdolgozati projektben. NEM te írtad a kódot.

Tiltások:
- Ne módosíts és ne hozz létre fájlt, csak olvass és válaszolj.
- Ne „engedélyezz” és ne javasolj új függőséget; ha szerinted kellene egy, jelezd döntést igénylő pontként.
- Ha egy állításod keretrendszer-verziótól függ (.NET 10, EF Core 10, React 19, PostgreSQL 18), írd mellé, hogy a tudásod elavult lehet, és adj forrást, ha tudsz.

Kontextus (olvasd el először):
- AGENTS.md (kötelező szabályok), CONTEXT.md (fogalmak)
- docs/02_architecture/adr/: <érintett ADR-ek> (az 1. lépcsőben: 0002, 0003, 0004, 0005, 0007, 0008)
- A szakasz célja: <szakasz és lépései, 2–3 mondat>

Review tárgya: `git diff <előző szakasz utolsó commitja>..HEAD` a <branch> branchen. Csak a commitolt állapotot nézd, a munkakönyvtár változásait ne.

Szempontok:
1. Rétegszabályok és ADR-megfelelés (a fenti ADR-ek): eltér-e a kód az elfogadott döntésektől?
2. Biztonság: auth-megkerülés, hiányzó validáció, adatszivárgás a hibaválaszban vagy a logban, CSRF, XSS.
3. Edge case-ek: üres vagy null bemenet, túl hosszú érték, párhuzamos kérés, hiányzó konfiguráció.
4. Hibakezelés: minden hibaút a ProblemDetails modellen megy-e át, stabil code-dal.
5. Tesztek: mi hiányzik? Van-e olyan teszt, amely zöld, de nem azt ellenőrzi, amit a neve állít?
6. Felesleges bonyolultság: absztrakció egyetlen megvalósítással, nem használt kód.
7. Mennyiségek és adatelkülönítés: alapegységben számol-e (ADR-0002), helyes-e a kerekítés és a FEFO, minden mennyiségváltozás a készletmozgás-naplóval egy tranzakcióban történik-e, és minden lekérdezés szűr-e a HouseholdId-re.

Kimenet: táblázat, oszlopok: # | Megállapítás | Fájl:sor | Szabály vagy forrás (idézet az AGENTS.md-ből, ADR-ből vagy hivatalos doksiból) | Súlyosság (Kritikus/Közepes/Alacsony) | Javaslat.
Ha nincs megállapításod egy szempontnál, írd ki, hogy „nincs”. Ne dicsérj, ne foglalj össze.
```

## 2. Ellenséges teszt (Antigravity, kritikus moduloknál)

```text
Szereped: ellenséges tesztelő. A célod, hogy ELTÖRD ennek a projektnek a(z) <modul> részét. NEM te írtad a kódot.
Ne módosíts fájlt: a teszteket szövegként add vissza.

Olvasd el: AGENTS.md, <érintett ADR-ek>, <érintett forrásfájlok>,
tests/KamraApp.Integration.Tests (a meglévő tesztek mintája és fixture-je).

Írj legfeljebb 8 xUnit integrációs tesztet a meglévő fixture-rel (WebApplicationFactory + Testcontainers), FluentAssertions 7 szintaxissal, ezekre:
<célzott támadások listája; az auth-modulnál:
- hamisított, lejárt, más felhasználótól származó vagy csonkolt cookie;
- antiforgery-token: hiányzik, más sessionből származik, vagy GET-tel próbálkozik módosító művelet;
- határértékek: 14, 15, 128 és 129 karakteres jelszó, unicode és whitespace, nagyon hosszú e-mail, e-mail kis- és nagybetűs változata;
- felsorolás: eltér-e a válaszidő vagy a válasz törzse ismeretlen e-mail és rossz jelszó esetén;
- lockout és rate limit kijátszása (például párhuzamos kérések a zárolási küszöbnél);
- hibaválaszban szivárgás: stack trace, belső útvonal, SQL- vagy Identity-üzenet.>

Minden teszthez írd le egy mondatban, milyen támadást modellez, és mit vársz a helyes viselkedéstől (státusz és code).
```

Utána az implementálónak (Claude Code):

```text
Itt a Gemini ellenséges tesztjei: [BEILLESZTVE]. Tedd be őket, futtasd le.
Mindegyikről: (1) érvényes-e a teszt (lehet, hogy a teszt a hibás); (2) ha érvényes és elbukik: diagnózis + minimális javítás jóváhagyás után;
(3) ha érvénytelen: miért, és ne vedd fel. A teszteket ne gyengítsd. A végén javasolj V-bejegyzést.
```

## 3. PR-review (GitHub Copilot)

A PR „Reviewers” mezőjében a Copilot hozzáadása; külön prompt nincs. A megállapításokat a 5. sablon szerint kell szétválogatni.

## 4. Megértés-ellenőrzés (Claude Code, szakasz végén)

```text
Kérdezz ki a(z) <szakasz> szakaszban írt kódról: 5 kérdés, egyenként, mindig várd meg a válaszom.
Legalább egy kérdés legyen arról, mi történik hibás bemenetnél vagy támadásnál, és egy arról, miért ezt a megoldást választottuk az ADR alternatívái helyett.
Javíts ki, ha tévedek. A végén mondd meg, mit nézzek át.
```

## 5. Review szétválogatása (Claude Code, minden review után)

```text
Itt a <Gemini/Copilot> reviewja: [BEILLESZTVE]. Válogasd szét: valós / részben valós / már benne van / téves / döntést igényel.
Minden tényállítást ellenőrizz forrással (Microsoft Learn, a csomag forráskódja, NuGet/npm oldal). Javítani csak a jóváhagyásom után javíts.
A reviewer nem engedélyezhet függőséget. Javasolj V-bejegyzést arra, ami ellenőrzött állítás volt.
```
