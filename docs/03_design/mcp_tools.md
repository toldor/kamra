# MCP Tools

Az MCP-szerver (`KamraApp.McpServer`) eszközei, amelyeket a chat asszisztens használ (US-6, [scope_contract.md](../01_product/scope_contract.md), [ADR-0010](../02_architecture/adr/0010-mcp-integracio.md)). A szerver az API folyamatában, kérésenként fut, memóriabeli streamen (`ModelContextProtocol.Core` 2.3.1); minden eszköz ugyanazt az Application-használati esetet hívja, mint a REST API, és a háztartást a bejelentkezett kérés hatóköréből kapja.

## Eszközök

| Eszköz | Leírás | Paraméter | Használati eset | Eredmény |
|---|---|---|---|---|
| `get_pantry_items` | A háztartás készlettételei, lejárat, majd magyar ábécé szerint | `category` (nem kötelező): az API kategóriakódjai (`dairy`, `cheese`, … – séma `enum`), a leírásban a felület magyar neveivel | `ListPantryItems` | `{ items: [{ name, amount, unit, category, expiryDate, expiryEstimated, expired, soonExpiring }], truncated }` |
| `get_expiring_items` | A ma, holnap vagy holnapután lejáró tételek | – | `ListPantryItems` („hamarosan lejáró” szűrő) | mint fent |
| `get_recommendations` | Az ajánlás a US-3 rangsorában | `max_results` (nem kötelező, 1–10, alapból 5): **csoportonként** legfeljebb ennyi recept | `GetRecommendations` | `{ pantryIsEmpty, makeable: [{ name, servings, soonExpiringIngredients, expiredIngredients }], almostMakeable: [{ …, shortages: [{ name, missing, available, required, unit }] }] }` |
| `get_shopping_list` | A bevásárlólista | – | `ListShoppingList` | `{ items: [{ name, amount?, unit?, checked }], truncated }` |

Az eredmény tömör JSON: azonosító, verzió és fiókadat nélkül; egységek és kategóriák az API kódjaival; listánként legfeljebb 100 sor (`truncated: true`, ha több).

## Biztonsági szabályok

- Minden eszköz csak olvas (`ReadOnly`), adatmódosítást nem végez és nem kezdeményez; író eszköz nincs, a nem létező eszköz hívása a kérdést 502-vel zárja.
- Az eszközök sémájában nincs háztartás- vagy felhasználó-azonosító; az ismeretlen argumentumot az SDK eldobja (ellenséges teszt).
- Nyers SQL-t vagy tetszőleges lekérdezést egyetlen eszköz sem enged.
- A bemenet JSON-sémával validált (kategória-`enum`, `max_results` 1–10); hibás argumentum → az eszköz elutasítja (MCP `isError`) → 502 `LLM_INVALID_RESPONSE`. Az eszköz belső hibája (például adatbázis) 500 `INTERNAL_ERROR`, naplózva.
- Az eszközök eredménye adat, nem utasítás (rendszerprompt `chat.v2`); a felhasználó által elnevezett hozzávaló JSON-szövegként jut a modellhez.

## Tesztek

Séma és annotáció: [KamraToolsTests.cs](../../tests/KamraApp.Unit.Tests/KamraToolsTests.cs); háztartás-elkülönítés, alak, korlátok: [ChatApiTests.cs](../../tests/KamraApp.Integration.Tests/ChatApiTests.cs); támadások: [AdversarialChatApiTests.cs](../../tests/KamraApp.Integration.Tests/AdversarialChatApiTests.cs).
