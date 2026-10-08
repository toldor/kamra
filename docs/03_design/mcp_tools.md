# MCP Tools

Az MCP szerver (`KamraApp.McpServer`) eszközei, amelyeket a chat asszisztens használ (US-6, [scope_contract.md](../01_product/scope_contract.md)).
Minden eszköznek van leírása, input sémája és tesztje (lásd `AGENTS.md` 5. pont).
Az eszközök kizárólag az Application rétegen keresztül, ugyanazokat a use case-eket hívják, mint a REST API.

## Eszközök

### `get_pantry_items`

**Leírás:** Visszaadja a háztartás aktuális készlettételeit, opcionálisan kategóriára szűrve.

| Paraméter | Típus | Kötelező | Leírás |
|---|---|---|---|
| category | string | nem | Szűrés kategóriára |

**Visszatérési érték:** `PantryItem[]`
**Állapot:** Nem kész

---

### `get_expiring_items`

**Leírás:** Visszaadja a hamarosan lejáró készlettételeket (lejárat ma, holnap vagy holnapután, lásd [CONTEXT.md](../../CONTEXT.md)).

**Paraméter:** –
**Visszatérési érték:** `PantryItem[]`
**Állapot:** Nem kész

---

### `get_recommendations`

**Leírás:** Visszaadja az Ajánlást: az elkészíthető és a majdnem elkészíthető recepteket a jelenlegi készlet alapján, ugyanazzal a determinisztikus rangsorral és indoklással, mint a felületen (US-3).

| Paraméter | Típus | Kötelező | Leírás |
|---|---|---|---|
| max_results | int | nem | Legfeljebb ennyi recept (alapértelmezett: 5) |

**Visszatérési érték:** `Recommendation[]`
**Állapot:** Nem kész

---

### `get_shopping_list`

**Leírás:** Visszaadja az aktuális bevásárlólistát.

**Paraméter:** –
**Visszatérési érték:** `ShoppingListItem[]`
**Állapot:** Nem kész

---

## Biztonsági szabályok

- Minden eszköz csak olvas: adatmódosítást egyik sem végez és nem is kezdeményez (a scope_contract szerint hatókörön kívül).
- Az eszközök csak a bejelentkezett felhasználó háztartásának adatait érik el.
- Nyers SQL-t vagy tetszőleges lekérdezést egyetlen eszköz sem enged.
- Minden eszköz bemenete JSON-sémával validált, mielőtt az Application rétegre kerül.

## Ismert hiányosságok

- Egyetlen eszköz sincs még implementálva.
