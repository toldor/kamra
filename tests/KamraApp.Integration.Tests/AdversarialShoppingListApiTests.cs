using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace KamraApp.Integration.Tests;

// Adversarial tests for US-5 (shopping list, suggestions, minimum, and their relation to pantry and cooking),
// written by Antigravity (Claude Sonnet 4.6) acting as a hostile tester (docs/07_ai/review_prompts.md,
// template 2). Integrated verbatim apart from compile fixes (line breaks lost when pasting). Of the 8 tests, the
// first (IDOR on the minimum) was left out as invalid: "tej" is a shared system ingredient and the minimum is per
// household, so the attacker's PUT removes only their own (missing) minimum and 204 is correct; the valid half
// (the minimum does not show in another household) is covered by ShoppingListApiTests.D4_….
[Collection("api")]
public class AdversarialShoppingListApiTests(KamraApiFactory factory)
{
    // ---------------------------------------------------------------------------
    // 2. Állapot-replay – elfogadás után a javaslat elutasítása 409-et ad.
    //    Az ismételt elfogadás ugyanazt a 204-et adja (idempotens), DE a sor törlése
    //    után egy újabb elfogadás még mindig 204-et kell adjon (a „changed" ág az
    //    elfogadott állapotnál nem fut újra, az „ismétlés" ága igen).
    //    Ezután az elutasítás 409 SHOPPING_SUGGESTION_STATE_CONFLICT.
    //    Helyes viselkedés:
    //      • 1. elfogadás → 204
    //      • 2. elfogadás (same body) → 204 (repeat, nem duplázódik a sor)
    //      • sor törlése → 204
    //      • 3. elfogadás után az üres lista megmarad (repeat, nem kerül vissza)
    //      • elutasítás → 409 SHOPPING_SUGGESTION_STATE_CONFLICT
    // ---------------------------------------------------------------------------
    [Fact]
    public async Task State_replay_accept_then_reject_is_conflict_row_is_not_readded()
    {
        var client = await ApiClient.SignedInAsync(factory);
        var suggestion = await DepletedSuggestionAsync(client);

        var accept1 = await client.PostAsync(AcceptPath(suggestion), new { amount = 200, unit = "g" });
        var accept2 = await client.PostAsync(AcceptPath(suggestion), new { amount = 200, unit = "g" });

        accept1.StatusCode.Should().Be(HttpStatusCode.NoContent, "first accept succeeds");
        accept2.StatusCode.Should().Be(HttpStatusCode.NoContent, "repeated accept is idempotent (204)");
        (await ListAsync(client)).Should().HaveCount(1, "only one row, not two");

        // Töröljük a sort.
        var row = (await client.GetJsonAsync("/api/v1/shopping-list")).EnumerateArray().Single();
        (await client.Http.DeleteAsync(RowPath(row), TestContext.Current.CancellationToken)).EnsureSuccessStatusCode();

        // Elfogadás a törlés után: a javaslat már Accepted, a repeat-ág fut, a sor nem kerül vissza.
        var accept3 = await client.PostAsync(AcceptPath(suggestion), new { amount = 200, unit = "g" });
        accept3.StatusCode.Should().Be(HttpStatusCode.NoContent, "accept after row deletion is still a repeat");
        (await ListAsync(client)).Should().BeEmpty("the row must not be re-added on a repeated accept");

        // Elutasítás – az elfogadott javaslaton ez ütközés.
        var reject = await client.PostAsync(RejectPath(suggestion), null);
        reject.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ApiClient.ProblemAsync(reject)).GetProperty("code").GetString()
            .Should().Be("SHOPPING_SUGGESTION_STATE_CONFLICT");
    }

    // ---------------------------------------------------------------------------
    // 3. Upsert-határ – összeg pontosan 100 000-en és 1-gyel felette.
    //    Támadás: A sor mennyisége 99 999 + 1 = 100 000 – ez elfogadható.
    //    Egy következő +1 g hozzáadás viszont 100 001-et eredményezne – ezt el kell utasítani.
    //    Helyes viselkedés:
    //      • 99 999 + 1 → 200 OK, sor.amount == 100 000
    //      • 100 000 + 1 → 400 VALIDATION_FAILED, errors.amount megjelenik, a sor
    //        megmarad 100 000-en.
    // ---------------------------------------------------------------------------
    [Fact]
    public async Task Upsert_boundary_sum_at_exactly_100000_ok_above_rejected()
    {
        var client = await ApiClient.SignedInAsync(factory);

        (await AddToListAsync(client, "tejföl", 99_999, "g")).EnsureSuccessStatusCode();
        var atLimit = await AddToListAsync(client, "tejföl", 1, "g");
        atLimit.StatusCode.Should().Be(HttpStatusCode.OK, "sum of exactly 100 000 must be accepted");

        var row = await atLimit.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        row.GetProperty("amount").GetProperty("value").GetDecimal().Should().Be(100_000m);

        var overLimit = await AddToListAsync(client, "tejföl", 1, "g");
        overLimit.StatusCode.Should().Be(HttpStatusCode.BadRequest, "sum of 100 001 must be rejected");
        (await ApiClient.ProblemAsync(overLimit)).GetProperty("errors").TryGetProperty("amount", out _)
            .Should().BeTrue("the 'amount' field must carry the error");

        // A sor változatlan marad.
        (await ListAsync(client)).Should().Equal(("tejföl", 100_000m, "g", false));
    }

    // ---------------------------------------------------------------------------
    // 4. Upsert-határ – inkompatibilis dimenziójú egységek (g + dl).
    //    Támadás: A sor „tejföl" (tömeg-dimenzió, g). Az újabb kérés dl egységgel jön –
    //    dimenziót kell ellenőrizni.
    //    Helyes viselkedés: 400 VALIDATION_FAILED, errors.unit megjelenik.
    //    Az eredeti sor érintetlen.
    // ---------------------------------------------------------------------------
    [Fact]
    public async Task Upsert_rejects_cross_dimension_unit_mismatch()
    {
        var client = await ApiClient.SignedInAsync(factory);
        (await AddToListAsync(client, "tejföl", 100, "g")).EnsureSuccessStatusCode();

        // tejföl dimenziója tömeg; dl térfogat-egység → VALIDATION_FAILED
        var body = $$"""{ "ingredientId": "{{await IngredientIdAsync(client, "tejföl")}}", "amount": 1, "unit": "dl" }""";
        var response = await client.Http.PostAsync("/api/v1/shopping-list/items",
            new StringContent(body, Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await ApiClient.ProblemAsync(response);
        problem.GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
        problem.GetProperty("errors").TryGetProperty("unit", out _).Should().BeTrue();

        // A sor változatlan.
        (await ListAsync(client)).Should().Equal(("tejföl", 100m, "g", false));
    }

    // ---------------------------------------------------------------------------
    // 5. Javaslat-kiváltás kijátszása – "discarded" és "corrected" csökkentés nem vált ki javaslatot.
    //    Támadás: A rossz rögzítés (corrected) okú csökkentés 0-ra viszi a tejfölt, de
    //    SuggestionRules.OnDecrease csak Consumed-ra vált ki javaslatot. Ellenőrzés:
    //    egyetlen sor, discarded és corrected → 0 javaslat (P14b/P14c megismétlése
    //    az ellenséges szögből: ha a szűrés megtörne, 1 javaslat jelenne meg).
    //    Helyes viselkedés: a javaslatlista üres marad.
    // ---------------------------------------------------------------------------
    [Theory]
    [InlineData("discarded")]
    [InlineData("corrected")]
    public async Task Non_consumed_decrease_to_zero_never_triggers_a_suggestion(string reason)
    {
        var client = await ApiClient.SignedInAsync(factory);
        var item = await ReadAsync(await AddAsync(client, "tejföl", 200, "g", 6));

        (await DecreaseAsync(client, item, 0, "g", reason)).EnsureSuccessStatusCode();

        (await SuggestionsAsync(client)).Should().BeEmpty($"reason '{reason}' must not trigger a shopping suggestion");
    }

    // ---------------------------------------------------------------------------
    // 6. Minimumszint törlése + elutasított javaslat: a javaslat már nem blokkolja az újat.
    //    Sorrend:
    //      1. Beállítunk minimumot (500 ml tej).
    //      2. Csökkentjük a készletet a minimum alá → BelowMinimum javaslat.
    //      3. Elutasítjuk.
    //      4. Töröljük a minimumszintet (üres PUT body).
    //      5. A javaslat „clear" állapotba kerül (ClearedAt kitöltve).
    //      6. Újra csökkentjük → NEM jön új javaslat, mert nincs minimum és a tej
    //         még > 0, tehát Depleted sem keletkezik.
    //    Helyes viselkedés: 0 javaslat a minimum törlése után.
    // ---------------------------------------------------------------------------
    [Fact]
    public async Task Removing_minimum_clears_rejected_suggestion_and_no_new_one_appears()
    {
        var client = await ApiClient.SignedInAsync(factory);
        var ingredientId = await IngredientIdAsync(client, "tej");

        // 1. minimum 500 ml
        (await client.PutAsync($"/api/v1/ingredients/{ingredientId}/minimum", new { amount = 500, unit = "ml" }))
            .EnsureSuccessStatusCode();

        // 2. 600 ml tej bekerül a kamrába, majd csökkentjük 400 ml-re (alatta van a minimumnak)
        var first = await ReadAsync(await AddAsync(client, "tej", 600, "ml", 5));
        first = await ReadAsync(await DecreaseAsync(client, first, 400, "ml", "consumed"));
        (await SuggestionsAsync(client)).Should().ContainSingle("expected a BelowMinimum suggestion");

        // 3. Elutasítjuk
        var below = (await SuggestionsAsync(client)).Single();
        (await client.PostAsync(RejectPath(below), null)).EnsureSuccessStatusCode();

        // 4. Minimumszint törlése (üres body → amount és unit null)
        (await client.PutAsync($"/api/v1/ingredients/{ingredientId}/minimum", new { }))
            .EnsureSuccessStatusCode();

        // 5–6. Ismételt csökkentés: 400 ml → 300 ml marad (> 0), és nincs minimum → 0 javaslat.
        (await DecreaseAsync(client, first, 300, "ml", "consumed")).EnsureSuccessStatusCode();
        (await SuggestionsAsync(client)).Should().BeEmpty("after minimum removal no BelowMinimum suggestion must appear");
    }

    // ---------------------------------------------------------------------------
    // 7. Verseny – két egyidejű elfogadás + elutasítás: pontosan egy nyer, a vesztes 409.
    //    Helyes viselkedés:
    //      • Pontosan az egyik kérés sikeres (204), a másik 409 STATE_CONFLICT.
    //      • A lista 0 vagy 1 sort tartalmaz, attól függően, melyik nyert.
    //      • A javaslatok listája üres (a nyertes döntött).
    //    (5-szörös futtatás az ADR-0008 6. lock-garantia megvizsgálásához.)
    // ---------------------------------------------------------------------------
    [Fact]
    public async Task Concurrent_accept_and_reject_exactly_one_wins_list_is_consistent()
    {
        for (var run = 0; run < 5; run++)
        {
            var client = await ApiClient.SignedInAsync(factory);
            var suggestion = await DepletedSuggestionAsync(client);

            var responses = await Task.WhenAll(
                client.PostAsync(AcceptPath(suggestion), new { amount = 200, unit = "g" }),
                client.PostAsync(RejectPath(suggestion), null));

            var successes = responses.Count(r => r.IsSuccessStatusCode);
            successes.Should().Be(1, $"run {run}: exactly one of accept/reject must win");

            var loser = responses.Single(r => !r.IsSuccessStatusCode);
            loser.StatusCode.Should().Be(HttpStatusCode.Conflict, $"run {run}: loser must be 409 STATE_CONFLICT");
            (await ApiClient.ProblemAsync(loser)).GetProperty("code").GetString()
                .Should().Be("SHOPPING_SUGGESTION_STATE_CONFLICT");

            var expectedRows = responses[0].IsSuccessStatusCode ? 1 : 0;
            (await ListAsync(client)).Should().HaveCount(expectedRows, $"run {run}: list row count must match winner");

            (await SuggestionsAsync(client)).Should().BeEmpty($"run {run}: suggestion must be resolved");
        }
    }

    // ---------------------------------------------------------------------------
    // 8. Hibaválasz-szivárgás – hiányos/hibás/null/szöveg-típusú kérések a bevásárlólista-végpontokon.
    //    Támadás: null IngredientId, hiányzó kötelező mező, negatív szám, ismeretlen mező,
    //    szám stringként – megpróbálunk 5xx-et és stack trace / SQL / EF szivárgást előidézni.
    //    Helyes viselkedés: minden eset 4xx; a válasz nem tartalmaz belső adatokat;
    //    a "code" mező minden esetben jelen van.
    // ---------------------------------------------------------------------------
    [Theory]
    [InlineData("/api/v1/shopping-list/items", """{ "ingredientId": null, "amount": 1, "unit": "g" }""")]  // null IngredientId
    [InlineData("/api/v1/shopping-list/items", """{ "amount": 1, "unit": "g" }""")]                         // hiányzó ingredientId
    [InlineData("/api/v1/shopping-list/items", """{ "amount": -5, "unit": "g" }""")]                        // negatív mennyiség
    [InlineData("/api/v1/shopping-list/items", """{ "amount": 1, "unit": "g", "unknownField": 99 }""")]     // ismeretlen mező
    [InlineData("/api/v1/shopping-suggestions/00000000-0000-7000-8000-000000000001/accept",
        """{ "amount": "száz", "unit": "g" }""")]                                                           // szám stringként
    public async Task Error_responses_never_leak_internal_details(string path, string body)
    {
        var client = await ApiClient.SignedInAsync(factory);

        var response = await client.Http.PostAsync(path,
            new StringContent(body, Encoding.UTF8, "application/json"),
            TestContext.Current.CancellationToken);

        // Minden ilyen kérés 4xx – soha nem lehet 5xx (szivárgás).
        ((int)response.StatusCode).Should().BeInRange(400, 499, "malformed / invalid input must produce a 4xx, not a 5xx");

        var raw = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        raw.Should().NotContainAny(
            "Exception", "StackTrace", " at KamraApp", "SELECT ", "INSERT ", "UPDATE ",
            "Npgsql", "EntityFramework", "Microsoft.EntityFrameworkCore",
            "password=", "Password=",
            "at System.", "at Microsoft.");

        // A válasz RFC 7807 ProblemDetails – van "code" mező.
        var problem = JsonDocument.Parse(raw).RootElement;
        problem.TryGetProperty("code", out _).Should().BeTrue("every error response must carry a stable 'code' field");
    }

    // -------------------------------------------------------------------------
    // Helpers (adapted from ShoppingListApiTests)
    // -------------------------------------------------------------------------

    private static DateOnly Today => KamraApp.Application.Common.Today.Of(TimeProvider.System);

    private static string RowPath(JsonElement row) =>
        $"/api/v1/shopping-list/items/{row.GetProperty("id").GetGuid()}";

    private static string AcceptPath(JsonElement suggestion) =>
        $"/api/v1/shopping-suggestions/{suggestion.GetProperty("id").GetGuid()}/accept";

    private static string RejectPath(JsonElement suggestion) =>
        $"/api/v1/shopping-suggestions/{suggestion.GetProperty("id").GetGuid()}/reject";

    private static async Task<List<JsonElement>> SuggestionsAsync(ApiClient client) =>
        [.. (await client.GetJsonAsync("/api/v1/shopping-suggestions")).EnumerateArray()];

    private static async Task<List<(string? Name, decimal? Amount, string? Unit, bool Checked)>> ListAsync(ApiClient client) =>
        [.. (await client.GetJsonAsync("/api/v1/shopping-list")).EnumerateArray().Select(r => (
            r.GetProperty("ingredientName").GetString(),
            r.GetProperty("amount").ValueKind == JsonValueKind.Null
                ? (decimal?)null
                : r.GetProperty("amount").GetProperty("value").GetDecimal(),
            r.GetProperty("amount").ValueKind == JsonValueKind.Null
                ? null
                : r.GetProperty("amount").GetProperty("unit").GetString(),
            r.GetProperty("checked").GetBoolean()))];

    private static async Task<HttpResponseMessage> AddToListAsync(ApiClient client, string ingredient, decimal? amount, string? unit) =>
        await client.PostAsync("/api/v1/shopping-list/items",
            new { ingredientId = await IngredientIdAsync(client, ingredient), amount, unit });

    private static async Task<Guid> IngredientIdAsync(ApiClient client, string name) =>
        (await client.GetJsonAsync($"/api/v1/ingredients?search={Uri.EscapeDataString(name)}"))
        .EnumerateArray()
        .Single(i => i.GetProperty("name").GetString() == name)
        .GetProperty("id").GetGuid();

    private static async Task<HttpResponseMessage> AddAsync(ApiClient client, string ingredient, decimal amount, string unit, int daysToExpiry)
    {
        var response = await client.PostAsync("/api/v1/pantry-items",
            new
            {
                ingredientId = await IngredientIdAsync(client, ingredient),
                amount,
                unit,
                expiryDate = Today.AddDays(daysToExpiry),
            });
        response.EnsureSuccessStatusCode();
        return response;
    }

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

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
    }

    private static async Task<JsonElement> DepletedSuggestionAsync(ApiClient client, string ingredient = "tejföl")
    {
        var (amount, unit) = ingredient == "tej" ? (1m, "l") : (200m, "g");
        var item = await ReadAsync(await AddAsync(client, ingredient, amount, unit, 6));
        (await DecreaseAsync(client, item, 0, unit, "consumed")).EnsureSuccessStatusCode();
        return (await SuggestionsAsync(client)).Single();
    }
}
