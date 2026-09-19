# MCP Tools

Az MCP szerver (`KamraApp.McpServer`) eszközei, amelyeket a chat asszisztens használ.
Minden eszköznek van leírása, input sémája és tesztje (lásd `AGENTS.md` 5. pont).
Az eszközök kizárólag az Application rétegen keresztül végeznek műveleteket.

## Eszközök

### `get_pantry_items`

**Leírás:** Visszaadja a kamra aktuális tartalmát, opcionálisan szűrve kategóriára vagy lejárat szerint.

| Paraméter | Típus | Kötelező | Leírás |
|---|---|---|---|
| category | string | nem | Szűrés kategóriára |
| expiring_within_days | int | nem | Csak a megadott napon belül lejáró tételek |

**Visszatérési érték:** `PantryItem[]`
**Állapot:** Nem kész

---

### `get_recipe_suggestions`

**Leírás:** Receptjavaslatokat ad a jelenlegi készlet alapján.

| Paraméter | Típus | Kötelező | Leírás |
|---|---|---|---|
| max_results | int | nem | Maximális javaslatok száma (alapértelmezett: 5) |

**Visszatérési érték:** `RecipeSuggestion[]`
**Állapot:** Nem kész

---

### `get_shopping_list`

**Leírás:** Visszaadja az aktuális bevásárlólistát.

**Paraméter:** –
**Visszatérési érték:** `ShoppingListItem[]`
**Állapot:** Nem kész

---

## Biztonsági szabályok

- Nyers SQL-t vagy tetszőleges lekérdezést egyetlen eszköz sem enged.
- Írási műveletek kizárólag validált use case-eken keresztül.
- Minden eszköz bemenete JSON-sémával validált, mielőtt az Application rétegre kerül.

## Ismert hiányosságok

- Egyetlen eszköz sincs még implementálva.
