using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KamraApp.Application.Common;
using KamraApp.Domain.Cookings;
using KamraApp.Infrastructure.Persistence;
using KamraApp.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KamraApp.Integration.Tests;

// US-4 through the API on a real PostgreSQL with the seeded recipes: the ADR-0008 Verification list
// (idempotency, concurrency, never negative stock, household isolation) and the hand-calculated examples
// P2, P4, P6, P8, P9 and P10 of test_report.md.
[Collection("api")]
public class CookingApiTests(KamraApiFactory factory)
{
    private static DateOnly Today => KamraApp.Application.Common.Today.Of(TimeProvider.System);

    private static Guid RecipeId(string name) => SeedData.Recipes.Recipes.Single(r => r.Name == name).Id;

    [Fact]
    public async Task P2_cooking_deducts_the_earliest_expiring_items_first_and_logs_each_deduction()
    {
        var client = await ApiClient.SignedInAsync(factory);
        await AddLecsoStockAsync(client);
        var preview = await PreviewAsync(client, "Lecsó", 4);

        var response = await client.PostAsync("/api/v1/cookings", Body(Guid.CreateVersion7(), "Lecsó", 4, Used(preview)));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var cooking = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Strings(cooking, "depletedIngredients").Should().Equal("paprika");
        (await PantryAsync(client)).Should().BeEquivalentTo([("paradicsom", 300m), ("vöröshagyma", 150m), ("étolaj", 970m)]);
        var movements = await CookingMovementsAsync(cooking.GetProperty("cookingId").GetGuid());
        movements.Should().BeEquivalentTo([("paradicsom", -200m), ("paradicsom", -100m), ("paprika", -500m), ("vöröshagyma", -150m), ("étolaj", -30m)]);
        (await QuantitiesMatchTheLogAsync(client)).Should().BeTrue();
    }

    [Fact]
    public async Task P2_the_preview_shows_which_items_the_deduction_comes_from()
    {
        var client = await ApiClient.SignedInAsync(factory);
        await AddLecsoStockAsync(client);

        var preview = await PreviewAsync(client, "Lecsó", 4);

        var tomato = Line(preview, "paradicsom");
        tomato.GetProperty("allocations").EnumerateArray()
            .Select(a => (a.GetProperty("expiryDate").GetString(), a.GetProperty("amount").GetDecimal(), a.GetProperty("expired").GetBoolean()))
            .Should().Equal((Iso(1), 200m, false), (Iso(5), 100m, false));
        Strings(preview, "staples").Should().Equal("só");
    }

    [Fact]
    public async Task P4_the_preview_scales_six_servings_of_pancakes_in_the_recipe_units()
    {
        var client = await ApiClient.SignedInAsync(factory);

        var preview = await PreviewAsync(client, "Palacsinta", 6);

        preview.GetProperty("servings").GetInt32().Should().Be(6);
        preview.GetProperty("recipeServings").GetInt32().Should().Be(4);
        preview.GetProperty("instructions").GetString().Should().NotBeNullOrWhiteSpace();
        Strings(preview, "staples").Should().Equal("só", "víz");
        preview.GetProperty("lines").EnumerateArray()
            .Select(l => (l.GetProperty("ingredientName").GetString(), l.GetProperty("required").GetDecimal(), l.GetProperty("unit").GetString(),
                l.GetProperty("available").GetDecimal(), l.GetProperty("shortage").GetDecimal()))
            .Should().Equal(("étolaj", 45m, "ml", 0m, 45m), ("finomliszt", 300m, "g", 0m, 300m), ("tej", 7.5m, "dl", 0m, 7.5m), ("tojás", 3m, "db", 0m, 3m));
    }

    [Fact]
    public async Task P6_only_the_used_sour_cream_leaves_the_pantry_and_the_rest_is_a_shortage()
    {
        var client = await ApiClient.SignedInAsync(factory);
        await AddAsync(client, "száraztészta", 400, "g", 90);
        await AddAsync(client, "túró", 500, "g", 5);
        await AddAsync(client, "szalonna", 150, "g", 7);
        await AddAsync(client, "tejföl", 150, "g", 6);
        var preview = await PreviewAsync(client, "Túrós csusza", 4);
        var sourCream = Line(preview, "tejföl");
        (sourCream.GetProperty("required").GetDecimal(), sourCream.GetProperty("available").GetDecimal(),
            sourCream.GetProperty("defaultUsed").GetDecimal(), sourCream.GetProperty("shortage").GetDecimal()).Should().Be((200m, 150m, 150m, 50m));

        var response = await client.PostAsync("/api/v1/cookings", Body(Guid.CreateVersion7(), "Túrós csusza", 4, Used(preview)));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var cooking = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var line = Line(cooking, "tejföl");
        (line.GetProperty("used").GetDecimal(), line.GetProperty("shortage").GetDecimal()).Should().Be((150m, 50m));
        Strings(cooking, "depletedIngredients").Should().Contain("tejföl");
        (await PantryAsync(client)).Should().NotContain(i => i.Name == "tejföl");
        (await CookingMovementsAsync(cooking.GetProperty("cookingId").GetGuid())).Should().Contain(("tejföl", -150m));
    }

    [Fact]
    public async Task P8_a_repeated_request_gets_the_same_answer_and_deducts_once()
    {
        var client = await ApiClient.SignedInAsync(factory);
        await AddLecsoStockAsync(client);
        var body = Body(Guid.CreateVersion7(), "Lecsó", 4, Used(await PreviewAsync(client, "Lecsó", 4)));

        var first = await client.PostAsync("/api/v1/cookings", body);
        var second = await client.PostAsync("/api/v1/cookings", body);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        (await RawAsync(second)).Should().Be(await RawAsync(first));
        (await PantryAsync(client)).Should().Contain(("paradicsom", 300m));
    }

    [Fact]
    public async Task The_same_request_sent_twice_at_once_deducts_once_and_both_get_the_same_answer()
    {
        var client = await ApiClient.SignedInAsync(factory);
        await AddAsync(client, "paradicsom", 300, "g", 3);
        var body = Body(Guid.CreateVersion7(), "Lecsó", 4, Used(await PreviewAsync(client, "Lecsó", 4)));

        var responses = await Task.WhenAll(client.PostAsync("/api/v1/cookings", body), client.PostAsync("/api/v1/cookings", body));

        responses.Select(r => r.StatusCode).Should().BeEquivalentTo([HttpStatusCode.Created, HttpStatusCode.OK]);
        (await RawAsync(responses[0])).Should().Be(await RawAsync(responses[1]));
        (await PantryAsync(client)).Should().NotContain(i => i.Name == "paradicsom");
        (await QuantitiesMatchTheLogAsync(client)).Should().BeTrue();
    }

    [Theory]
    [InlineData("servings")]
    [InlineData("amount")]
    [InlineData("recipe")]
    public async Task The_same_request_id_with_other_content_is_refused(string change)
    {
        var client = await ApiClient.SignedInAsync(factory);
        await AddLecsoStockAsync(client);
        var requestId = Guid.CreateVersion7();
        var used = Used(await PreviewAsync(client, "Lecsó", 4));
        (await client.PostAsync("/api/v1/cookings", Body(requestId, "Lecsó", 4, used))).EnsureSuccessStatusCode();
        var pantry = await PantryAsync(client);

        var other = change switch
        {
            "servings" => Body(requestId, "Lecsó", 5, used),
            "amount" => Body(requestId, "Lecsó", 4, [.. used.Select(u => u with { Amount = 0 })]),
            _ => Body(requestId, "Palacsinta", 4, Used(await PreviewAsync(client, "Palacsinta", 4))),
        };
        var response = await client.PostAsync("/api/v1/cookings", other);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ApiClient.ProblemAsync(response)).GetProperty("code").GetString().Should().Be("IDEMPOTENCY_CONFLICT");
        (await PantryAsync(client)).Should().BeEquivalentTo(pantry);
    }

    [Fact]
    public async Task P9_of_two_cookings_at_once_over_the_stock_one_wins_and_the_stock_never_goes_negative()
    {
        var client = await ApiClient.SignedInAsync(factory);
        await AddAsync(client, "paradicsom", 400, "g", 3);
        var used = TomatoOnly(await PreviewAsync(client, "Lecsó", 4), 300);

        var responses = await Task.WhenAll(
            client.PostAsync("/api/v1/cookings", Body(Guid.CreateVersion7(), "Lecsó", 4, used)),
            client.PostAsync("/api/v1/cookings", Body(Guid.CreateVersion7(), "Lecsó", 4, used)));

        responses.Select(r => r.StatusCode).Should().BeEquivalentTo([HttpStatusCode.Created, HttpStatusCode.Conflict]);
        var loser = responses.Single(r => r.StatusCode == HttpStatusCode.Conflict);
        (await ApiClient.ProblemAsync(loser)).GetProperty("code").GetString().Should().BeOneOf("INSUFFICIENT_STOCK", "PANTRY_ITEM_MODIFIED");
        (await PantryAsync(client)).Should().Equal(("paradicsom", 100m));
        (await QuantitiesMatchTheLogAsync(client)).Should().BeTrue();
    }

    [Fact]
    public async Task P10_using_more_than_the_stock_is_refused_with_the_fresh_amounts_and_nothing_is_deducted()
    {
        var client = await ApiClient.SignedInAsync(factory);
        await AddAsync(client, "paradicsom", 200, "g", 3);
        var used = TomatoOnly(await PreviewAsync(client, "Lecsó", 4), 250);

        var response = await client.PostAsync("/api/v1/cookings", Body(Guid.CreateVersion7(), "Lecsó", 4, used));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await ApiClient.ProblemAsync(response);
        problem.GetProperty("code").GetString().Should().Be("INSUFFICIENT_STOCK");
        var tomato = problem.GetProperty("available").EnumerateArray().Single(a => a.GetProperty("ingredientName").GetString() == "paradicsom");
        (tomato.GetProperty("amount").GetDecimal(), tomato.GetProperty("unit").GetString()).Should().Be((200m, "g"));
        (await PantryAsync(client)).Should().Equal(("paradicsom", 200m));
        (await QuantitiesMatchTheLogAsync(client)).Should().BeTrue();
    }

    // ADR-0008 Verification: an item changed between loading and saving (another tab, another cooking)
    // makes the whole save fail - deterministic, unlike the race of P9. Deliberately below the API: the
    // change must land between the two steps; the HTTP path of the same conflict is covered by P9 and
    // AdversarialCookingApiTests (cooking and a manual edit at once).
    [Fact]
    public async Task An_item_changed_after_loading_fails_the_save_with_PANTRY_ITEM_MODIFIED_and_nothing_is_saved()
    {
        var client = await ApiClient.SignedInAsync(factory);
        await AddAsync(client, "paradicsom", 400, "g", 3);
        var householdId = await HouseholdIdAsync(client);
        var ct = TestContext.Current.CancellationToken;
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KamraDbContext>();
        var recipe = (await new RecipeRepository(db).FindAsync(householdId, RecipeId("Lecsó"), ct))!.Candidate;
        var items = await new PantryRepository(db).ListForCookingAsync(householdId, [.. recipe.Needs.Select(n => n.IngredientId)], ct);
        using (var other = factory.Services.CreateScope())
        {
            await other.ServiceProvider.GetRequiredService<KamraDbContext>().Database
                .ExecuteSqlAsync($"""UPDATE "PantryItems" SET "UpdatedAt" = now() WHERE "Id" = {items.Single().Id}""", ct);
        }

        var used = recipe.Needs.Where(n => !n.IsStaple).ToDictionary(n => n.IngredientId, n => n.IngredientName == "paradicsom" ? 300m : 0m);
        var (cooking, movements) = Cooking.Create(householdId, recipe, 4, Guid.CreateVersion7(), used, items, DateTimeOffset.UtcNow);
        var cookings = new CookingRepository(db);
        cookings.Add(cooking, movements);

        var error = await FluentActions.Awaiting(() => cookings.SaveChangesAsync(ct)).Should().ThrowAsync<ConflictException>();

        error.Which.Code.Should().Be("PANTRY_ITEM_MODIFIED");
        (await PantryAsync(client)).Should().Equal(("paradicsom", 400m));
        (await new CookingRepository(db).FindByRequestAsync(householdId, cooking.RequestId, ct)).Should().BeNull();
    }

    [Fact]
    public async Task Another_household_with_the_same_request_id_gets_its_own_cooking()
    {
        var owner = await ApiClient.SignedInAsync(factory);
        var other = await ApiClient.SignedInAsync(factory);
        await AddAsync(owner, "paradicsom", 400, "g", 3);
        await AddAsync(other, "paradicsom", 400, "g", 3);
        var requestId = Guid.CreateVersion7();
        var used = TomatoOnly(await PreviewAsync(owner, "Lecsó", 4), 300);

        var first = await owner.PostAsync("/api/v1/cookings", Body(requestId, "Lecsó", 4, used));
        var second = await other.PostAsync("/api/v1/cookings", Body(requestId, "Lecsó", 4, used));

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Created, "the request id is unique per household, so it never reveals another household's cooking");
        (await IdAsync(second)).Should().NotBe(await IdAsync(first));
        (await PantryAsync(owner)).Should().Equal(("paradicsom", 100m));
        (await PantryAsync(other)).Should().Equal(("paradicsom", 100m));
    }

    [Fact]
    public async Task An_unknown_recipe_is_not_found_for_the_preview_and_the_cooking()
    {
        var client = await ApiClient.SignedInAsync(factory);
        var unknown = Guid.CreateVersion7();

        var preview = await client.Http.GetAsync($"/api/v1/recipes/{unknown}/cooking-preview", TestContext.Current.CancellationToken);
        var cooking = await client.PostAsync("/api/v1/cookings",
            new { requestId = Guid.CreateVersion7(), recipeId = unknown, servings = 4, used = Array.Empty<object>() });

        preview.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ApiClient.ProblemAsync(preview)).GetProperty("code").GetString().Should().Be("RECIPE_NOT_FOUND");
        cooking.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ApiClient.ProblemAsync(cooking)).GetProperty("code").GetString().Should().Be("RECIPE_NOT_FOUND");
    }

    // D-a: 12.5 g of potatoes is 0.0125 kg in the recipe's unit; the preview's default must be accepted.
    [Fact]
    public async Task The_preview_default_from_finer_stock_than_the_recipe_unit_can_be_confirmed()
    {
        var client = await ApiClient.SignedInAsync(factory);
        await AddAsync(client, "burgonya", 12.5m, "g", 30);
        var preview = await PreviewAsync(client, "Paprikás krumpli", 4);

        var response = await client.PostAsync("/api/v1/cookings", Body(Guid.CreateVersion7(), "Paprikás krumpli", 4, Used(preview)));

        Line(preview, "burgonya").GetProperty("defaultUsed").GetDecimal().Should().Be(0.0125m);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await PantryAsync(client)).Should().BeEmpty();
    }

    [Fact]
    public async Task A_missing_used_entry_is_a_field_error_not_a_server_error()
    {
        var client = await ApiClient.SignedInAsync(factory);

        var response = await client.PostAsync("/api/v1/cookings",
            new { requestId = Guid.CreateVersion7(), recipeId = RecipeId("Lecsó"), servings = 4, used = new object?[] { null } });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ApiClient.ProblemAsync(response)).GetProperty("errors").TryGetProperty("used", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Servings_out_of_range_are_a_field_error()
    {
        var client = await ApiClient.SignedInAsync(factory);
        var used = Used(await PreviewAsync(client, "Lecsó", 4));

        var preview = await client.Http.GetAsync($"/api/v1/recipes/{RecipeId("Lecsó")}/cooking-preview?servings=0", TestContext.Current.CancellationToken);
        var cooking = await client.PostAsync("/api/v1/cookings", Body(Guid.CreateVersion7(), "Lecsó", 101, used));

        foreach (var response in new[] { preview, cooking })
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await ApiClient.ProblemAsync(response)).GetProperty("errors").TryGetProperty("servings", out _).Should().BeTrue();
        }
    }

    [Theory]
    [InlineData("GET", "/api/v1/recipes/0199c9a0-0000-7000-8000-000000000001/cooking-preview")]
    [InlineData("POST", "/api/v1/cookings")]
    public async Task Cooking_requires_sign_in(string method, string path)
    {
        var anonymous = await ApiClient.CreateAsync(factory);

        var response = await anonymous.Http.SendAsync(new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = method == "GET" ? null : JsonContent.Create(new { }),
        }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ApiClient.ProblemAsync(response)).GetProperty("code").GetString().Should().Be("UNAUTHENTICATED");
    }

    private sealed record UsedAmount(Guid IngredientId, decimal Amount);

    private static object Body(Guid requestId, string recipe, int servings, IEnumerable<UsedAmount> used) =>
        new { requestId, recipeId = RecipeId(recipe), servings, used = used.Select(u => new { ingredientId = u.IngredientId, amount = u.Amount }) };

    // The confirmation screen's defaults: the smaller of required and available.
    private static List<UsedAmount> Used(JsonElement preview) =>
        [.. preview.GetProperty("lines").EnumerateArray()
            .Select(l => new UsedAmount(l.GetProperty("ingredientId").GetGuid(), l.GetProperty("defaultUsed").GetDecimal()))];

    // Only the tomatoes are used; every other line is 0 (S5-D4).
    private static List<UsedAmount> TomatoOnly(JsonElement preview, decimal grams) =>
        [.. preview.GetProperty("lines").EnumerateArray().Select(l => new UsedAmount(l.GetProperty("ingredientId").GetGuid(),
            l.GetProperty("ingredientName").GetString() == "paradicsom" ? grams : 0))];

    private static async Task<JsonElement> PreviewAsync(ApiClient client, string recipe, int servings) =>
        await client.GetJsonAsync($"/api/v1/recipes/{RecipeId(recipe)}/cooking-preview?servings={servings}");

    private static JsonElement Line(JsonElement element, string ingredient) =>
        element.GetProperty("lines").EnumerateArray().Single(l => l.GetProperty("ingredientName").GetString() == ingredient);

    private static List<string?> Strings(JsonElement element, string property) =>
        [.. element.GetProperty(property).EnumerateArray().Select(s => s.GetString())];

    private static string Iso(int days) => Today.AddDays(days).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

    private static async Task<string> RawAsync(HttpResponseMessage response) =>
        await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

    private static async Task<Guid> IdAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("cookingId").GetGuid();

    private static async Task<List<(string? Name, decimal Quantity)>> PantryAsync(ApiClient client) =>
        [.. (await client.GetJsonAsync("/api/v1/pantry-items")).EnumerateArray()
            .Select(i => (i.GetProperty("ingredientName").GetString(), i.GetProperty("quantity").GetDecimal()))];

    private static async Task AddLecsoStockAsync(ApiClient client)
    {
        await AddAsync(client, "paradicsom", 200, "g", 1);
        await AddAsync(client, "paradicsom", 400, "g", 5);
        await AddAsync(client, "paprika", 500, "g", 2);
        await AddAsync(client, "vöröshagyma", 300, "g", 10);
        await AddAsync(client, "étolaj", 1, "l", 90);
    }

    private static async Task AddAsync(ApiClient client, string ingredient, decimal amount, string unit, int daysToExpiry)
    {
        var ingredientId = (await client.GetJsonAsync($"/api/v1/ingredients?search={Uri.EscapeDataString(ingredient)}")).EnumerateArray()
            .Single(i => i.GetProperty("name").GetString() == ingredient).GetProperty("id").GetGuid();
        var response = await client.Http.PostAsJsonAsync("/api/v1/pantry-items",
            new { ingredientId, amount, unit, expiryDate = Today.AddDays(daysToExpiry) }, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task<List<(string Name, decimal Delta)>> CookingMovementsAsync(Guid cookingId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KamraDbContext>();
        var rows = await (from movement in db.StockMovements
                          join item in db.PantryItems on movement.PantryItemId equals item.Id
                          join ingredient in db.Ingredients on item.IngredientId equals ingredient.Id
                          where movement.CookingId == cookingId
                          select new { ingredient.Name, movement.Delta, movement.Reason })
            .ToListAsync(TestContext.Current.CancellationToken);
        rows.Should().AllSatisfy(r => r.Reason.Should().Be(KamraApp.Domain.Pantry.MovementReason.Consumed));
        return [.. rows.Select(r => (r.Name, r.Delta))];
    }

    // CAP-08 invariant for the client's household: every item's quantity is the sum of its movements, and never negative.
    private async Task<bool> QuantitiesMatchTheLogAsync(ApiClient client)
    {
        var householdId = await HouseholdIdAsync(client);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KamraDbContext>();
        return !await db.PantryItems.AnyAsync(i => i.HouseholdId == householdId && (i.Quantity < 0
            || db.StockMovements.Where(m => m.PantryItemId == i.Id).Sum(m => m.Delta) != i.Quantity), TestContext.Current.CancellationToken);
    }

    private static async Task<Guid> HouseholdIdAsync(ApiClient client) =>
        (await client.GetJsonAsync("/api/v1/auth/me")).GetProperty("householdId").GetGuid();
}
