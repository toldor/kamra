using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using KamraApp.Application.Pantry;
using KamraApp.Application.Recommendations;
using KamraApp.Application.ShoppingList;
using KamraApp.Domain.Pantry;
using KamraApp.Domain.Quantities;
using ModelContextProtocol.Server;

namespace KamraApp.McpServer;

// US-6 (mcp_tools.md, us6_minispec K4-K5): the four read-only tools of the chat. Each calls the same use case as the
// REST API (AGENTS.md 3.); the household comes from the request scope, never from an argument (ADR-0010 2.).
// The results are compact: no ids, no versions, at most 100 rows.
[McpServerToolType]
public sealed class KamraTools(ListPantryItems listPantryItems, GetRecommendations getRecommendations, ListShoppingList listShoppingList)
{
    public const int MaxRows = 100;

    [McpServerTool(Name = "get_pantry_items", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("A háztartás készlettételei (The household's pantry items), lejárat szerint. Optional category: " +
        "dairy = tejtermék, cheese = sajt, eggs = tojás, freshMeatFish = friss hús, baromfi, hal, " +
        "processedMeat = felvágott, húskészítmény, vegetables = zöldség, fruit = gyümölcs, bakery = pékáru, " +
        "dryGoods = száraz élelmiszer, cannedAndSauces = konzerv, befőtt, szósz, frozen = fagyasztott, beverages = ital, " +
        "preparedFood = készétel, maradék, other = egyéb.")]
    public Task<PantryToolResult> GetPantryItems(
        [Description("Only this category (optional).")] Category? category = null,
        CancellationToken cancellationToken = default)
    {
        _ = listPantryItems; // red stage (spec-first): the T1 implementation uses it
        throw new NotImplementedException();
    }

    [McpServerTool(Name = "get_expiring_items", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("A ma, holnap vagy holnapután lejáró tételek (Items that expire today, tomorrow or the day after).")]
    public Task<PantryToolResult> GetExpiringItems(CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    [McpServerTool(Name = "get_recommendations", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Az ajánlás: a készletből elkészíthető és majdnem elkészíthető receptek, elöl a hamarosan lejáró " +
        "tételeket felhasználókkal (Recipes that can be made or almost made from the pantry, ranked).")]
    [SuppressMessage("Naming", "CA1707", Justification = "The parameter name is the MCP argument name of mcp_tools.md.")]
    public Task<RecommendationsToolResult> GetRecommendations(
        [Description("At most this many recipes per group, 1-10 (default 5).")] int? max_results = null,
        CancellationToken cancellationToken = default)
    {
        _ = getRecommendations; // red stage (spec-first): the T1 implementation uses it
        throw new NotImplementedException();
    }

    [McpServerTool(Name = "get_shopping_list", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("A bevásárlólista (The shopping list).")]
    public Task<ShoppingListToolResult> GetShoppingList(CancellationToken cancellationToken = default)
    {
        _ = listShoppingList; // red stage (spec-first): the T1 implementation uses it
        throw new NotImplementedException();
    }
}

public sealed record PantryToolItem(string Name, decimal Amount, Unit Unit, Category Category, DateOnly ExpiryDate, bool ExpiryEstimated,
    bool Expired, bool SoonExpiring);

public sealed record PantryToolResult(IReadOnlyList<PantryToolItem> Items, bool Truncated);

public sealed record ShortageToolItem(string Name, decimal Missing, decimal Available, decimal Required, Unit Unit);

public sealed record RecipeToolItem(string Name, int Servings, IReadOnlyList<string> SoonExpiringIngredients,
    IReadOnlyList<string> ExpiredIngredients, IReadOnlyList<ShortageToolItem>? Shortages);

public sealed record RecommendationsToolResult(bool PantryIsEmpty, IReadOnlyList<RecipeToolItem> Makeable,
    IReadOnlyList<RecipeToolItem> AlmostMakeable);

public sealed record ShoppingListToolItem(string Name, decimal? Amount, Unit? Unit, bool Checked);

public sealed record ShoppingListToolResult(IReadOnlyList<ShoppingListToolItem> Items, bool Truncated);
