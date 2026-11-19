# Post-mortem: a kijelentkezés után is érvényes süti (2026-10-03)

**Összefoglaló:** a kijelentkezés csak az adott böngészőből törölte a bejelentkezési sütit. Egy ellopott süti ezért a felhasználó kijelentkezése után is hozzáfért a háztartás adataihoz, legfeljebb 14 napig. A hibát egy ellenséges teszt találta meg a walking skeleton auth-szakaszában, kiadás előtt; valódi felhasználót nem érintett.

| | |
|---|---|
| **Hogyan derült ki** | A független reviewer (Antigravity, Gemini 3.1 Pro) ellenséges tesztje a regisztrációkor kapott sütit a kijelentkezés után egy másik kliensről küldte vissza: a `/auth/me` 200-at adott, nem 401-et ([V-11](../07_ai/verification_log.md)). Ugyanezt a modult a kódreview és a saját biztonsági review hibátlannak találta ([V-10](../07_ai/verification_log.md)). |
| **Ok** | A süti titkosított, önálló jegy: a szerver a kérésnél csak az aláírást és a lejáratot ellenőrizte, a felhasználó állapotát nem. A kijelentkezés a jegyet nem tudta visszavonni. Az [ADR-0006](../02_architecture/adr/0006-cookie-auth-identity.md) ezt elfogadott maradó kockázatként rögzítette, a valódi hatás felmérése nélkül. |
| **Hatás** | Közepes: a fiók átvétele egy ellopott sütivel, a kijelentkezés után is; a süti `HttpOnly`, `Secure` és `SameSite=Strict`, ezért a megszerzése nehéz, de nem kizárt (például egy fertőzött gépről). |
| **Javítás** | A kijelentkezés új biztonsági bélyeget ad a felhasználónak (`UpdateSecurityStampAsync`), és az API minden kérésnél összeveti a süti bélyegét az adatbázisbelivel (`SecurityStampValidatorOptions.ValidationInterval = 0`). Így a kijelentkezés minden eszközön érvényteleníti a munkamenetet. Ára kérésenként egy felhasználó-lekérdezés; a [performance.md](performance.md) szerint ez a párhuzamos terhelésnél a valószínű szűk keresztmetszet egyik eleme, a cél mellett is bőven belefér. |
| **Megelőző teszt** | `Reusing_a_cookie_after_logout_is_rejected` ([AdversarialAuthTests.cs](../../tests/KamraApp.Integration.Tests/AdversarialAuthTests.cs)): a régi süti a kijelentkezés után 401-et kap. A teszt a javítás előtt piros volt, azóta minden CI-futásban zöld. |
| **Döntés** | A fejlesztő felülírta az ADR-ben elfogadott kockázatot ([ai_manifest.md](../07_ai/ai_manifest.md) D-3); az ADR-0006 Consequences szakasza dátummal kiegészült. |

## Tanulság és bevezetett gyakorlat

- **Az üres review nem bizonyíték.** Kritikus modulnál (auth, készlet, főzés) a review mellé támadó szemléletű, futtatható teszt kell; ez azóta a szakaszonkénti review-kapu része ([ai_manifest.md](../07_ai/ai_manifest.md) „Munkamódszer”).
- **Az elfogadott kockázatot is mérni kell.** Egy ADR-ben „maradó kockázatként” rögzített viselkedést egy teszttel érdemes megmutatni, mielőtt elfogadjuk: itt a teszt mutatta meg, hogy a javítás ára kicsi.
