using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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
public sealed class KamraTools(ListPantryItems listPantryItems, GetRecommendations getRecommendations, ListShoppingList listShoppingList,
    ToolFailures failures)
{
    public const int MaxRows = 100;
    public const int DefaultRecipes = 5;
    public const int MaxRecipes = 10;

    [McpServerTool(Name = "get_pantry_items", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("A háztartás készlettételei (The household's pantry items), lejárat szerint. Optional category: " +
        "dairy = tejtermék, cheese = sajt, eggs = tojás, freshMeatFish = friss hús, baromfi, hal, " +
        "processedMeat = felvágott, húskészítmény, vegetables = zöldség, fruit = gyümölcs, bakery = pékáru, " +
        "dryGoods = száraz élelmiszer, cannedAndSauces = konzerv, befőtt, szósz, frozen = fagyasztott, beverages = ital, " +
        "preparedFood = készétel, maradék, other = egyéb.")]
    public async Task<PantryToolResult> GetPantryItems(
        [Description("Only this category (optional).")] Category? category = null,
        CancellationToken cancellationToken = default) =>
        await Guarded(async () => Pantry(await listPantryItems.ExecuteAsync(new ListPantryItemsQuery { Category = category?.ToString() },
            cancellationToken)));

    [McpServerTool(Name = "get_expiring_items", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("A ma, holnap vagy holnapután lejáró tételek (Items that expire today, tomorrow or the day after).")]
    public async Task<PantryToolResult> GetExpiringItems(CancellationToken cancellationToken = default) =>
        await Guarded(async () => Pantry(await listPantryItems.ExecuteAsync(new ListPantryItemsQuery { ExpiringSoon = true }, cancellationToken)));

    [McpServerTool(Name = "get_recommendations", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Az ajánlás: a készletből elkészíthető és majdnem elkészíthető receptek, elöl a hamarosan lejáró " +
        "tételeket felhasználókkal (Recipes that can be made or almost made from the pantry, ranked).")]
    [SuppressMessage("Naming", "CA1707", Justification = "The parameter name is the MCP argument name of mcp_tools.md.")]
    public async Task<RecommendationsToolResult> GetRecommendations(
        [Description("At most this many recipes per group, 1-10 (default 5)."), Range(1, MaxRecipes)] int? max_results = null,
        CancellationToken cancellationToken = default)
    {
        // A refused argument makes the call fail (MCP isError), which ends the question (ADR-0010 4.).
        var take = max_results ?? DefaultRecipes;
        ArgumentOutOfRangeException.ThrowIfLessThan(take, 1, nameof(max_results));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(take, MaxRecipes, nameof(max_results));

        return await Guarded(async () =>
        {
            var recommendations = await getRecommendations.ExecuteAsync(cancellationToken);
            return new RecommendationsToolResult(recommendations.PantryIsEmpty,
                [.. recommendations.Makeable.Take(take).Select(r => Recipe(r, withShortages: false))],
                [.. recommendations.AlmostMakeable.Take(take).Select(r => Recipe(r, withShortages: true))]);
        });
    }

    [McpServerTool(Name = "get_shopping_list", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("A bevásárlólista (The shopping list).")]
    public async Task<ShoppingListToolResult> GetShoppingList(CancellationToken cancellationToken = default) =>
        await Guarded(async () =>
        {
            var items = await listShoppingList.ExecuteAsync(cancellationToken);
            return new ShoppingListToolResult(
                [.. items.Take(MaxRows).Select(i => new ShoppingListToolItem(i.IngredientName, i.Amount?.Value, i.Amount?.Unit, i.Checked))],
                items.Count > MaxRows);
        });

    // T1 review J3: a refused argument stays the model's fault; any other failure of the use case (the database, a
    // bug) is recorded, so the chat reports it as the app's own error instead of blaming the model.
    private async Task<T> Guarded<T>(Func<Task<T>> body)
    {
        try
        {
            return await body();
        }
        catch (Exception failure) when (failure is not ArgumentException)
        {
            failures.Record(failure);
            throw;
        }
    }

    // The use case's order is kept: earliest expiry first, then the Hungarian alphabet (V-33).
    private static PantryToolResult Pantry(IReadOnlyList<PantryItemResponse> items) =>
        new([.. items.Take(MaxRows)
                .Select(i => new PantryToolItem(i.IngredientName, i.Amount, i.Unit, i.Category, i.ExpiryDate, i.ExpiryEstimated,
                    i.Expired, i.SoonExpiring))],
            items.Count > MaxRows);

    private static RecipeToolItem Recipe(RecommendationResponse recipe, bool withShortages) =>
        new(recipe.Name, recipe.Servings, recipe.SoonExpiringIngredients, recipe.ExpiredIngredients,
            withShortages ? [.. recipe.Shortages.Select(s => new ShortageToolItem(s.IngredientName, s.Missing, s.Available, s.Required, s.Unit))] : null);
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
