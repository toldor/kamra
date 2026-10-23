using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KamraApp.Application.Common;
using KamraApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KamraApp.Integration.Tests;

// US-1 through the API on a real PostgreSQL: household isolation (QA-1, S-2), optimistic concurrency
// (ADR-0008) and validation (no invalid input may become a 500).
[Collection("api")]
public class PantryApiTests(KamraApiFactory factory)
{
    private static DateOnly Today => KamraApp.Application.Common.Today.Of(TimeProvider.System);

    [Fact]
    public async Task Categories_list_every_category_with_its_shelf_life()
    {
        var client = await ApiClient.SignedInAsync(factory);

        var categories = (await client.GetJsonAsync("/api/v1/categories")).EnumerateArray().ToList();

        categories.Should().HaveCount(14);
        categories.Should().Contain(c => c.GetProperty("category").GetString() == "dairy" && c.GetProperty("shelfLifeDays").GetInt32() == 7);
        categories.Should().Contain(c => c.GetProperty("category").GetString() == "other" && c.GetProperty("shelfLifeDays").ValueKind == JsonValueKind.Null);
    }

    [Fact]
    public async Task Ingredients_are_searched_by_normalized_name()
    {
        var client = await ApiClient.SignedInAsync(factory);

        var names = (await client.GetJsonAsync("/api/v1/ingredients?search=%20%20TEJ%20")).EnumerateArray()
            .Select(i => i.GetProperty("name").GetString()).ToList();

        names.Should().Contain(["tej", "tejföl", "tejszín"]).And.NotContain("vaj");
    }

    [Fact]
    public async Task An_added_item_is_listed_with_its_entered_unit_and_estimated_expiry()
    {
        var client = await ApiClient.SignedInAsync(factory);

        var response = await AddAsync(client, "tejföl", amount: 20, unit: "dkg");

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var item = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        item.GetProperty("amount").GetDecimal().Should().Be(20m);
        item.GetProperty("unit").GetString().Should().Be("dkg");
        item.GetProperty("quantity").GetDecimal().Should().Be(200m);
        item.GetProperty("category").GetString().Should().Be("dairy");
        item.GetProperty("expiryEstimated").GetBoolean().Should().BeTrue();
        item.GetProperty("expiryDate").GetString().Should().Be(Today.AddDays(7).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        var listed = (await client.GetJsonAsync("/api/v1/pantry-items")).EnumerateArray().ToList();
        listed.Should().ContainSingle(i => i.GetProperty("id").GetGuid() == item.GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task The_list_filters_by_search_category_and_soon_expiring()
    {
        var client = await ApiClient.SignedInAsync(factory);
        await AddAsync(client, "tej", 1, "l", expiryDate: Today.AddDays(1));
        await AddAsync(client, "rizs", 1, "kg");
        await AddAsync(client, "joghurt", 150, "g", expiryDate: Today.AddDays(5));

        (await NamesAsync(client, "?search=TEJ")).Should().Equal("tej");
        (await NamesAsync(client, "?category=dryGoods")).Should().Equal("rizs");
        (await NamesAsync(client, "?expiringSoon=true")).Should().Equal("tej");
        (await NamesAsync(client, "")).Should().Equal("tej", "joghurt", "rizs"); // by expiry date
    }

    [Fact]
    public async Task An_expired_item_is_listed_and_marked()
    {
        var client = await ApiClient.SignedInAsync(factory);
        await AddAsync(client, "tej", 1, "l", expiryDate: Today.AddDays(-1));

        var item = (await client.GetJsonAsync("/api/v1/pantry-items")).EnumerateArray().Single();

        item.GetProperty("expired").GetBoolean().Should().BeTrue();
        item.GetProperty("soonExpiring").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task A_decrease_with_a_reason_is_logged_and_the_quantity_equals_the_log()
    {
        var client = await ApiClient.SignedInAsync(factory);
        var item = await ReadAsync(await AddAsync(client, "tej", 1, "l"));

        var response = await client.PutAsync(ItemPath(item), Edit(item, amount: 6, unit: "dl", reason: "consumed"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await ReadAsync(response);
        updated.GetProperty("quantity").GetDecimal().Should().Be(600m);
        updated.GetProperty("version").GetUInt32().Should().NotBe(item.GetProperty("version").GetUInt32());
        (await MovementsAsync(Id(item))).Should().Equal(("Added", 1000m), ("Consumed", -400m));
    }

    [Fact]
    public async Task A_decrease_to_zero_removes_the_item_from_the_list()
    {
        var client = await ApiClient.SignedInAsync(factory);
        var item = await ReadAsync(await AddAsync(client, "tej", 1, "l"));

        var deleted = await ReadAsync(await client.PutAsync(ItemPath(item), Edit(item, amount: 0, unit: "l", reason: "discarded")));

        (await client.GetJsonAsync("/api/v1/pantry-items")).EnumerateArray().Should().BeEmpty();
        var again = await client.PutAsync(ItemPath(item), Edit(deleted, amount: 0, unit: "l", reason: "discarded"));
        again.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ApiClient.ProblemAsync(again)).GetProperty("code").GetString().Should().Be("PANTRY_ITEM_NOT_FOUND");
    }

    [Fact]
    public async Task A_stale_version_returns_409_and_leaves_the_item_unchanged()
    {
        var client = await ApiClient.SignedInAsync(factory);
        var item = await ReadAsync(await AddAsync(client, "tej", 1, "l"));
        await ExecuteSqlAsync($"""UPDATE "PantryItems" SET "Category" = 'Frozen' WHERE "Id" = {Id(item)}"""); // another tab

        var response = await client.PutAsync(ItemPath(item), Edit(item, amount: 5, unit: "dl", reason: "consumed"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ApiClient.ProblemAsync(response)).GetProperty("code").GetString().Should().Be("PANTRY_ITEM_MODIFIED");
        (await MovementsAsync(Id(item))).Should().Equal(("Added", 1000m));
    }

    [Fact]
    public async Task Of_two_concurrent_edits_with_the_same_version_one_wins()
    {
        var client = await ApiClient.SignedInAsync(factory);
        var item = await ReadAsync(await AddAsync(client, "tej", 1, "l"));

        var responses = await Task.WhenAll(
            client.PutAsync(ItemPath(item), Edit(item, amount: 8, unit: "dl", reason: "consumed")),
            client.PutAsync(ItemPath(item), Edit(item, amount: 5, unit: "dl", reason: "discarded")));

        responses.Select(r => r.StatusCode).Should().BeEquivalentTo([HttpStatusCode.OK, HttpStatusCode.Conflict]);
        var movements = await MovementsAsync(Id(item));
        movements.Should().HaveCount(2, "the losing edit must not be logged");
        var quantity = (await client.GetJsonAsync("/api/v1/pantry-items")).EnumerateArray().Single().GetProperty("quantity").GetDecimal();
        movements.Sum(m => m.Delta).Should().Be(quantity);
    }

    [Fact]
    public async Task Another_households_item_is_indistinguishable_from_a_missing_one()
    {
        var owner = await ApiClient.SignedInAsync(factory);
        var item = await ReadAsync(await AddAsync(owner, "tej", 1, "l"));
        var other = await ApiClient.SignedInAsync(factory);

        var foreign = await other.PutAsync(ItemPath(item), Edit(item, amount: 0, unit: "l", reason: "discarded"));
        var missing = await other.PutAsync($"/api/v1/pantry-items/{Guid.CreateVersion7()}", Edit(item, amount: 0, unit: "l", reason: "discarded"));

        (await other.GetJsonAsync("/api/v1/pantry-items")).EnumerateArray().Should().BeEmpty();
        foreign.StatusCode.Should().Be(HttpStatusCode.NotFound);
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
        WithoutRequestIds(await ApiClient.ProblemAsync(foreign)).Should().Be(WithoutRequestIds(await ApiClient.ProblemAsync(missing)));
        (await MovementsAsync(Id(item))).Should().Equal(("Added", 1000m));
    }

    [Theory]
    [InlineData("GET", "/api/v1/categories")]
    [InlineData("GET", "/api/v1/ingredients")]
    [InlineData("GET", "/api/v1/pantry-items")]
    [InlineData("POST", "/api/v1/pantry-items")]
    [InlineData("PUT", "/api/v1/pantry-items/0199c9a0-0000-7000-8000-000000000001")]
    public async Task Pantry_endpoints_require_sign_in(string method, string path)
    {
        var anonymous = await ApiClient.CreateAsync(factory);

        var response = await anonymous.Http.SendAsync(new HttpRequestMessage(new HttpMethod(method), path)
        {
            Content = method == "GET" ? null : JsonContent.Create(new { }),
        }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ApiClient.ProblemAsync(response)).GetProperty("code").GetString().Should().Be("UNAUTHENTICATED");
    }

    [Fact]
    public async Task A_post_without_antiforgery_token_is_rejected_and_creates_nothing()
    {
        var client = await ApiClient.SignedInAsync(factory);
        client.Http.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");

        var response = await AddAsync(client, "tej", 1, "l");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ApiClient.ProblemAsync(response)).GetProperty("code").GetString().Should().Be("ANTIFORGERY_TOKEN_INVALID");
        (await client.GetJsonAsync("/api/v1/pantry-items")).EnumerateArray().Should().BeEmpty();
    }

    [Theory]
    [InlineData("""{ "amount": 1000000000000, "unit": "g" }""", "amount")]
    [InlineData("""{ "amount": 1.2345, "unit": "g" }""", "amount")]
    [InlineData("""{ "amount": 2, "unit": "dl" }""", "unit")]
    [InlineData("""{ "amount": 200, "unit": "g", "expiryDate": "2026-13-45" }""", "")]
    [InlineData("""{ "amount": "200", "unit": "g" }""", "")]
    public async Task Invalid_input_returns_400_not_500(string body, string field)
    {
        var client = await ApiClient.SignedInAsync(factory);
        var sourCream = await IngredientIdAsync(client, "tejföl");
        var json = body.Replace("{ ", $$"""{ "ingredientId": "{{sourCream}}", """, StringComparison.Ordinal);

        var response = await client.Http.PostAsync("/api/v1/pantry-items",
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await ApiClient.ProblemAsync(response);
        problem.GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
        problem.GetProperty("errors").TryGetProperty(field, out _).Should().BeTrue();
    }

    [Theory]
    [InlineData("""{ "amount": 5, "unit": "dl", "category": "dairy", "reason": "consumed", "version": -1 }""", "")]
    [InlineData("""{ "amount": 5, "unit": "dl", "category": "dairy", "reason": "added", "version": 1 }""", "reason")]
    [InlineData("""{ "amount": 5, "unit": "dl", "category": "dairy", "reason": "consumed", "expiryDate": "tomorrow", "version": 1 }""", "")]
    [InlineData("""{ "amount": 0.0001, "unit": "dl", "category": "dairy", "reason": "consumed", "version": 1 }""", "amount")]
    public async Task Invalid_update_returns_400_not_500(string body, string field)
    {
        var client = await ApiClient.SignedInAsync(factory);
        var item = await ReadAsync(await AddAsync(client, "tej", 1, "l"));

        var response = await client.Http.PutAsync(ItemPath(item),
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await ApiClient.ProblemAsync(response);
        problem.GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
        problem.GetProperty("errors").TryGetProperty(field, out _).Should().BeTrue();
    }

    [Theory]
    [InlineData("%25")]
    [InlineData("_")]
    public async Task Search_wildcards_are_matched_literally(string search)
    {
        var client = await ApiClient.SignedInAsync(factory);
        await AddAsync(client, "tej", 1, "l");

        (await client.GetJsonAsync($"/api/v1/ingredients?search={search}")).EnumerateArray().Should().BeEmpty();
        (await client.GetJsonAsync($"/api/v1/pantry-items?search={search}")).EnumerateArray().Should().BeEmpty();
    }

    [Fact]
    public async Task A_too_long_search_returns_400()
    {
        var client = await ApiClient.SignedInAsync(factory);

        foreach (var path in new[] { "/api/v1/pantry-items", "/api/v1/ingredients" })
        {
            var response = await client.Http.GetAsync($"{path}?search={new string('a', 101)}", TestContext.Current.CancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest, path);
            (await ApiClient.ProblemAsync(response)).GetProperty("errors").TryGetProperty("search", out _).Should().BeTrue(path);
        }
    }

    [Fact]
    public async Task The_csp_is_strict_on_the_api_and_relaxed_only_for_the_scalar_page()
    {
        var client = await ApiClient.SignedInAsync(factory);

        var api = await client.Http.GetAsync("/api/v1/categories", TestContext.Current.CancellationToken);
        var scalar = await client.Http.GetAsync("/scalar/", TestContext.Current.CancellationToken);

        api.Headers.GetValues("Content-Security-Policy").Should().Equal("default-src 'self'");
        var scalarCsp = scalar.Headers.GetValues("Content-Security-Policy").Single();
        var nonce = System.Text.RegularExpressions.Regex.Match(scalarCsp, "script-src 'self' 'nonce-([^']+)'").Groups[1].Value;
        nonce.Should().NotBeEmpty("the inline scripts are allowed only by a per-request nonce");
        scalarCsp.Should().NotContain("script-src 'self' 'unsafe-inline'");
        (await scalar.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Contain($"nonce=\"{nonce}\"");
    }

    [Fact]
    public async Task Scalar_and_the_openapi_document_are_served_in_development()
    {
        var client = await ApiClient.CreateAsync(factory);

        (await client.Http.GetAsync("/scalar", TestContext.Current.CancellationToken)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.Http.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Scalar_and_the_openapi_document_are_not_exposed_in_production()
    {
        await using var production = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        var client = await ApiClient.CreateAsync(production);

        (await client.Http.GetAsync("/scalar", TestContext.Current.CancellationToken)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.Http.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<Guid> IngredientIdAsync(ApiClient client, string name) =>
        (await client.GetJsonAsync($"/api/v1/ingredients?search={Uri.EscapeDataString(name)}")).EnumerateArray()
            .Single(i => i.GetProperty("name").GetString() == name).GetProperty("id").GetGuid();

    private static async Task<HttpResponseMessage> AddAsync(ApiClient client, string ingredient, decimal amount, string unit, DateOnly? expiryDate = null) =>
        await client.Http.PostAsJsonAsync("/api/v1/pantry-items",
            new { ingredientId = await IngredientIdAsync(client, ingredient), amount, unit, expiryDate },
            TestContext.Current.CancellationToken);

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
    }

    private static async Task<List<string?>> NamesAsync(ApiClient client, string query) =>
        [.. (await client.GetJsonAsync($"/api/v1/pantry-items{query}")).EnumerateArray().Select(i => i.GetProperty("ingredientName").GetString())];

    private static Guid Id(JsonElement item) => item.GetProperty("id").GetGuid();

    private static string ItemPath(JsonElement item) => $"/api/v1/pantry-items/{Id(item)}";

    // Keeps the category and an estimated expiry; only the amount, unit and reason change.
    private static object Edit(JsonElement item, decimal amount, string unit, string? reason) => new
    {
        amount,
        unit,
        category = item.GetProperty("category").GetString(),
        expiryDate = (DateOnly?)null,
        reason,
        version = item.GetProperty("version").GetUInt32(),
    };

    // correlationId and traceId identify the request, so they differ for every response.
    private static string WithoutRequestIds(JsonElement problem) =>
        JsonSerializer.Serialize(problem.EnumerateObject().Where(p => p.Name is not ("correlationId" or "traceId")).ToDictionary(p => p.Name, p => p.Value));

    private async Task<List<(string Reason, decimal Delta)>> MovementsAsync(Guid pantryItemId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KamraDbContext>();
        var rows = await db.StockMovements.Where(m => m.PantryItemId == pantryItemId).OrderBy(m => m.Id)
            .Select(m => new { m.Reason, m.Delta }).ToListAsync(TestContext.Current.CancellationToken);
        return [.. rows.Select(r => (r.Reason.ToString(), r.Delta))];
    }

    private async Task ExecuteSqlAsync(FormattableString sql)
    {
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<KamraDbContext>().Database.ExecuteSqlAsync(sql, TestContext.Current.CancellationToken);
    }
}
