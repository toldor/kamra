using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KamraApp.Infrastructure.Persistence;
using KamraApp.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KamraApp.Integration.Tests;

// US-3 through the API with the seeded recipes (ADR-0005) on a real PostgreSQL. P5 and P7 are the
// hand-calculated examples of test_report.md.
[Collection("api")]
public class RecommendationsApiTests(KamraApiFactory factory)
{
    private static DateOnly Today => KamraApp.Application.Common.Today.Of(TimeProvider.System);

    [Fact]
    public async Task The_migrations_seed_the_five_recipes_on_seeded_ingredients()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KamraDbContext>();

        var recipeKeys = await db.Database
            .SqlQuery<string>($"""SELECT "SeedKey" AS "Value" FROM "Recipes" WHERE "HouseholdId" IS NULL""")
            .ToListAsync(TestContext.Current.CancellationToken);
        var lines = await db.Database
            .SqlQuery<int>($"""SELECT COUNT(*)::int AS "Value" FROM "RecipeIngredients" ri JOIN "Recipes" r ON r."Id" = ri."RecipeId" WHERE r."HouseholdId" IS NULL""")
            .SingleAsync(TestContext.Current.CancellationToken);
        var linesOnOwnIngredients = await db.Database
            .SqlQuery<int>($"""
                SELECT COUNT(*)::int AS "Value" FROM "RecipeIngredients" ri
                JOIN "Recipes" r ON r."Id" = ri."RecipeId" JOIN "Ingredients" i ON i."Id" = ri."IngredientId"
                WHERE r."HouseholdId" IS NULL AND i."HouseholdId" IS NOT NULL
                """)
            .SingleAsync(TestContext.Current.CancellationToken);

        recipeKeys.Should().BeEquivalentTo(SeedData.Recipes.Recipes.Select(r => r.SeedKey));
        lines.Should().Be(SeedData.Recipes.Ingredients.Count);
        linesOnOwnIngredients.Should().Be(0, "every system recipe ingredient is on the system ingredient list (ADR-0003)");
    }

    [Fact]
    public async Task P7_the_recipe_using_soon_expiring_items_comes_first()
    {
        var client = await ApiClient.SignedInAsync(factory);
        await AddP7StockAsync(client);

        var result = await client.GetJsonAsync("/api/v1/recommendations");

        Names(result, "makeable").Should().Equal("Lecsó", "Paprikás krumpli");
        var lecso = result.GetProperty("makeable")[0];
        lecso.GetProperty("soonExpiringIngredients").EnumerateArray().Select(n => n.GetString()).Should().Equal("paradicsom", "paprika");
        result.GetProperty("makeable")[1].GetProperty("soonExpiringIngredients").GetArrayLength().Should().Be(0);
        Names(result, "almostMakeable").Should().Equal("Rántotta");
        Shortages(result.GetProperty("almostMakeable")[0]).Should().Equal("tojás: 3 hiányzik (0 van, 3 kell) db", "vaj: 20 hiányzik (0 van, 20 kell) g");
    }

    [Fact]
    public async Task P5_shows_the_missing_sour_cream_with_what_there_is_and_what_is_needed()
    {
        var client = await ApiClient.SignedInAsync(factory);
        await AddAsync(client, "száraztészta", 400, "g", 90);
        await AddAsync(client, "túró", 500, "g", 5);
        await AddAsync(client, "szalonna", 150, "g", 7);
        await AddAsync(client, "tejföl", 150, "g", 6);

        var result = await client.GetJsonAsync("/api/v1/recommendations");

        Names(result, "almostMakeable").Should().Contain("Túrós csusza");
        var csusza = result.GetProperty("almostMakeable").EnumerateArray().Single(r => r.GetProperty("name").GetString() == "Túrós csusza");
        Shortages(csusza).Should().Equal("tejföl: 50 hiányzik (150 van, 200 kell) g");
    }

    [Fact]
    public async Task Another_households_stock_is_never_used()
    {
        var owner = await ApiClient.SignedInAsync(factory);
        await AddP7StockAsync(owner);
        var other = await ApiClient.SignedInAsync(factory);

        var result = await other.GetJsonAsync("/api/v1/recommendations");

        result.GetProperty("pantryIsEmpty").GetBoolean().Should().BeTrue();
        result.GetProperty("makeable").GetArrayLength().Should().Be(0);
        result.GetProperty("almostMakeable").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Recommendations_require_sign_in()
    {
        var anonymous = await ApiClient.CreateAsync(factory);

        var response = await anonymous.Http.GetAsync("/api/v1/recommendations", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task AddP7StockAsync(ApiClient client)
    {
        await AddAsync(client, "paprika", 500, "g", 2);
        await AddAsync(client, "paradicsom", 300, "g", 1);
        await AddAsync(client, "vöröshagyma", 300, "g", 10);
        await AddAsync(client, "étolaj", 1, "l", 90);
        await AddAsync(client, "burgonya", 1, "kg", 30);
        await AddAsync(client, "kolbász", 150, "g", 7);
        await AddAsync(client, "őrölt pirospaprika", 100, "g", 90);
    }

    private static async Task AddAsync(ApiClient client, string ingredient, decimal amount, string unit, int daysToExpiry)
    {
        var ingredientId = (await client.GetJsonAsync($"/api/v1/ingredients?search={Uri.EscapeDataString(ingredient)}")).EnumerateArray()
            .Single(i => i.GetProperty("name").GetString() == ingredient).GetProperty("id").GetGuid();
        var response = await client.Http.PostAsJsonAsync("/api/v1/pantry-items",
            new { ingredientId, amount, unit, expiryDate = Today.AddDays(daysToExpiry) }, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static List<string?> Names(JsonElement result, string list) =>
        [.. result.GetProperty(list).EnumerateArray().Select(r => r.GetProperty("name").GetString())];

    // Numbers are compared by value: "3", "3.0" and "3.000" are the same amount, whatever the culture.
    private static List<string> Shortages(JsonElement recommendation) =>
        [.. recommendation.GetProperty("shortages").EnumerateArray().Select(s =>
            $"{s.GetProperty("ingredientName").GetString()}: {Number(s, "missing")} hiányzik " +
            $"({Number(s, "available")} van, {Number(s, "required")} kell) {s.GetProperty("unit").GetString()}")];

    private static string Number(JsonElement element, string property) =>
        element.GetProperty(property).GetDecimal().ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
}
