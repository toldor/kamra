# C4 – Component view (az API belseje)

Az API container ([c4_context_container.md](c4_context_container.md)) moduljai, a Clean Architecture rétegeivel ([ADR-0004](adr/0004-clean-architecture-retegek.md)). Egy modul = egy mappa minden rétegben (`Api/<Modul>`, `Application/<Modul>`, `Domain/<Modul>`), az adatelérés az Infrastructure `Persistence` mappájában.

```mermaid
flowchart TB
    spa["SPA<br/>[React]"]
    db[("PostgreSQL")]

    subgraph api["API container"]
        subgraph web["Api réteg – controllerek, auth, hibamodell"]
            cAuth["Auth"]
            cPantry["Pantry<br/>(készlet, hozzávalók, kategóriák)"]
            cRec["Recommendations"]
            cRecipes["Recipes"]
            cCook["Cookings"]
            cShop["ShoppingList"]
            err["ErrorHandling<br/>(ProblemDetails)"]
        end
        subgraph app["Application réteg – use case-ek, portok"]
            uAuth["Auth"]
            uPantry["Pantry, Ingredients"]
            uRec["Recommendations"]
            uRecipes["Recipes"]
            uCook["Cookings"]
            uShop["ShoppingList<br/>(SuggestionEvaluator)"]
        end
        subgraph dom["Domain réteg – entitások, szabályok"]
            dQty["Quantities"]
            dPantry["Pantry, Ingredients"]
            dRec["Recommendations<br/>(Recommender)"]
            dRecipes["Recipes"]
            dCook["Cookings<br/>(CookingCalculator, FEFO)"]
            dShop["ShoppingList<br/>(SuggestionRules)"]
        end
        subgraph infra["Infrastructure – a portok megvalósítása"]
            repo["Persistence<br/>(repositoryk, IngredientLocks,<br/>TransactionalSave, migrációk, seed)"]
            ident["Identity"]
        end
    end

    spa -- "JSON [/api/v1]" --> web
    cAuth --> uAuth
    cPantry --> uPantry
    cRec --> uRec
    cRecipes --> uRecipes
    cCook --> uCook
    cShop --> uShop
    uPantry --> dPantry
    uRec --> dRec
    uRecipes --> dRecipes
    uCook --> dCook
    uCook --> uShop
    uPantry --> uShop
    uShop --> dShop
    dPantry --> dQty
    dRecipes --> dPantry
    dCook --> dRec
    repo -. "megvalósítja a portokat" .-> app
    ident -. "IIdentityService" .-> uAuth
    repo --> db
    ident --> db
```

| Modul | Felelősség | Api | Application | Domain |
|---|---|---|---|---|
| Auth | Regisztráció, bejelentkezés, háztartás létrehozása | `Api/Auth` | `Application/Auth` | `Domain/Households` |
| Pantry | Készlettételek, mozgásnapló, becsült lejárat; hozzávalók és minimumszint | `Api/Pantry` | `Application/Pantry`, `Application/Ingredients` | `Domain/Pantry`, `Domain/Ingredients`, `Domain/Quantities` |
| Recommendations | „Mit főzzek?” | `Api/Recommendations` | `Application/Recommendations` | `Domain/Recommendations` |
| Recipes | Saját recept, archiválás | `Api/Recipes` | `Application/Recipes` | `Domain/Recipes` |
| Cookings | Előnézet, megerősítés, levonás | `Api/Cookings` | `Application/Cookings` | `Domain/Cookings` |
| ShoppingList | Lista, javaslatok, a javaslatok kiváltása | `Api/ShoppingList` | `Application/ShoppingList` | `Domain/ShoppingList` |

**Függőségi szabályok:**
- Befelé mutatnak: Api → Application → Domain; az Infrastructure az Application portjait valósítja meg. A Domain nem hivatkozik más rétegre, az Application nem hivatkozik az Infrastructure-re és az ASP.NET Core-ra; ezt a [LayerRulesTests.cs](../../tests/KamraApp.Unit.Tests/LayerRulesTests.cs) ellenőrzi.
- A modulok közti hívás az Application rétegben történik, és egy irányba: a készlet és a főzés változásai a ShoppingList `SuggestionEvaluator`-át hívják (egy tranzakcióban, ADR-0008), fordítva nem.
- Minden repository-port háztartáshoz kötött metódusa megkapja a háztartás azonosítóját (QA-1).
