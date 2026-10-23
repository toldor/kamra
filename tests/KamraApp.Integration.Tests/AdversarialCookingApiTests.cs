using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KamraApp.Infrastructure.Persistence;
using KamraApp.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KamraApp.Integration.Tests;

[Collection("api")]
public class AdversarialCookingApiTests(KamraApiFactory factory)
{
    private static DateOnly Today => KamraApp.Application.Common.Today.Of(TimeProvider.System);
    private static Guid RecipeId(string name) => SeedData.Recipes.Recipes.Single(r => r.Name == name).Id;

    // 1. Modellezett támadás: Párhuzamos kéréshullám (5 egyidejű kérés) pontosan ugyanazzal a requestId-vel a kettős levonás vagy holtpont előidézésére.
    // Várt viselkedés: Pontosan 1 kérés kap 201 Created státuszt, a többi 4 pedig 200 OK választ ad azonos választörzzsel; a készletből a levonás csak egyszer hajtódik végre.
    [Fact]
    public async Task Idempotency_FiveConcurrentRequestsWithSameRequestId_DeductsOnceAndReturnsSameBody()
    {
        var client = await ApiClient.SignedInAsync(factory);
        await AddAsync(client, "paradicsom", 1000, "g", 3);
        var requestId = Guid.CreateVersion7();
        var preview = await PreviewAsync(client, "Lecsó", 4);
        var body = Body(requestId, "Lecsó", 4, Used(preview));

        var tasks = Enumerable.Range(0, 5).Select(_ => client.PostAsync("/api/v1/cookings", body)).ToList();
        var responses = await Task.WhenAll(tasks);

        responses.Count(r => r.StatusCode == HttpStatusCode.Created).Should().Be(1);
        responses.Count(r => r.StatusCode == HttpStatusCode.OK).Should().Be(4);

        var firstBody = await RawAsync(responses.First(r => r.StatusCode == HttpStatusCode.Created));
        foreach (var okResponse in responses.Where(r => r.StatusCode == HttpStatusCode.OK))
        {
            (await RawAsync(okResponse)).Should().Be(firstBody);
        }

        (await PantryAsync(client)).Should().Contain(("paradicsom", 700m));
        (await QuantitiesMatchTheLogAsync(client)).Should().BeTrue();
    }

    // 2. Modellezett támadás: Replay és módosítási kísérlet – a sikeres főzés után a kliens ugyanazt a requestId-t küldi módosított mennyiséggel vagy más háztartás requestId-jével.
    // Várt viselkedés: Ugyanazzal a requestId-vel módosított tartalom esetén 409 Conflict és IDEMPOTENCY_CONFLICT; másik háztartás által használt requestId esetén független 201
    // Created válasz saját főzéssel.
    [Fact]
    public async Task Idempotency_ReplayWithTamperedAmountsOrCrossHousehold_EnforcesIsolationAndRejectsTampering()
    {
        var victim = await ApiClient.SignedInAsync(factory);
        var attacker = await ApiClient.SignedInAsync(factory);
        await AddAsync(victim, "paradicsom", 500, "g", 3);
        await AddAsync(attacker, "paradicsom", 500, "g", 3);

        var requestId = Guid.CreateVersion7();
        var victimPreview = await PreviewAsync(victim, "Lecsó", 4);
        var victimUsed = Used(victimPreview);

        var firstResponse = await victim.PostAsync("/api/v1/cookings", Body(requestId, "Lecsó", 4, victimUsed));
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Módosított mennyiséggel küldött ismétlés ugyanattól a felhasználótól -> 409 IDEMPOTENCY_CONFLICT
        var tamperedUsed = victimUsed.Select(u => u with { Amount = u.Amount + 10m });
        var tamperedResponse = await victim.PostAsync("/api/v1/cookings", Body(requestId, "Lecsó", 4, tamperedUsed));
        tamperedResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ApiClient.ProblemAsync(tamperedResponse)).GetProperty("code").GetString().Should().Be("IDEMPOTENCY_CONFLICT");

        // Másik háztartás ugyanazzal a requestId-vel próbál főzni -> független 201 Created saját főzéssel
        var attackerResponse = await attacker.PostAsync("/api/v1/cookings", Body(requestId, "Lecsó", 4, Used(await PreviewAsync(attacker, "Lecsó", 4))));
        attackerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        (await IdAsync(attackerResponse)).Should().NotBe(await IdAsync(firstResponse));
    }

    // 3. Modellezett támadás: Párhuzamos versenyhelyzet kiváltása főzés (POST /cookings) és kézi készletcsökkentés (PUT /pantry-items/{id}) között ugyanarra a készlettételre.
    // Várt viselkedés: Az xmin optimista zárolás miatt nem történhet piszkos felülírás: az egyik kérés sikeres, a másik 409 Conflict (PANTRY_ITEM_MODIFIED vagy INSUFFICIENT_STOCK),
    // a készlet sosem negatív és megegyezik a loggal.
    [Fact]
    public async Task Concurrency_ConcurrentCookingAndManualReduction_MaintainsPantryIntegrityWithoutDataLoss()
    {
        var client = await ApiClient.SignedInAsync(factory);
        await AddAsync(client, "paradicsom", 400, "g", 3);
        var pantryItem = (await client.GetJsonAsync("/api/v1/pantry-items")).EnumerateArray().Single(i => i.GetProperty("ingredientName").GetString() == "paradicsom");
        var itemId = pantryItem.GetProperty("id").GetGuid();
        var version = pantryItem.GetProperty("version").GetUInt32();

        var preview = await PreviewAsync(client, "Lecsó", 4);
        var cookingBody = Body(Guid.CreateVersion7(), "Lecsó", 4, TomatoOnly(preview, 300));
        var manualReduction = new { amount = 200m, unit = "g", category = "vegetables", reason = "consumed", version };

        var cookingTask = client.PostAsync("/api/v1/cookings", cookingBody);
        var manualTask = client.Http.PutAsJsonAsync($"/api/v1/pantry-items/{itemId}", manualReduction, TestContext.Current.CancellationToken);

        var responses = await Task.WhenAll(cookingTask, manualTask);
        var cookingResponse = responses[0];
        var manualResponse = responses[1];

        var statusCodes = new[] { cookingResponse.StatusCode, manualResponse.StatusCode };
        statusCodes.Should().Contain(HttpStatusCode.Conflict);

        (await QuantitiesMatchTheLogAsync(client)).Should().BeTrue();
        var finalPantry = await PantryAsync(client);
        finalPantry.All(p => p.Quantity >= 0).Should().BeTrue();
    }

    // 4. Modellezett támadás: Készlet-túllépés előidézése több készlettétel esetén (a kért mennyiség meghaladja a rendelkezésre álló tételek összegét).
    // Várt viselkedés: 409 Conflict, code: INSUFFICIENT_STOCK, a válaszban a friss készletadatokkal, levonás nélkül és sértetlen napló-invariánssal.
    [Fact]
    public async Task Stock_ExceedingTotalAcrossMultipleItems_ReturnsInsufficientStockWithoutDeduction()
    {
        var client = await ApiClient.SignedInAsync(factory);
        await AddAsync(client, "paradicsom", 100, "g", 1);
        await AddAsync(client, "paradicsom", 150, "g", 5);

        var preview = await PreviewAsync(client, "Lecsó", 4);
        var used = TomatoOnly(preview, 250.001m); // 250g van készleten összesen, 250.001-et próbál felhasználni

        var response = await client.PostAsync("/api/v1/cookings", Body(Guid.CreateVersion7(), "Lecsó", 4, used));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await ApiClient.ProblemAsync(response);
        problem.GetProperty("code").GetString().Should().Be("INSUFFICIENT_STOCK");
        var available = problem.GetProperty("available").EnumerateArray().Single(a => a.GetProperty("ingredientName").GetString() == "paradicsom");
        available.GetProperty("amount").GetDecimal().Should().Be(250m);

        (await PantryAsync(client)).Should().BeEquivalentTo([("paradicsom", 100m), ("paradicsom", 150m)]);
        (await QuantitiesMatchTheLogAsync(client)).Should().BeTrue();
    }

    // 5. Modellezett támadás: Szélsőértékek és tiltott adagszámok küldése az előnézet és a megerősítés végpontjára (0, -1, 101, int.MaxValue).
    // Várt viselkedés: 400 Bad Request és VALIDATION_FAILED, a hibás 'servings' mező megjelölésével az 'errors' objektumban.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(int.MaxValue)]
    public async Task Boundaries_ServingsOutOfRange_ReturnsBadRequestWithValidationFailed(int invalidServings)
    {
        var client = await ApiClient.SignedInAsync(factory);

        var preview = await client.Http.GetAsync($"/api/v1/recipes/{RecipeId("Lecsó")}/cooking-preview?servings={invalidServings}", TestContext.Current.CancellationToken);
        preview.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var previewProblem = await ApiClient.ProblemAsync(preview);
        previewProblem.GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
        previewProblem.GetProperty("errors").TryGetProperty("servings", out _).Should().BeTrue();

        var cooking = await client.PostAsync("/api/v1/cookings",
            new { requestId = Guid.CreateVersion7(), recipeId = RecipeId("Lecsó"), servings = invalidServings, used = Array.Empty<object>() });
        cooking.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var cookingProblem = await ApiClient.ProblemAsync(cooking);
        cookingProblem.GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
        cookingProblem.GetProperty("errors").TryGetProperty("servings", out _).Should().BeTrue();
    }

    // 6. Modellezett támadás: Érvénytelen mennyiségek beküldése a felhasznált hozzávalók listájában (negatív, 100 000 fölötti, túl sok tizedesjegy).
    // Várt viselkedés: 400 Bad Request és VALIDATION_FAILED a 'used' mezőre; semmilyen készletmódosítás nem futhat le.
    [Theory]
    [InlineData(-0.001)]
    [InlineData(100000.001)]
    [InlineData(1.0001)] // Alapegységben több mint 3 tizedesjegy
    public async Task Boundaries_AmountInvalidOrTooFine_ReturnsBadRequestForUsedField(decimal invalidAmount)
    {
        var client = await ApiClient.SignedInAsync(factory);
        await AddAsync(client, "paradicsom", 500, "g", 3);
        var preview = await PreviewAsync(client, "Lecsó", 4);
        var used = TomatoOnly(preview, invalidAmount);

        var response = await client.PostAsync("/api/v1/cookings", Body(Guid.CreateVersion7(), "Lecsó", 4, used));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await ApiClient.ProblemAsync(response);
        problem.GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
        problem.GetProperty("errors").TryGetProperty("used", out _).Should().BeTrue();
        (await PantryAsync(client)).Should().Equal(("paradicsom", 500m));
    }

    // 7. Modellezett támadás: Manipulált 'used' lista beküldése: duplikált ingredientId, nem levonható alaphozzávaló (só) vagy ismeretlen ingredientId a törzsben.
    // Várt viselkedés: 400 Bad Request, code: VALIDATION_FAILED a 'used' mező hibájával; a kérés nem jut el a domain levonásig.
    [Theory]
    [InlineData("duplicate")]
    [InlineData("staple")]
    [InlineData("unknown")]
    public async Task Boundaries_MalformedUsedList_ReturnsBadRequestWithFieldValidation(string attackVariant)
    {
        var client = await ApiClient.SignedInAsync(factory);
        await AddAsync(client, "paradicsom", 500, "g", 3);
        var preview = await PreviewAsync(client, "Lecsó", 4);
        var used = Used(preview);

        List<UsedAmount> attackedUsed = attackVariant switch
        {
            "duplicate" => [.. used, used.First()],
            "staple" => [.. used, new UsedAmount(SeedData.Ingredients.Single(i => i.SeedKey == "so").Id, 5m)],
            _ => [.. used, new UsedAmount(Guid.CreateVersion7(), 10m)],
        };

        var response = await client.PostAsync("/api/v1/cookings", Body(Guid.CreateVersion7(), "Lecsó", 4, attackedUsed));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await ApiClient.ProblemAsync(response);
        problem.GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
        problem.GetProperty("errors").TryGetProperty("used", out _).Should().BeTrue();
    }

    // 8. Modellezett támadás: Információszivárgási próba hibák kiváltásakor (nem létező recept, sémasértő JSON, adatbáziskényszer-ütközés).
    // Várt viselkedés: A ProblemDetails válaszok (400, 404, 409) egyike sem tartalmazhat belső implementációs részletet (stack trace, fájlelérési út, Npgsql, EF Core vagy Postgres
    // hibaüzenet).
    [Fact]
    public async Task Security_ErrorResponses_DoNotLeakStackTracesDatabaseOrInternalDetails()
    {
        var client = await ApiClient.SignedInAsync(factory);

        var badJson = await client.Http.PostAsync("/api/v1/cookings",
            new StringContent("{ invalid_json }", System.Text.Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);
        var notFound = await client.Http.GetAsync($"/api/v1/recipes/{Guid.CreateVersion7()}/cooking-preview", TestContext.Current.CancellationToken);
        var emptyGuid = await client.PostAsync("/api/v1/cookings",
            new { requestId = Guid.Empty, recipeId = Guid.Empty, servings = 0, used = Array.Empty<object>() });

        foreach (var response in new[] { badJson, notFound, emptyGuid })
        {
            var rawBody = await RawAsync(response);
            rawBody.Should().NotContain("StackTrace")
                .And.NotContain("Npgsql")
                .And.NotContain("PostgresException")
                .And.NotContain("EntityFrameworkCore")
                .And.NotContain("KamraApp.Domain")
                .And.NotContain(".cs:");

            var problem = await ApiClient.ProblemAsync(response);
            problem.TryGetProperty("correlationId", out var correlationId).Should().BeTrue();
            correlationId.GetString().Should().NotBeNullOrWhiteSpace();
        }
    }

    private sealed record UsedAmount(Guid IngredientId, decimal Amount);

    private static object Body(Guid requestId, string recipe, int servings, IEnumerable<UsedAmount> used) =>
        new { requestId, recipeId = RecipeId(recipe), servings, used = used.Select(u => new { ingredientId = u.IngredientId, amount = u.Amount }) };

    private static List<UsedAmount> Used(JsonElement preview) =>
        [.. preview.GetProperty("lines").EnumerateArray()
            .Select(l => new UsedAmount(l.GetProperty("ingredientId").GetGuid(), l.GetProperty("defaultUsed").GetDecimal()))];

    private static List<UsedAmount> TomatoOnly(JsonElement preview, decimal grams) =>
        [.. preview.GetProperty("lines").EnumerateArray().Select(l => new UsedAmount(l.GetProperty("ingredientId").GetGuid(),
            l.GetProperty("ingredientName").GetString() == "paradicsom" ? grams : 0))];

    private static async Task<JsonElement> PreviewAsync(ApiClient client, string recipe, int servings) =>
        await client.GetJsonAsync($"/api/v1/recipes/{RecipeId(recipe)}/cooking-preview?servings={servings}");

    private static async Task<string> RawAsync(HttpResponseMessage response) =>
        await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

    private static async Task<Guid> IdAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken)).GetProperty("cookingId").GetGuid();

    private static async Task<List<(string? Name, decimal Quantity)>> PantryAsync(ApiClient client) =>
        [.. (await client.GetJsonAsync("/api/v1/pantry-items")).EnumerateArray()
            .Select(i => (i.GetProperty("ingredientName").GetString(), i.GetProperty("quantity").GetDecimal()))];

    private static async Task AddAsync(ApiClient client, string ingredient, decimal amount, string unit, int daysToExpiry)
    {
        var ingredientId = (await client.GetJsonAsync($"/api/v1/ingredients?search={Uri.EscapeDataString(ingredient)}")).EnumerateArray()
            .Single(i => i.GetProperty("name").GetString() == ingredient).GetProperty("id").GetGuid();
        var response = await client.Http.PostAsJsonAsync("/api/v1/pantry-items",
            new { ingredientId, amount, unit, expiryDate = Today.AddDays(daysToExpiry) }, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task<bool> QuantitiesMatchTheLogAsync(ApiClient client)
    {
        var householdId = (await client.GetJsonAsync("/api/v1/auth/me")).GetProperty("householdId").GetGuid();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KamraDbContext>();
        return !await db.PantryItems.AnyAsync(i => i.HouseholdId == householdId && (i.Quantity < 0
            || db.StockMovements.Where(m => m.PantryItemId == i.Id).Sum(m => m.Delta) != i.Quantity), TestContext.Current.CancellationToken);
    }
}
