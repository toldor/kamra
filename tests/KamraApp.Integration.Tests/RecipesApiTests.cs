using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KamraApp.Infrastructure.Persistence;
using KamraApp.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KamraApp.Integration.Tests;

// US-3 own recipes, archiving and own ingredients (T3 mini-spec N2-N8) through the API on a real PostgreSQL:
// the hand-calculated examples P17 and P18 (kamra_okt22_terv.md, 5.), D-c, household isolation (QA-1),
// concurrent requests, the migration's schema, sign-in and antiforgery.
[Collection("api")]
public class RecipesApiTests(KamraApiFactory factory)
{
    private const string Kakao = "Zabtejes kakaó";

    private static DateOnly Today => KamraApp.Application.Common.Today.Of(TimeProvider.System);

    [Fact]
    public async Task P17_an_own_recipe_with_an_own_ingredient_is_listed_and_makeable()
    {
        var a = await ApiClient.SignedInAsync(factory);
        var (recipeId, _) = await SetUpKakaoAsync(a);

        var row = (await RecipesAsync(a)).Single(r => r.GetProperty("id").GetGuid() == recipeId);
        (row.GetProperty("name").GetString(), row.GetProperty("servings").GetInt32(), row.GetProperty("own").GetBoolean())
            .Should().Be((Kakao, 1, true));
        Strings(row, "ingredients").Should().Equal("zabtej");
        (await RecipesAsync(a)).Single(r => r.GetProperty("name").GetString() == "Lecsó").GetProperty("own").GetBoolean().Should().BeFalse();
        Names(await a.GetJsonAsync("/api/v1/recommendations"), "makeable").Should().Contain(Kakao);
    }

    [Theory]
    [InlineData("Zabtej ")]   // the household's own name, normalized
    [InlineData("tej")]       // a system name
    [InlineData(" TEJ")]
    public async Task P17_a_name_already_visible_to_the_household_is_taken(string name)
    {
        var a = await ApiClient.SignedInAsync(factory);
        await SetUpKakaoAsync(a);

        var response = await CreateIngredientAsync(a, name, "volume", "beverages");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ApiClient.ProblemAsync(response)).GetProperty("code").GetString().Should().Be("INGREDIENT_NAME_TAKEN");
    }

    [Fact]
    public async Task P17_another_household_sees_none_of_it_and_may_use_the_same_name()
    {
        var a = await ApiClient.SignedInAsync(factory);
        var (recipeId, zabtejId) = await SetUpKakaoAsync(a);
        var b = await ApiClient.SignedInAsync(factory);

        (await b.GetJsonAsync("/api/v1/ingredients?search=zabtej")).GetArrayLength().Should().Be(0);
        var withForeign = await CreateRecipeAsync(b, "Lopott kakaó", 1, (zabtejId, 250, "ml"));
        withForeign.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ApiClient.ProblemAsync(withForeign)).GetProperty("code").GetString().Should().Be("INGREDIENT_NOT_FOUND");
        (await RecipesAsync(b)).Should().NotContain(r => r.GetProperty("id").GetGuid() == recipeId);
        var preview = await b.Http.GetAsync($"/api/v1/recipes/{recipeId}/cooking-preview", TestContext.Current.CancellationToken);
        preview.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ApiClient.ProblemAsync(preview)).GetProperty("code").GetString().Should().Be("RECIPE_NOT_FOUND");

        (await CreateIngredientAsync(b, "zabtej", "volume", "beverages")).StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task P18a_an_archived_recipe_is_gone_cannot_be_cooked_again_and_keeps_its_cooking()
    {
        var a = await ApiClient.SignedInAsync(factory);
        var (recipeId, zabtejId) = await SetUpKakaoAsync(a);
        var cooking = CookingBody(Guid.CreateVersion7(), recipeId, await a.GetJsonAsync($"/api/v1/recipes/{recipeId}/cooking-preview"));
        var cooked = await a.PostAsync("/api/v1/cookings", cooking);
        cooked.StatusCode.Should().Be(HttpStatusCode.Created);
        var firstBody = await RawAsync(cooked);

        (await ArchiveAsync(a, recipeId)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        Names(await a.GetJsonAsync("/api/v1/recommendations"), "makeable").Should().NotContain(Kakao);
        (await RecipesAsync(a)).Should().NotContain(r => r.GetProperty("id").GetGuid() == recipeId);
        var preview = await a.Http.GetAsync($"/api/v1/recipes/{recipeId}/cooking-preview", TestContext.Current.CancellationToken);
        await ShouldBeArchivedAsync(preview);
        await ShouldBeArchivedAsync(await a.PostAsync("/api/v1/cookings", CookingBody(Guid.CreateVersion7(), recipeId, zabtejId, 250)));

        // D-c: the earlier request is still answered from the stored cooking, byte for byte.
        var repeated = await a.PostAsync("/api/v1/cookings", cooking);
        repeated.StatusCode.Should().Be(HttpStatusCode.OK);
        (await RawAsync(repeated)).Should().Be(firstBody);
        (await PantryAsync(a)).Should().Equal((zabtejId, 750m));
        (await CookingsAndMovementsAsync(recipeId)).Should().Be((1, 1));
    }

    [Fact]
    public async Task P18b_archiving_a_system_recipe_affects_only_the_own_household()
    {
        var a = await ApiClient.SignedInAsync(factory);
        var b = await ApiClient.SignedInAsync(factory);
        await AddP7StockAsync(a);
        await AddP7StockAsync(b);

        (await ArchiveAsync(a, RecipeId("Lecsó"))).StatusCode.Should().Be(HttpStatusCode.NoContent);

        Names(await a.GetJsonAsync("/api/v1/recommendations"), "makeable")[0].Should().Be("Paprikás krumpli");
        Names(await b.GetJsonAsync("/api/v1/recommendations"), "makeable")[0].Should().Be("Lecsó");
        (await RecipesAsync(a)).Should().NotContain(r => r.GetProperty("name").GetString() == "Lecsó");
        (await RecipesAsync(b)).Should().Contain(r => r.GetProperty("name").GetString() == "Lecsó");
    }

    [Fact]
    public async Task Archiving_again_answers_204_and_keeps_the_first_time()
    {
        var a = await ApiClient.SignedInAsync(factory);
        var recipeId = RecipeId("Rántotta");
        (await ArchiveAsync(a, recipeId)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var first = await ArchivedAtAsync(a, recipeId);

        (await ArchiveAsync(a, recipeId)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await ArchivedAtAsync(a, recipeId)).Should().Equal(first);
    }

    [Fact]
    public async Task Concurrent_archivings_all_answer_204_with_one_row()
    {
        for (var round = 0; round < 5; round++)
        {
            var a = await ApiClient.SignedInAsync(factory);
            var recipeId = RecipeId("Palacsinta");

            var responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => ArchiveAsync(a, recipeId)));

            responses.Should().AllSatisfy(r => r.StatusCode.Should().Be(HttpStatusCode.NoContent));
            (await ArchivedAtAsync(a, recipeId)).Should().HaveCount(1);
        }
    }

    [Fact]
    public async Task Another_households_own_recipe_or_an_unknown_one_cannot_be_archived()
    {
        var a = await ApiClient.SignedInAsync(factory);
        var (recipeId, _) = await SetUpKakaoAsync(a);
        var b = await ApiClient.SignedInAsync(factory);

        foreach (var id in new[] { recipeId, Guid.CreateVersion7() })
        {
            var response = await ArchiveAsync(b, id);
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await ApiClient.ProblemAsync(response)).GetProperty("code").GetString().Should().Be("RECIPE_NOT_FOUND");
        }

        (await ArchivedAtAsync(b, recipeId)).Should().BeEmpty();
        Names(await a.GetJsonAsync("/api/v1/recommendations"), "makeable").Should().Contain(Kakao);
    }

    [Fact]
    public async Task A_retired_ingredient_cannot_go_in_a_new_recipe()
    {
        var a = await ApiClient.SignedInAsync(factory);
        var id = await IdAsync(await CreateIngredientAsync(a, "régi zabtej", "volume", "beverages"));
        await ExecuteSqlAsync($"""UPDATE "Ingredients" SET "RetiredAt" = now() WHERE "Id" = {id}""");

        var response = await CreateRecipeAsync(a, "Régi kakaó", 1, (id, 250, "ml"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ApiClient.ProblemAsync(response)).GetProperty("code").GetString().Should().Be("INGREDIENT_NOT_FOUND");
    }

    [Fact]
    public async Task Concurrent_own_ingredients_with_the_same_name_create_one()
    {
        for (var round = 0; round < 5; round++)
        {
            var a = await ApiClient.SignedInAsync(factory);

            var responses = await Task.WhenAll(CreateIngredientAsync(a, "mandulatej", "volume", "beverages"),
                CreateIngredientAsync(a, "Mandulatej", "volume", "beverages"));

            responses.Select(r => r.StatusCode).Should().BeEquivalentTo([HttpStatusCode.Created, HttpStatusCode.Conflict]);
            (await ApiClient.ProblemAsync(responses.Single(r => r.StatusCode == HttpStatusCode.Conflict)))
                .GetProperty("code").GetString().Should().Be("INGREDIENT_NAME_TAKEN");
        }
    }

    [Fact]
    public async Task A_row_error_names_the_row_and_the_field()
    {
        var a = await ApiClient.SignedInAsync(factory);

        var response = await a.PostAsync("/api/v1/recipes", new
        {
            name = "Kakaó",
            servings = 2,
            ingredients = new object[]
            {
                new { ingredientId = await IngredientIdAsync(a, "tej"), amount = 5, unit = "dl" },
                new { amount = 2, unit = "dkg" },
            },
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await ApiClient.ProblemAsync(response);
        problem.GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
        problem.GetProperty("errors").EnumerateObject().Select(e => e.Name).Should().Equal("ingredients[1].ingredientId");
    }

    [Fact]
    public async Task The_migration_keys_archives_by_household_and_recipe_and_deletes_them_with_the_household()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KamraDbContext>();

        var constraints = await db.Database.SqlQuery<string>(
                $"""SELECT pg_get_constraintdef(oid) AS "Value" FROM pg_constraint WHERE conrelid = '"RecipeArchives"'::regclass AND contype IN ('p', 'f') ORDER BY contype, conname""")
            .ToListAsync(TestContext.Current.CancellationToken);

        constraints.Should().BeEquivalentTo(
            "FOREIGN KEY (\"HouseholdId\") REFERENCES \"Households\"(\"Id\") ON DELETE CASCADE",
            "FOREIGN KEY (\"RecipeId\") REFERENCES \"Recipes\"(\"Id\")",
            "PRIMARY KEY (\"HouseholdId\", \"RecipeId\")");
    }

    [Theory]
    [InlineData("GET", "/api/v1/recipes")]
    [InlineData("POST", "/api/v1/recipes")]
    [InlineData("POST", "/api/v1/recipes/0199c9a0-0000-7000-8000-000000000001/archive")]
    [InlineData("POST", "/api/v1/ingredients")]
    public async Task Recipe_endpoints_require_sign_in(string method, string path)
    {
        var anonymous = await ApiClient.CreateAsync(factory);

        var response = await anonymous.Http.SendAsync(new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = method == "GET" ? null : JsonContent.Create(new { }),
        }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ApiClient.ProblemAsync(response)).GetProperty("code").GetString().Should().Be("UNAUTHENTICATED");
    }

    [Theory]
    [InlineData("/api/v1/recipes")]
    [InlineData("/api/v1/recipes/{lecso}/archive")]
    [InlineData("/api/v1/ingredients")]
    public async Task Writes_without_the_antiforgery_token_are_rejected(string path)
    {
        var a = await ApiClient.SignedInAsync(factory);
        a.Http.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");

        var response = await a.PostAsync(path.Replace("{lecso}", RecipeId("Lecsó").ToString(), StringComparison.Ordinal),
            new { name = "zabtej", dimension = "volume", defaultCategory = "beverages" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ApiClient.ProblemAsync(response)).GetProperty("code").GetString().Should().Be("ANTIFORGERY_TOKEN_INVALID");
    }

    // ---- helpers ----

    // P17: own "zabtej" (volume, drink), own recipe "Zabtejes kakaó" for 1 serving with 250 ml of it, 1 l in stock.
    private static async Task<(Guid RecipeId, Guid ZabtejId)> SetUpKakaoAsync(ApiClient client)
    {
        var created = await CreateIngredientAsync(client, "zabtej", "volume", "beverages");
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var zabtejId = await IdAsync(created);
        var recipe = await CreateRecipeAsync(client, Kakao, 1, (zabtejId, 250, "ml"));
        recipe.StatusCode.Should().Be(HttpStatusCode.Created);
        (await client.PostAsync("/api/v1/pantry-items", new { ingredientId = zabtejId, amount = 1, unit = "l", expiryDate = Today.AddDays(7) }))
            .EnsureSuccessStatusCode();
        return (await IdAsync(recipe), zabtejId);
    }

    private static Task<HttpResponseMessage> CreateIngredientAsync(ApiClient client, string name, string dimension, string category) =>
        client.PostAsync("/api/v1/ingredients", new { name, dimension, defaultCategory = category });

    private static Task<HttpResponseMessage> CreateRecipeAsync(ApiClient client, string name, int servings,
        params (Guid IngredientId, decimal Amount, string Unit)[] rows) =>
        client.PostAsync("/api/v1/recipes", new
        {
            name,
            servings,
            instructions = "Melegítsd fel, és keverd el.",
            ingredients = rows.Select(r => new { ingredientId = r.IngredientId, amount = r.Amount, unit = r.Unit }),
        });

    private static Task<HttpResponseMessage> ArchiveAsync(ApiClient client, Guid recipeId) =>
        client.PostAsync($"/api/v1/recipes/{recipeId}/archive");

    private static async Task ShouldBeArchivedAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ApiClient.ProblemAsync(response)).GetProperty("code").GetString().Should().Be("RECIPE_ARCHIVED");
    }

    // The confirmation screen's defaults.
    private static object CookingBody(Guid requestId, Guid recipeId, JsonElement preview) => new
    {
        requestId,
        recipeId,
        servings = preview.GetProperty("servings").GetInt32(),
        used = preview.GetProperty("lines").EnumerateArray()
            .Select(l => new { ingredientId = l.GetProperty("ingredientId").GetGuid(), amount = l.GetProperty("defaultUsed").GetDecimal() }),
    };

    // A new cooking of the P17 recipe (its only line is zabtej), written without a preview.
    private static object CookingBody(Guid requestId, Guid recipeId, Guid zabtejId, decimal amount) => new
    {
        requestId,
        recipeId,
        servings = 1,
        used = new[] { new { ingredientId = zabtejId, amount } },
    };

    private static async Task<List<JsonElement>> RecipesAsync(ApiClient client) =>
        [.. (await client.GetJsonAsync("/api/v1/recipes")).EnumerateArray()];

    private static List<string?> Names(JsonElement result, string list) =>
        [.. result.GetProperty(list).EnumerateArray().Select(r => r.GetProperty("name").GetString())];

    private static List<string?> Strings(JsonElement element, string property) =>
        [.. element.GetProperty(property).EnumerateArray().Select(s => s.GetString())];

    private static Guid RecipeId(string name) => SeedData.Recipes.Recipes.Single(r => r.Name == name).Id;

    private static async Task<Guid> IdAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("id").GetGuid();

    private static async Task<string> RawAsync(HttpResponseMessage response) =>
        await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

    private static async Task<Guid> IngredientIdAsync(ApiClient client, string name) =>
        (await client.GetJsonAsync($"/api/v1/ingredients?search={Uri.EscapeDataString(name)}")).EnumerateArray()
            .Single(i => i.GetProperty("name").GetString() == name).GetProperty("id").GetGuid();

    private static async Task<List<(Guid IngredientId, decimal Quantity)>> PantryAsync(ApiClient client) =>
        [.. (await client.GetJsonAsync("/api/v1/pantry-items")).EnumerateArray()
            .Select(i => (i.GetProperty("ingredientId").GetGuid(), i.GetProperty("quantity").GetDecimal()))];

    private static async Task AddP7StockAsync(ApiClient client)
    {
        foreach (var (name, amount, unit, days) in new[]
        {
            ("paprika", 500m, "g", 2), ("paradicsom", 300m, "g", 1), ("vöröshagyma", 300m, "g", 10), ("étolaj", 1m, "l", 90),
            ("burgonya", 1m, "kg", 30), ("kolbász", 150m, "g", 7), ("őrölt pirospaprika", 100m, "g", 90),
        })
        {
            (await client.PostAsync("/api/v1/pantry-items",
                new { ingredientId = await IngredientIdAsync(client, name), amount, unit, expiryDate = Today.AddDays(days) })).EnsureSuccessStatusCode();
        }
    }

    private async Task<List<DateTimeOffset>> ArchivedAtAsync(ApiClient client, Guid recipeId)
    {
        var householdId = (await client.GetJsonAsync("/api/v1/auth/me")).GetProperty("householdId").GetGuid();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KamraDbContext>();
        return await db.Database.SqlQuery<DateTimeOffset>(
                $"""SELECT "ArchivedAt" AS "Value" FROM "RecipeArchives" WHERE "HouseholdId" = {householdId} AND "RecipeId" = {recipeId}""")
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    private async Task<(int Cookings, int Movements)> CookingsAndMovementsAsync(Guid recipeId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KamraDbContext>();
        var cookings = await db.Cookings.Where(c => c.RecipeId == recipeId).Select(c => c.Id).ToListAsync(TestContext.Current.CancellationToken);
        var movements = await db.StockMovements.CountAsync(m => m.CookingId != null && cookings.Contains(m.CookingId.Value), TestContext.Current.CancellationToken);
        return (cookings.Count, movements);
    }

    private async Task ExecuteSqlAsync(FormattableString sql)
    {
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<KamraDbContext>().Database.ExecuteSqlAsync(sql, TestContext.Current.CancellationToken);
    }
}
