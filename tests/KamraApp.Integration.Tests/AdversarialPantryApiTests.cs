using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;

namespace KamraApp.Integration.Tests;

// Adversarial tests written by Antigravity (Gemini 3.1 Pro) against the US-1 pantry API
// (docs/07_ai/review_prompts.md, template 2). Integrated verbatim apart from compile fixes: the
// duplicated FluentAssertions using is dropped (global using) and every call gets the test's
// cancellation token, as in the other test classes.
[Collection("api")]
public class AdversarialPantryApiTests(KamraApiFactory factory)
{
    // 1. Modellezett támadás: A támadó megpróbálja lekérdezni vagy módosítani egy másik háztartás (áldozat) létező készlettételét, hogy illetéktelenül adathoz jusson.
    // Várt viselkedés: Az API 404 PANTRY_ITEM_NOT_FOUND hibával válaszol, pontosan úgy, mintha egy nem létező azonosítót kért volna, így nem szivárog ki a tétel létezése, és a listában sem jelenik meg.
    [Fact]
    public async Task IDOR_OtherHouseholdsData_IsIndistinguishableFromNotFound()
    {
        var victim = await ApiClient.SignedInAsync(factory);
        var attacker = await ApiClient.SignedInAsync(factory);

        var victimItem = await ReadAsync(await AddAsync(victim, "tej", 1, "l"));
        var victimItemId = victimItem.GetProperty("id").GetGuid();
        var victimVersion = victimItem.GetProperty("version").GetUInt32();

        var fakeId = Guid.CreateVersion7();
        var payload = new { amount = 0.5m, unit = "l", category = "dairy", reason = "consumed", version = victimVersion };

        var attackerUpdateReal = await attacker.Http.PutAsJsonAsync($"/api/v1/pantry-items/{victimItemId}", payload, TestContext.Current.CancellationToken);
        var attackerUpdateFake = await attacker.Http.PutAsJsonAsync($"/api/v1/pantry-items/{fakeId}", payload, TestContext.Current.CancellationToken);

        attackerUpdateReal.StatusCode.Should().Be(HttpStatusCode.NotFound);
        attackerUpdateFake.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var problemReal = await ApiClient.ProblemAsync(attackerUpdateReal);
        problemReal.GetProperty("code").GetString().Should().Be("PANTRY_ITEM_NOT_FOUND");

        var attackerList = (await attacker.GetJsonAsync("/api/v1/pantry-items")).EnumerateArray();
        attackerList.Should().BeEmpty();
    }

    // 2. Modellezett támadás: A kliens szándékosan hibás, negatív, túl nagy vagy szöveges típusú verziót küld (a Theory adatokkal), illetve egy korábbi érvényes verzióval próbálja felülírni a már módosított tételt (replay attack).
    // Várt viselkedés: A típus/érték hibákra 400 VALIDATION_FAILED, míg a lejárt verzió újrajátszására az optimista zárolás miatt 409 PANTRY_ITEM_MODIFIED a válasz.
    [Theory]
    [InlineData("""{ "amount": 1, "unit": "l", "category": "dairy", "version": -1 }""", "")]
    [InlineData("""{ "amount": 1, "unit": "l", "category": "dairy", "version": 4294967296 }""", "")]
    [InlineData("""{ "amount": 1, "unit": "l", "category": "dairy", "version": "egy" }""", "")]
    [InlineData("""{ "amount": 1, "unit": "l", "category": "dairy" }""", "version")] // hiányzó verzió
    public async Task VersionManipulation_InvalidTypesReturn400_AndReplayAttackReturns409(string payload, string expectedErrorField)
    {
        var client = await ApiClient.SignedInAsync(factory);
        var item = await ReadAsync(await AddAsync(client, "tej", 1, "l"));
        var itemId = item.GetProperty("id").GetGuid();
        var v1 = item.GetProperty("version").GetUInt32();

        // Érvénytelen típusú verziók tesztje (400)
        var badResponse = await client.Http.PutAsync($"/api/v1/pantry-items/{itemId}",
            new StringContent(payload, System.Text.Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);
        badResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await ApiClient.ProblemAsync(badResponse);
        if (!string.IsNullOrEmpty(expectedErrorField))
        {
            problem.GetProperty("errors").TryGetProperty(expectedErrorField, out _).Should().BeTrue();
        }

        // Replay attack: sikeres mentés után a régi verzió újraküldése (409)
        await client.Http.PutAsJsonAsync($"/api/v1/pantry-items/{itemId}",
            new { amount = 0.5m, unit = "l", category = "dairy", reason = "consumed", version = v1 }, TestContext.Current.CancellationToken);

        var replayResponse = await client.Http.PutAsJsonAsync($"/api/v1/pantry-items/{itemId}",
            new { amount = 0.1m, unit = "l", category = "dairy", reason = "consumed", version = v1 }, TestContext.Current.CancellationToken);

        replayResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ApiClient.ProblemAsync(replayResponse)).GetProperty("code").GetString().Should().Be("PANTRY_ITEM_MODIFIED");
    }

    // 3. Modellezett támadás: A támadó határértékeken kívüli (negatív, 0 alatti tört, túl nagy), tudományos formátumú, vagy stringként átadott számot próbál rögzíteni.
    // Várt viselkedés: Az API csak a 0.001 és 100 000 közötti értékeket fogadja el numerikus formátumban (a strict JSON miatt stringként sem); minden másra 400 VALIDATION_FAILED hibát ad, megelőzve az adatbázis túlcsordulását.
    [Theory]
    [InlineData("""{ "amount": 0.0001, "unit": "l", "category": "dairy" }""")]
    [InlineData("""{ "amount": 0, "unit": "l", "category": "dairy" }""")]
    [InlineData("""{ "amount": -1, "unit": "l", "category": "dairy" }""")]
    [InlineData("""{ "amount": 100000.001, "unit": "l", "category": "dairy" }""")]
    [InlineData("""{ "amount": 1e6, "unit": "l", "category": "dairy" }""")]
    [InlineData("""{ "amount": "100", "unit": "l", "category": "dairy" }""")]
    public async Task BoundaryValues_Amounts_AreStrictlyValidated(string payload)
    {
        var client = await ApiClient.SignedInAsync(factory);
        var ingredientId = await IngredientIdAsync(client, "tej");
        var body = payload.Replace("{ ", $$"""{ "ingredientId": "{{ingredientId}}", """, StringComparison.Ordinal);

        var response = await client.Http.PostAsync("/api/v1/pantry-items",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ApiClient.ProblemAsync(response)).GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
    }

    // 4. Modellezett támadás: A kliens az enum mezőkbe (kategória, egység) számokat, elgépelt / ismeretlen stringeket küld.
    // Várt viselkedés: A JSON options `Strict` beállítása és a domén-validáció miatt a számmal átadott enum vagy a hibás string biztonságosan 400 VALIDATION_FAILED-et okoz, a kérés nem jut el feldolgozásig.
    [Theory]
    [InlineData("""{ "amount": 1, "unit": 0, "category": "dairy" }""", "")] // Unit számként
    [InlineData("""{ "amount": 1, "unit": "l", "category": 99 }""", "")] // Kategória számként
    [InlineData("""{ "amount": 1, "unit": "ismeretlen", "category": "dairy" }""", "unit")]
    [InlineData("""{ "amount": 1, "unit": "l", "category": "alma" }""", "category")]
    public async Task InvalidTypeBinding_RejectsEnumsAsNumbersAndUnknownValues(string payload, string expectedErrorField)
    {
        var client = await ApiClient.SignedInAsync(factory);
        var ingredientId = await IngredientIdAsync(client, "tej");
        var body = payload.Replace("{ ", $$"""{ "ingredientId": "{{ingredientId}}", """, StringComparison.Ordinal);

        var response = await client.Http.PostAsync("/api/v1/pantry-items",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await ApiClient.ProblemAsync(response);
        problem.GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
        if (!string.IsNullOrEmpty(expectedErrorField))
        {
            problem.GetProperty("errors").TryGetProperty(expectedErrorField, out _).Should().BeTrue();
        }
    }

    // 5. Modellezett támadás: A támadó megpróbál ok nélkül csökkenteni egy készletet, módosításkor "added" okot megadni, vagy a dimenziót engedély nélkül megváltoztatni (pl. liter-t kilogramm-ra).
    // Várt viselkedés: A domén-invariánsok védik az integritást, ezért az érvénytelen változtatások 400 VALIDATION_FAILED hibát dobnak a kérdéses mező megjelölésével.
    [Theory]
    [InlineData(0.5, "l", null, "reason")] // csökkentés ok nélkül
    [InlineData(1.5, "l", "consumed", "reason")] // növelés csökkentés okkal
    [InlineData(0.5, "l", "added", "reason")] // 'added' ok használata PUT kérésnél
    [InlineData(1.0, "kg", "corrected", "unit")] // térfogatról (l) tömegre (kg) váltás
    public async Task ReasonBypass_AndCrossDimensionChanges_AreRejected(decimal amount, string unit, string? reason, string field)
    {
        var client = await ApiClient.SignedInAsync(factory);
        var item = await ReadAsync(await AddAsync(client, "tej", 1, "l"));
        var itemId = item.GetProperty("id").GetGuid();
        var version = item.GetProperty("version").GetUInt32();

        var response = await client.Http.PutAsJsonAsync($"/api/v1/pantry-items/{itemId}",
            new { amount, unit, category = "dairy", reason, version }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await ApiClient.ProblemAsync(response);
        problem.GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
        problem.GetProperty("errors").TryGetProperty(field, out _).Should().BeTrue();
    }

    // 6. Modellezett támadás: A támadó egy korábban 0-ra csökkentett (logikailag törölt) tételt próbál meg lekérdezni a listából, vagy újból szerkeszteni.
    // Várt viselkedés: A törölt elem frissítése 404 PANTRY_ITEM_NOT_FOUND hibával tér vissza, a listázásból pedig hiányzik, mert a repository csak a `Quantity > 0` tételeket adja vissza.
    [Fact]
    public async Task DepletedItems_BehaveAsDeleted_AndCannotBeQueriedOrUpdated()
    {
        var client = await ApiClient.SignedInAsync(factory);
        var item = await ReadAsync(await AddAsync(client, "tej", 1, "l"));
        var itemId = item.GetProperty("id").GetGuid();
        var version = item.GetProperty("version").GetUInt32();

        var deleteResponse = await client.Http.PutAsJsonAsync($"/api/v1/pantry-items/{itemId}",
            new { amount = 0m, unit = "l", category = "dairy", reason = "consumed", version }, TestContext.Current.CancellationToken);
        var newVersion = (await ReadAsync(deleteResponse)).GetProperty("version").GetUInt32();

        var updateResponse = await client.Http.PutAsJsonAsync($"/api/v1/pantry-items/{itemId}",
            new { amount = 1m, unit = "l", category = "dairy", reason = "corrected", version = newVersion }, TestContext.Current.CancellationToken);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ApiClient.ProblemAsync(updateResponse)).GetProperty("code").GetString().Should().Be("PANTRY_ITEM_NOT_FOUND");

        var list = (await client.GetJsonAsync("/api/v1/pantry-items")).EnumerateArray();
        list.Should().BeEmpty();
    }

    // 7. Modellezett támadás: A támadó szándékosan megszegett (invalid) formátumú JSON-t küld, hogy az 500-as hibát kikényszerítve rendszerszintű adatokhoz, pl. stack trace-hez jusson.
    // Várt viselkedés: Az API biztonságos RFC 7807 ProblemDetails választ ad 400-as státuszkóddal, ami garantáltan nem tartalmaz belső útvonalakat, kivételeket (Exception) vagy SQL utasításokat.
    [Fact]
    public async Task ErrorResponses_NeverLeakInternalData()
    {
        var client = await ApiClient.SignedInAsync(factory);

        var response = await client.Http.PostAsync("/api/v1/pantry-items",
            new StringContent("""{ "amount": 1, "unit": "l", "category": }""", System.Text.Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problemJson = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        problemJson.Should().NotContain("Exception");
        problemJson.Should().NotContain("StackTrace");
        problemJson.Should().NotContain("at KamraApp");
        problemJson.Should().NotContain("SELECT");

        var problem = JsonDocument.Parse(problemJson).RootElement;
        problem.GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
    }

    // 8. Modellezett támadás: A támadó Production (éles) környezetben kutat a rendszer belső dokumentációja (OpenAPI/Scalar) után, és próbálja kihasználni a CSP hiányosságait (inline scriptek).
    // Várt viselkedés: A fejlesztői API-leírások 404 Not Found választ adnak, és az API-kérések CSP fejléce szigorú marad (`default-src 'self'`), lazító `unsafe-inline` vagy `nonce` nélkül.
    [Fact]
    public async Task ProductionEnvironment_HidesDeveloperTools_AndEnforcesStrictCsp()
    {
        await using var prodFactory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        var prodClient = await ApiClient.SignedInAsync(prodFactory);

        var scalarResponse = await prodClient.Http.GetAsync("/scalar", TestContext.Current.CancellationToken);
        var openApiResponse = await prodClient.Http.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);
        var apiResponse = await prodClient.Http.GetAsync("/api/v1/categories", TestContext.Current.CancellationToken);

        scalarResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        openApiResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var cspHeaders = apiResponse.Headers.GetValues("Content-Security-Policy").ToList();
        cspHeaders.Should().ContainSingle();
        cspHeaders[0].Should().Be("default-src 'self'");
        cspHeaders[0].Should().NotContain("unsafe-inline");
        cspHeaders[0].Should().NotContain("nonce-");
    }

    private static async Task<Guid> IngredientIdAsync(ApiClient client, string name) =>
        (await client.GetJsonAsync($"/api/v1/ingredients?search={Uri.EscapeDataString(name)}")).EnumerateArray()
            .Single(i => i.GetProperty("name").GetString() == name).GetProperty("id").GetGuid();

    private static async Task<HttpResponseMessage> AddAsync(ApiClient client, string ingredient, decimal amount, string unit) =>
        await client.Http.PostAsJsonAsync("/api/v1/pantry-items",
            new { ingredientId = await IngredientIdAsync(client, ingredient), amount, unit }, TestContext.Current.CancellationToken);

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
    }
}
