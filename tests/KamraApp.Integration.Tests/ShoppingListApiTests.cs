using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KamraApp.Infrastructure.Persistence;
using KamraApp.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace KamraApp.Integration.Tests;

// US-5 through the API on a real PostgreSQL with the seeded recipes: the hand-calculated examples P11-P16, P19
// and P20 (kamra_okt22_terv.md, 5.), the same shortage on every screen (the supervisor's 4th letter), the
// ADR-0008 6. lock under concurrent requests, the partial unique index, household isolation (QA-1), sign-in and
// antiforgery.
[Collection("api")]
public class ShoppingListApiTests(KamraApiFactory factory)
{
    private static DateOnly Today => KamraApp.Application.Common.Today.Of(TimeProvider.System);

    [Fact]
    public async Task P11_the_shortage_of_an_almost_makeable_recipe_goes_on_the_list()
    {
        var client = await ApiClient.SignedInAsync(factory);
        await AddAsync(client, "száraztészta", 400, "g", 90);
        await AddAsync(client, "túró", 500, "g", 5);
        await AddAsync(client, "szalonna", 150, "g", 7);
        await AddAsync(client, "tejföl", 150, "g", 6);
        var shortage = (await client.GetJsonAsync("/api/v1/recommendations")).GetProperty("almostMakeable").EnumerateArray()
            .Single(r => r.GetProperty("name").GetString() == "Túrós csusza").GetProperty("shortages").EnumerateArray().Single();

        var response = await AddToListAsync(client, "tejföl", shortage.GetProperty("missing").GetDecimal(), shortage.GetProperty("unit").GetString());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ListAsync(client)).Should().Equal(("tejföl", 50m, "g", false));
    }

    [Fact]
    public async Task P12a_the_same_ingredient_adds_up_on_one_row()
    {
        var client = await ApiClient.SignedInAsync(factory);

        (await AddToListAsync(client, "tejföl", 50, "g")).EnsureSuccessStatusCode();
        (await AddToListAsync(client, "tejföl", 100, "g")).EnsureSuccessStatusCode();

        (await ListAsync(client)).Should().Equal(("tejföl", 150m, "g", false));
    }

    [Fact]
    public async Task P12b_a_checked_row_starts_again_with_the_new_amount()
    {
        var client = await ApiClient.SignedInAsync(factory);
        var row = await ReadAsync(await AddToListAsync(client, "tejföl", 50, "g"));
        (await client.PutAsync(RowPath(row), new { @checked = true, amount = 50, unit = "g" })).EnsureSuccessStatusCode();

        (await AddToListAsync(client, "tejföl", 50, "g")).EnsureSuccessStatusCode();

        (await ListAsync(client)).Should().Equal(("tejföl", 50m, "g", false));
    }

    [Fact]
    public async Task P13_the_whole_path_shows_the_same_shortage_and_ends_with_the_right_list()
    {
        var client = await ApiClient.SignedInAsync(factory);
        await AddAsync(client, "paprika", 500, "g", 2);
        await AddAsync(client, "paradicsom", 200, "g", 1);
        await AddAsync(client, "vöröshagyma", 300, "g", 10);
        await AddAsync(client, "étolaj", 1, "l", 90);

        var recommended = (await client.GetJsonAsync("/api/v1/recommendations")).GetProperty("almostMakeable").EnumerateArray()
            .Single(r => r.GetProperty("name").GetString() == "Lecsó").GetProperty("shortages").EnumerateArray().Single();
        (recommended.GetProperty("ingredientName").GetString(), recommended.GetProperty("missing").GetDecimal(),
            recommended.GetProperty("available").GetDecimal(), recommended.GetProperty("required").GetDecimal()).Should().Be(("paradicsom", 100m, 200m, 300m));
        var preview = await PreviewAsync(client, "Lecsó", 4);
        var previewShortage = Line(preview, "paradicsom").GetProperty("shortage").GetDecimal();
        previewShortage.Should().Be(100m);
        (await AddToListAsync(client, "paradicsom", previewShortage, "g")).EnsureSuccessStatusCode();

        var body = CookingBody(Guid.CreateVersion7(), "Lecsó", 4, preview);
        var cooked = await client.PostAsync("/api/v1/cookings", body);

        cooked.StatusCode.Should().Be(HttpStatusCode.Created);
        var cooking = await ReadAsync(cooked);
        Strings(cooking, "depletedIngredients").Should().Equal("paprika", "paradicsom");
        Strings(cooking, "suggestedIngredients").Should().Equal("paprika");     // M15: the tomato is already on the list
        (await PantryAsync(client)).Should().BeEquivalentTo([("vöröshagyma", 150m), ("étolaj", 970m)]);
        var suggestion = (await SuggestionsAsync(client)).Should().ContainSingle().Subject;
        (suggestion.GetProperty("ingredientName").GetString(), suggestion.GetProperty("trigger").GetString()).Should().Be(("paprika", "depleted"));

        (await client.PostAsync(AcceptPath(suggestion), new { amount = 500, unit = "g" })).StatusCode.Should().Be(HttpStatusCode.OK);
        var tomatoRow = (await client.GetJsonAsync("/api/v1/shopping-list")).EnumerateArray()
            .Single(r => r.GetProperty("ingredientName").GetString() == "paradicsom");
        (await client.PutAsync(RowPath(tomatoRow), new { @checked = true, amount = 100, unit = "g" })).EnsureSuccessStatusCode();

        (await ListAsync(client)).Should().Equal(("paprika", 500m, "g", false), ("paradicsom", 100m, "g", true));
        (await SuggestionsAsync(client)).Should().BeEmpty();
        var repeated = await client.PostAsync("/api/v1/cookings", body);
        repeated.StatusCode.Should().Be(HttpStatusCode.OK);
        (await RawAsync(repeated)).Should().Be(await RawAsync(cooked)); // M5: the stored answer, with the same suggestions
    }

    [Theory]
    [InlineData("consumed", 1)]   // P14a
    [InlineData("discarded", 0)]  // P14b
    [InlineData("corrected", 0)]  // P14c
    public async Task P14_only_using_up_the_last_item_suggests(string reason, int suggestions)
    {
        var client = await ApiClient.SignedInAsync(factory);
        var item = await ReadAsync(await AddAsync(client, "tejföl", 200, "g", 6));

        (await DecreaseAsync(client, item, 0, "g", reason)).EnsureSuccessStatusCode();

        (await SuggestionsAsync(client)).Should().HaveCount(suggestions);
    }

    [Fact]
    public async Task P15_a_rejected_suggestion_returns_only_after_buying_and_using_up_again()
    {
        var client = await ApiClient.SignedInAsync(factory);
        var first = await ReadAsync(await AddAsync(client, "tej", 1, "l", 5));
        (await DecreaseAsync(client, first, 0, "l", "consumed")).EnsureSuccessStatusCode();
        var rejected = (await SuggestionsAsync(client)).Should().ContainSingle().Subject;

        (await client.PostAsync(RejectPath(rejected), null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await SuggestionsAsync(client)).Should().BeEmpty();

        var second = await ReadAsync(await AddAsync(client, "tej", 1, "l", 7));
        (await SuggestionsAsync(client)).Should().BeEmpty();
        (await DecreaseAsync(client, second, 0, "l", "consumed")).EnsureSuccessStatusCode();

        var renewed = (await SuggestionsAsync(client)).Should().ContainSingle().Subject;
        renewed.GetProperty("id").GetGuid().Should().NotBe(rejected.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task P16a_an_accepted_suggestion_without_an_amount_takes_the_next_known_amount()
    {
        var client = await ApiClient.SignedInAsync(factory);
        var item = await ReadAsync(await AddAsync(client, "tejföl", 200, "g", 6));
        (await DecreaseAsync(client, item, 0, "g", "consumed")).EnsureSuccessStatusCode();
        var suggestion = (await SuggestionsAsync(client)).Should().ContainSingle().Subject;

        var accepted = await client.PostAsync(AcceptPath(suggestion), new { });
        accepted.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ListAsync(client)).Should().Equal(("tejföl", (decimal?)null, (string?)null, false));
        (await AddToListAsync(client, "tejföl", 200, "g")).EnsureSuccessStatusCode();

        (await ListAsync(client)).Should().Equal(("tejföl", 200m, "g", false));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task P16b_an_unknown_amount_keeps_the_known_one_in_either_order(bool unknownFirst)
    {
        var client = await ApiClient.SignedInAsync(factory);

        foreach (var known in unknownFirst ? new[] { false, true } : new[] { true, false })
        {
            (await (known ? AddToListAsync(client, "tejföl", 200, "g") : AddToListAsync(client, "tejföl", null, null))).EnsureSuccessStatusCode();
        }

        (await ListAsync(client)).Should().Equal(("tejföl", 200m, "g", false));
    }

    [Fact]
    public async Task P16c_two_unknown_amounts_stay_unknown()
    {
        var client = await ApiClient.SignedInAsync(factory);

        (await AddToListAsync(client, "tejföl", null, null)).EnsureSuccessStatusCode();
        (await AddToListAsync(client, "tejföl", null, null)).EnsureSuccessStatusCode();

        (await ListAsync(client)).Should().Equal(("tejföl", (decimal?)null, (string?)null, false));
    }

    [Fact]
    public async Task P19_the_minimum_suggests_on_crossing_it_and_again_after_a_cleared_rejection()
    {
        var client = await ApiClient.SignedInAsync(factory);
        var first = await ReadAsync(await AddAsync(client, "tej", 600, "ml", 5));
        (await SetMinimumAsync(client, "tej", 500, "ml")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await SuggestionsAsync(client)).Should().BeEmpty();

        first = await ReadAsync(await DecreaseAsync(client, first, 400, "ml", "consumed"));
        var below = (await SuggestionsAsync(client)).Should().ContainSingle().Subject;
        Reason(below).Should().Be(("tej", "belowMinimum", (400m, "ml"), (500m, "ml"), (100m, "ml")));

        (await client.PostAsync(RejectPath(below), null)).EnsureSuccessStatusCode();
        var second = await ReadAsync(await AddAsync(client, "tej", 1, "l", 9));          // 1400 ml: cleared
        (await DecreaseAsync(client, first, 0, "ml", "consumed")).EnsureSuccessStatusCode(); // 1000 ml: still above
        (await SuggestionsAsync(client)).Should().BeEmpty();
        (await DecreaseAsync(client, second, 0.3m, "l", "consumed")).EnsureSuccessStatusCode(); // 300 ml

        Reason((await SuggestionsAsync(client)).Should().ContainSingle().Subject).Should().Be(("tej", "belowMinimum", (300m, "ml"), (500m, "ml"), (200m, "ml")));
    }

    [Fact]
    public async Task P19c_setting_a_minimum_above_the_stock_suggests_at_once()
    {
        var client = await ApiClient.SignedInAsync(factory);
        await AddAsync(client, "tej", 300, "ml", 5);

        (await SetMinimumAsync(client, "tej", 500, "ml")).EnsureSuccessStatusCode();

        Reason((await SuggestionsAsync(client)).Should().ContainSingle().Subject).Should().Be(("tej", "belowMinimum", (300m, "ml"), (500m, "ml"), (200m, "ml")));
    }

    [Fact]
    public async Task D4_the_ingredient_list_shows_the_households_minimum_until_it_is_removed()
    {
        var client = await ApiClient.SignedInAsync(factory);
        var other = await ApiClient.SignedInAsync(factory);

        (await SetMinimumAsync(client, "tej", 5, "dl")).EnsureSuccessStatusCode();

        Amount((await IngredientAsync(client, "tej")).GetProperty("minimum")).Should().Be((5m, "dl"));
        (await IngredientAsync(other, "tej")).GetProperty("minimum").ValueKind.Should().Be(JsonValueKind.Null);

        (await client.PutAsync($"/api/v1/ingredients/{await IngredientIdAsync(client, "tej")}/minimum", new { })).EnsureSuccessStatusCode();

        (await IngredientAsync(client, "tej")).GetProperty("minimum").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task P20_using_up_the_last_two_items_at_once_makes_exactly_one_suggestion()
    {
        for (var run = 0; run < 5; run++)
        {
            var client = await ApiClient.SignedInAsync(factory);
            var soon = await ReadAsync(await AddAsync(client, "tej", 500, "ml", 1));
            var later = await ReadAsync(await AddAsync(client, "tej", 500, "ml", 3));

            var responses = await Task.WhenAll(DecreaseAsync(client, soon, 0, "ml", "consumed"), DecreaseAsync(client, later, 0, "ml", "consumed"));

            responses.Select(r => r.StatusCode).Should().AllBeEquivalentTo(HttpStatusCode.OK);
            (await PantryAsync(client)).Should().BeEmpty();
            (await SuggestionsAsync(client)).Should().ContainSingle($"run {run}: one suggestion, never none or two (ADR-0008 6.)");
        }
    }

    [Fact]
    public async Task The_same_ingredient_added_twice_at_once_is_one_row_with_the_sum()
    {
        var client = await ApiClient.SignedInAsync(factory);

        var responses = await Task.WhenAll(AddToListAsync(client, "tejföl", 50, "g"), AddToListAsync(client, "tejföl", 50, "g"));

        responses.Select(r => r.StatusCode).Should().AllBeEquivalentTo(HttpStatusCode.OK);
        (await ListAsync(client)).Should().Equal(("tejföl", 100m, "g", false));
    }

    [Fact]
    public async Task Accepting_twice_gives_the_same_row_and_rejecting_afterwards_is_a_conflict()
    {
        var client = await ApiClient.SignedInAsync(factory);
        var suggestion = await DepletedSuggestionAsync(client);

        var first = await client.PostAsync(AcceptPath(suggestion), new { amount = 200, unit = "g" });
        var second = await client.PostAsync(AcceptPath(suggestion), new { amount = 200, unit = "g" });
        var reject = await client.PostAsync(RejectPath(suggestion), null);

        (first.StatusCode, second.StatusCode).Should().Be((HttpStatusCode.OK, HttpStatusCode.OK));
        (await RawAsync(second)).Should().Be(await RawAsync(first));
        (await ListAsync(client)).Should().Equal(("tejföl", 200m, "g", false)); // the repeat did not add again
        reject.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ApiClient.ProblemAsync(reject)).GetProperty("code").GetString().Should().Be("SHOPPING_SUGGESTION_STATE_CONFLICT");
    }

    [Fact]
    public async Task Accepting_and_rejecting_at_once_lets_exactly_one_win()
    {
        var client = await ApiClient.SignedInAsync(factory);
        var suggestion = await DepletedSuggestionAsync(client);

        var responses = await Task.WhenAll(client.PostAsync(AcceptPath(suggestion), new { }), client.PostAsync(RejectPath(suggestion), null));

        responses.Count(r => r.IsSuccessStatusCode).Should().Be(1);
        var loser = responses.Single(r => !r.IsSuccessStatusCode);
        loser.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ApiClient.ProblemAsync(loser)).GetProperty("code").GetString().Should().Be("SHOPPING_SUGGESTION_STATE_CONFLICT");
        (await ListAsync(client)).Should().HaveCount(responses[0].IsSuccessStatusCode ? 1 : 0);
    }

    [Fact]
    public async Task The_database_keeps_at_most_one_blocking_suggestion_per_ingredient()
    {
        var client = await ApiClient.SignedInAsync(factory);
        var suggestion = await DepletedSuggestionAsync(client);
        var householdId = (await client.GetJsonAsync("/api/v1/auth/me")).GetProperty("householdId").GetGuid();
        var ingredientId = suggestion.GetProperty("ingredientId").GetGuid();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KamraDbContext>();

        var duplicate = () => db.Database.ExecuteSqlAsync(
            $"""INSERT INTO "ShoppingSuggestions" ("Id", "HouseholdId", "IngredientId", "Trigger", "State", "CreatedAt") VALUES ({Guid.CreateVersion7()}, {householdId}, {ingredientId}, 'BelowMinimum', 'Open', {DateTimeOffset.UtcNow})""",
            TestContext.Current.CancellationToken);

        (await duplicate.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
    }

    [Fact]
    public async Task Another_households_rows_and_suggestions_are_indistinguishable_from_missing_ones()
    {
        var owner = await ApiClient.SignedInAsync(factory);
        var row = await ReadAsync(await AddToListAsync(owner, "tejföl", 50, "g"));
        var suggestion = await DepletedSuggestionAsync(owner, "tej");
        var other = await ApiClient.SignedInAsync(factory);
        var missing = Guid.CreateVersion7();

        var foreignEdit = await other.PutAsync(RowPath(row), new { @checked = true });
        var missingEdit = await other.PutAsync($"/api/v1/shopping-list/items/{missing}", new { @checked = true });
        var foreignDelete = await other.Http.DeleteAsync(RowPath(row), TestContext.Current.CancellationToken);
        var foreignAccept = await other.PostAsync(AcceptPath(suggestion), new { });
        var foreignReject = await other.PostAsync(RejectPath(suggestion), null);
        var missingAccept = await other.PostAsync($"/api/v1/shopping-suggestions/{missing}/accept", new { });

        var foreignEditProblem = await ApiClient.ProblemAsync(foreignEdit);
        var foreignAcceptProblem = await ApiClient.ProblemAsync(foreignAccept);
        WithoutRequestIds(foreignEditProblem).Should().Be(WithoutRequestIds(await ApiClient.ProblemAsync(missingEdit)));
        foreignEditProblem.GetProperty("code").GetString().Should().Be("SHOPPING_LIST_ITEM_NOT_FOUND");
        foreignDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
        WithoutRequestIds(foreignAcceptProblem).Should().Be(WithoutRequestIds(await ApiClient.ProblemAsync(missingAccept)));
        foreignAcceptProblem.GetProperty("code").GetString().Should().Be("SHOPPING_SUGGESTION_NOT_FOUND");
        foreignReject.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ListAsync(other)).Should().BeEmpty();
        (await SuggestionsAsync(other)).Should().BeEmpty();
        (await ListAsync(owner)).Should().Equal(("tejföl", 50m, "g", false));
        (await SuggestionsAsync(owner)).Should().ContainSingle();
    }

    [Theory]
    [InlineData("GET", "/api/v1/shopping-list")]
    [InlineData("POST", "/api/v1/shopping-list/items")]
    [InlineData("PUT", "/api/v1/shopping-list/items/0199c9a0-0000-7000-8000-000000000001")]
    [InlineData("DELETE", "/api/v1/shopping-list/items/0199c9a0-0000-7000-8000-000000000001")]
    [InlineData("GET", "/api/v1/shopping-suggestions")]
    [InlineData("POST", "/api/v1/shopping-suggestions/0199c9a0-0000-7000-8000-000000000001/accept")]
    [InlineData("POST", "/api/v1/shopping-suggestions/0199c9a0-0000-7000-8000-000000000001/reject")]
    [InlineData("PUT", "/api/v1/ingredients/0199c9a0-0000-7000-8000-000000000001/minimum")]
    public async Task Shopping_list_endpoints_require_sign_in(string method, string path)
    {
        var anonymous = await ApiClient.CreateAsync(factory);

        var response = await anonymous.Http.SendAsync(new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = method is "GET" or "DELETE" ? null : JsonContent.Create(new { }),
        }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ApiClient.ProblemAsync(response)).GetProperty("code").GetString().Should().Be("UNAUTHENTICATED");
    }

    [Fact]
    public async Task Adding_without_the_antiforgery_token_is_rejected()
    {
        var client = await ApiClient.SignedInAsync(factory);
        var ingredientId = await IngredientIdAsync(client, "tejföl");
        client.Http.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");

        var response = await client.PostAsync("/api/v1/shopping-list/items", new { ingredientId, amount = 50, unit = "g" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ApiClient.ProblemAsync(response)).GetProperty("code").GetString().Should().Be("ANTIFORGERY_TOKEN_INVALID");
    }

    [Theory]
    [InlineData("""{ "amount": 50 }""", "unit")]
    [InlineData("""{ "unit": "g" }""", "amount")]
    [InlineData("""{ "amount": 0, "unit": "g" }""", "amount")]
    [InlineData("""{ "amount": 2, "unit": "dl" }""", "unit")]   // tejföl is measured by mass
    [InlineData("""{ "amount": "50", "unit": "g" }""", "")]     // numbers only as JSON numbers
    public async Task An_invalid_row_is_rejected_with_a_field_error(string fields, string field)
    {
        var client = await ApiClient.SignedInAsync(factory);
        var ingredientId = await IngredientIdAsync(client, "tejföl");
        var body = fields.Replace("{ ", $$"""{ "ingredientId": "{{ingredientId}}", """, StringComparison.Ordinal);

        var response = await client.Http.PostAsync("/api/v1/shopping-list/items",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await ApiClient.ProblemAsync(response);
        problem.GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
        if (field.Length > 0)
        {
            problem.GetProperty("errors").TryGetProperty(field, out _).Should().BeTrue();
        }

        (await ListAsync(client)).Should().BeEmpty();
    }

    [Fact]
    public async Task An_unknown_ingredient_is_not_found()
    {
        var client = await ApiClient.SignedInAsync(factory);

        var response = await client.PostAsync("/api/v1/shopping-list/items", new { ingredientId = Guid.CreateVersion7(), amount = 1, unit = "g" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ApiClient.ProblemAsync(response)).GetProperty("code").GetString().Should().Be("INGREDIENT_NOT_FOUND");
    }

    [Fact]
    public async Task M7_a_sum_over_the_limit_is_rejected_and_the_row_stays()
    {
        var client = await ApiClient.SignedInAsync(factory);
        (await AddToListAsync(client, "tejföl", 99_999, "g")).EnsureSuccessStatusCode();

        var response = await AddToListAsync(client, "tejföl", 2, "g");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ApiClient.ProblemAsync(response)).GetProperty("errors").TryGetProperty("amount", out _).Should().BeTrue();
        (await ListAsync(client)).Should().Equal(("tejföl", 99_999m, "g", false));
    }

    [Fact]
    public async Task A_deleted_row_is_gone_and_no_longer_stops_a_suggestion()
    {
        var client = await ApiClient.SignedInAsync(factory);
        var row = await ReadAsync(await AddToListAsync(client, "tejföl", 50, "g"));
        var item = await ReadAsync(await AddAsync(client, "tejföl", 200, "g", 6));

        (await client.Http.DeleteAsync(RowPath(row), TestContext.Current.CancellationToken)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await DecreaseAsync(client, item, 0, "g", "consumed")).EnsureSuccessStatusCode();

        (await ListAsync(client)).Should().BeEmpty();
        (await SuggestionsAsync(client)).Should().ContainSingle();
        (await client.Http.DeleteAsync(RowPath(row), TestContext.Current.CancellationToken)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static string RowPath(JsonElement row) => $"/api/v1/shopping-list/items/{row.GetProperty("id").GetGuid()}";

    private static string AcceptPath(JsonElement suggestion) => $"/api/v1/shopping-suggestions/{suggestion.GetProperty("id").GetGuid()}/accept";

    private static string RejectPath(JsonElement suggestion) => $"/api/v1/shopping-suggestions/{suggestion.GetProperty("id").GetGuid()}/reject";

    // A depleted suggestion for the ingredient: 200 g (or 1 l) bought and used up.
    private static async Task<JsonElement> DepletedSuggestionAsync(ApiClient client, string ingredient = "tejföl")
    {
        var (amount, unit) = ingredient == "tej" ? (1m, "l") : (200m, "g");
        var item = await ReadAsync(await AddAsync(client, ingredient, amount, unit, 6));
        (await DecreaseAsync(client, item, 0, unit, "consumed")).EnsureSuccessStatusCode();
        return (await SuggestionsAsync(client)).Single();
    }

    // Name, trigger, and the current, minimum and suggested amounts in the minimum's unit.
    private static (string?, string?, (decimal, string?), (decimal, string?), (decimal, string?)) Reason(JsonElement suggestion) =>
        (suggestion.GetProperty("ingredientName").GetString(), suggestion.GetProperty("trigger").GetString(),
            Amount(suggestion.GetProperty("currentAmount")), Amount(suggestion.GetProperty("minimumAmount")), Amount(suggestion.GetProperty("suggestedAmount")));

    private static (decimal, string?) Amount(JsonElement amount) => (amount.GetProperty("value").GetDecimal(), amount.GetProperty("unit").GetString());

    private static async Task<List<JsonElement>> SuggestionsAsync(ApiClient client) =>
        [.. (await client.GetJsonAsync("/api/v1/shopping-suggestions")).EnumerateArray()];

    private static async Task<List<(string? Name, decimal? Amount, string? Unit, bool Checked)>> ListAsync(ApiClient client) =>
        [.. (await client.GetJsonAsync("/api/v1/shopping-list")).EnumerateArray().Select(r => (
            r.GetProperty("ingredientName").GetString(),
            r.GetProperty("amount").ValueKind == JsonValueKind.Null ? (decimal?)null : r.GetProperty("amount").GetProperty("value").GetDecimal(),
            r.GetProperty("amount").ValueKind == JsonValueKind.Null ? null : r.GetProperty("amount").GetProperty("unit").GetString(),
            r.GetProperty("checked").GetBoolean()))];

    private static async Task<HttpResponseMessage> AddToListAsync(ApiClient client, string ingredient, decimal? amount, string? unit) =>
        await client.PostAsync("/api/v1/shopping-list/items", new { ingredientId = await IngredientIdAsync(client, ingredient), amount, unit });

    private static async Task<HttpResponseMessage> SetMinimumAsync(ApiClient client, string ingredient, decimal amount, string unit) =>
        await client.PutAsync($"/api/v1/ingredients/{await IngredientIdAsync(client, ingredient)}/minimum", new { amount, unit });

    private static async Task<JsonElement> IngredientAsync(ApiClient client, string name) =>
        (await client.GetJsonAsync($"/api/v1/ingredients?search={Uri.EscapeDataString(name)}")).EnumerateArray()
            .Single(i => i.GetProperty("name").GetString() == name);

    private static async Task<Guid> IngredientIdAsync(ApiClient client, string name) => (await IngredientAsync(client, name)).GetProperty("id").GetGuid();

    private static async Task<HttpResponseMessage> AddAsync(ApiClient client, string ingredient, decimal amount, string unit, int daysToExpiry)
    {
        var response = await client.PostAsync("/api/v1/pantry-items",
            new { ingredientId = await IngredientIdAsync(client, ingredient), amount, unit, expiryDate = Today.AddDays(daysToExpiry) });
        response.EnsureSuccessStatusCode();
        return response;
    }

    // A decrease of a pantry item (US-1) with its reason; the item as the last answer returned it.
    private static Task<HttpResponseMessage> DecreaseAsync(ApiClient client, JsonElement item, decimal amount, string unit, string reason) =>
        client.PutAsync($"/api/v1/pantry-items/{item.GetProperty("id").GetGuid()}", new
        {
            amount,
            unit,
            category = item.GetProperty("category").GetString(),
            expiryDate = item.GetProperty("expiryDate").GetString(),
            reason,
            version = item.GetProperty("version").GetUInt32(),
        });

    private static async Task<List<(string? Name, decimal Quantity)>> PantryAsync(ApiClient client) =>
        [.. (await client.GetJsonAsync("/api/v1/pantry-items")).EnumerateArray()
            .Select(i => (i.GetProperty("ingredientName").GetString(), i.GetProperty("quantity").GetDecimal()))];

    private static async Task<JsonElement> PreviewAsync(ApiClient client, string recipe, int servings) =>
        await client.GetJsonAsync($"/api/v1/recipes/{SeedData.Recipes.Recipes.Single(r => r.Name == recipe).Id}/cooking-preview?servings={servings}");

    // The confirmation screen's defaults: the smaller of required and available.
    private static object CookingBody(Guid requestId, string recipe, int servings, JsonElement preview) => new
    {
        requestId,
        recipeId = SeedData.Recipes.Recipes.Single(r => r.Name == recipe).Id,
        servings,
        used = preview.GetProperty("lines").EnumerateArray()
            .Select(l => new { ingredientId = l.GetProperty("ingredientId").GetGuid(), amount = l.GetProperty("defaultUsed").GetDecimal() }),
    };

    private static JsonElement Line(JsonElement element, string ingredient) =>
        element.GetProperty("lines").EnumerateArray().Single(l => l.GetProperty("ingredientName").GetString() == ingredient);

    private static List<string?> Strings(JsonElement element, string property) =>
        [.. element.GetProperty(property).EnumerateArray().Select(s => s.GetString())];

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

    private static async Task<string> RawAsync(HttpResponseMessage response) =>
        await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

    private static string WithoutRequestIds(JsonElement problem) =>
        JsonSerializer.Serialize(problem.EnumerateObject().Where(p => p.Name is not ("correlationId" or "traceId")).ToDictionary(p => p.Name, p => p.Value));
}
