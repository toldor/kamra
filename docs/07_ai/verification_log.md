# AI Ellenőrzési Napló

Ha az AI biztonsági, teljesítménybeli, helyességi vagy licencelési állítást tesz, ide kerül egy bejegyzés (lásd `AGENTS.md` 10. pont).

## Formátum

```
### V-XX – [állítás rövid leírása]
- **Dátum:** ÉÉÉÉ-HH-NN
- **Állítás:** ...
- **Kockázat:** Alacsony / Közepes / Magas
- **Ellenőrzési módszer:** ...
- **Eredmény:** ...
- **Következtetés:** ...
```

---

### V-01 – A termék megkülönböztető értéke a meglévő appokhoz képest
- **Dátum:** 2026-09-26
- **Állítás:** A vision.md értékajánlatához az AI (Claude Code) két megkülönböztető állítást javasolt: (1) a versenytársak fotóval, vonalkóddal, blokkszkenneléssel vagy tételenkénti listával dolgoznak, nálunk viszont egy szabad szöveges, magyar mondat több tételt, mennyiséget és lejáratot tartalmazhat; (2) a determinisztikus, indoklással ellátott receptajánlás a meglévő appokban nincs meg.
- **Kockázat:** Magas. A bíráló egy keresés után megcáfolhatja a termék fő pozicionálását, ez pedig az egész vision hitelességét rontja.
- **Ellenőrzési módszer:** Forrásellenőrzés: webkeresés és a versenytársak saját oldalainak, illetve összehasonlító cikkeinek átnézése (Samsung Food, KitchenPal, NoWaste.ai, SuperCook, Pantryfy, Recipy, Pantry Vault AI).
- **Eredmény:** **FAIL.** (1) A szabad szöveges, több tételes bevitel létezik: a [Pantry Vault AI](https://github.com/hardijain26/pantry_vault_ai) README-je szerint *„paste a shopping list and Gemini splits it into items with quantities and expiry dates”*, a [Pantryfy](https://www.pantryfy.ai/blog/best-pantry-inventory-apps) pedig szabad szöveges bevitelt ír. (2) Az átlátható ajánlásra és a magyar nyelvre vonatkozó állítás negatív állítás („más appban nincs”), amit nem lehet ellenőrizni. Mellékeredmény: a lejárat szerint rangsorolt receptajánlás és a főzés utáni levonás a [Samsung Food](https://support.samsungfood.com/hc/en-us/articles/30251599415956-How-to-Search-for-Recipes-Using-Your-Available-Ingredients)-ban is megvan, ezért ezt már korábban sem állítottuk egyedinek.
- **Következtetés:** A „meglévő alkalmazásokhoz képest” rész kikerült a [vision.md](../01_product/vision.md)-ből. Az értékajánlat csak a felhasználó jelenlegi helyzetéhez (státusz quo) mér, a G3 guardrail szövegét is ehhez igazítottuk. Tanulság: az AI által javasolt „csak nálunk van” típusú állítás alapból gyanús. Versenytárs-összehasonlítást csak funkciónkénti, forrásolt táblázatban teszünk, egyediséget nem állítunk.
