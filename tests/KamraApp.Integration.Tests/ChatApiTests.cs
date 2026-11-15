using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using KamraApp.Application.Chat;
using KamraApp.Application.ShoppingList;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace KamraApp.Integration.Tests;

// US-6 through the API on a real PostgreSQL, with the switch on and the model replaced by a script that calls
// the real MCP tools in the API process (ADR-0010; AGENTS.md 6.: no real LLM call in tests). Household isolation
// of every tool (QA-1), compact results without ids (us6_minispec K5), the error codes and the limit (K3, K8, K9).
[Collection("api")]
public partial class ChatApiTests(KamraApiFactory factory)
{
    private static DateOnly Today => KamraApp.Application.Common.Today.Of(TimeProvider.System);

    [Fact]
    public async Task The_chat_is_off_by_default_and_refused_with_LLM_DISABLED()
    {
        var client = await ApiClient.SignedInAsync(factory);

        var availability = await client.GetJsonAsync("/api/v1/chat/availability");
        var response = await client.PostAsync("/api/v1/chat", new { question = "Mi jár le?" });

        availability.GetProperty("enabled").GetBoolean().Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        (await ApiClient.ProblemAsync(response)).GetProperty("code").GetString().Should().Be("LLM_DISABLED");
    }

    [Fact]
    public async Task The_chat_is_available_when_the_switch_is_on()
    {
        using var app = WithChat(new ScriptedChatModel().AnswerWith("Szia!"));
        var client = await ApiClient.SignedInAsync(app);

        var availability = await client.GetJsonAsync("/api/v1/chat/availability");

        availability.GetProperty("enabled").GetBoolean().Should().BeTrue();
    }

    // K8, ADR-0010 7.: the switch of US-2 turns the chat on too, with the real Gemini adapter (not called here).
    [Fact]
    public async Task The_switch_wires_the_gemini_chat_adapter()
    {
        using var app = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Llm:Enabled", "true");
            builder.UseSetting("Llm:ApiKey", "test-key-not-used");
        });
        var client = await ApiClient.SignedInAsync(app);

        var availability = await client.GetJsonAsync("/api/v1/chat/availability");

        availability.GetProperty("enabled").GetBoolean().Should().BeTrue();
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IChatModel>().Should().BeOfType<KamraApp.Infrastructure.Llm.GeminiChatModel>();
    }

    [Fact]
    public async Task Without_a_session_the_chat_is_401_and_the_model_is_not_called()
    {
        var model = new ScriptedChatModel().AnswerWith("Szia!");
        using var app = WithChat(model);
        var client = await ApiClient.CreateAsync(app);

        var response = await client.PostAsync("/api/v1/chat", new { question = "Mi jár le?" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        model.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task A_too_long_question_is_VALIDATION_FAILED()
    {
        var model = new ScriptedChatModel().AnswerWith("Szia!");
        using var app = WithChat(model);
        var client = await ApiClient.SignedInAsync(app);

        var response = await client.PostAsync("/api/v1/chat", new { question = new string('a', 501) });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await ApiClient.ProblemAsync(response);
        problem.GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
        problem.GetProperty("errors").TryGetProperty("question", out _).Should().BeTrue();
        model.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task The_answer_comes_with_the_tools_used_and_the_history_reaches_the_model_as_text()
    {
        var model = new ScriptedChatModel().CallThenAnswer("get_shopping_list", "{}", "A lista üres.");
        using var app = WithChat(model);
        var client = await ApiClient.SignedInAsync(app);

        var response = await client.PostAsync("/api/v1/chat", new
        {
            question = "Mi van a listán?",
            history = new object[] { new { role = "user", text = "Szia" }, new { role = "assistant", text = "Szia! Miben segíthetek?" } },
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var answer = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        answer.GetProperty("answer").GetString().Should().Be("A lista üres.");
        answer.GetProperty("toolsUsed").EnumerateArray().Select(t => t.GetString()).Should().Equal("get_shopping_list");
        model.Calls[0].Should().Equal(new UserTurn("Szia"), new AssistantTurn("Szia! Miben segíthetek?"), new UserTurn("Mi van a listán?"));
    }

    [Fact]
    public async Task The_model_gets_the_four_tools_of_the_mcp_server()
    {
        var model = new ScriptedChatModel().AnswerWith("Szia!");
        using var app = WithChat(model);
        var client = await ApiClient.SignedInAsync(app);

        (await client.PostAsync("/api/v1/chat", new { question = "Szia" })).StatusCode.Should().Be(HttpStatusCode.OK);

        model.Tools.Select(t => t.Name).Should().BeEquivalentTo("get_pantry_items", "get_expiring_items", "get_recommendations", "get_shopping_list");
        model.Tools.Should().OnlyContain(t => !t.InputSchemaJson.Contains("household", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("get_pantry_items")]
    [InlineData("get_expiring_items")]
    [InlineData("get_shopping_list")]
    public async Task Each_tool_returns_only_the_own_households_data(string tool)
    {
        var model = new ScriptedChatModel();
        using var app = WithChat(model);
        var other = await ApiClient.SignedInAsync(app);
        await AddPantryItemAsync(other, "tejföl", 200, "g", Today.AddDays(1));
        await AddShoppingItemAsync(other, "finomliszt");
        var own = await ApiClient.SignedInAsync(app);
        await AddPantryItemAsync(own, "vaj", 100, "g", Today.AddDays(1));
        await AddShoppingItemAsync(own, "cukor");

        var result = await ToolResultAsync(model, own, tool, "{}");

        // The names as the model reads them: the raw JSON escapes "ö" (external review X1), so a raw text search for
        // "tejföl" would pass even if the item were there.
        Names(result).Should().Equal(tool == "get_shopping_list" ? "cukor" : "vaj");
        GuidPattern().IsMatch(result).Should().BeFalse("us6_minispec K5: no ids go to the model");
    }

    [Fact]
    public async Task The_recommendation_tool_sees_only_the_own_pantry()
    {
        var model = new ScriptedChatModel();
        using var app = WithChat(model);
        var other = await ApiClient.SignedInAsync(app);
        await AddPantryItemAsync(other, "tejföl", 200, "g", Today.AddDays(1));
        var own = await ApiClient.SignedInAsync(app);

        var ownResult = Parse(await ToolResultAsync(model, own, "get_recommendations", "{}"));
        var otherResult = Parse(await ToolResultAsync(model, other, "get_recommendations", "{}"));

        ownResult.GetProperty("pantryIsEmpty").GetBoolean().Should().BeTrue();
        otherResult.GetProperty("pantryIsEmpty").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task The_pantry_tool_result_is_compact_with_the_API_codes_and_no_ids()
    {
        var model = new ScriptedChatModel();
        using var app = WithChat(model);
        var client = await ApiClient.SignedInAsync(app);
        await AddPantryItemAsync(client, "vaj", 25, "dkg", Today.AddDays(1));

        var result = await ToolResultAsync(model, client, "get_pantry_items", "{}");

        var item = Parse(result).GetProperty("items").EnumerateArray().Single();
        item.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(
            "name", "amount", "unit", "category", "expiryDate", "expiryEstimated", "expired", "soonExpiring");
        item.GetProperty("name").GetString().Should().Be("vaj");
        item.GetProperty("amount").GetDecimal().Should().Be(25m);
        item.GetProperty("unit").GetString().Should().Be("dkg");
        item.GetProperty("category").GetString().Should().Be("dairy");
        item.GetProperty("expiryDate").GetString().Should().Be(Today.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        item.GetProperty("soonExpiring").GetBoolean().Should().BeTrue();
        Parse(result).GetProperty("truncated").GetBoolean().Should().BeFalse();
        GuidPattern().IsMatch(result).Should().BeFalse("us6_minispec K5: no ids go to the model");
    }

    [Fact]
    public async Task The_expiring_tool_returns_only_the_soon_expiring_items()
    {
        var model = new ScriptedChatModel();
        using var app = WithChat(model);
        var client = await ApiClient.SignedInAsync(app);
        await AddPantryItemAsync(client, "vaj", 100, "g", Today.AddDays(2));
        await AddPantryItemAsync(client, "cukor", 1, "kg", Today.AddDays(30));

        var result = await ToolResultAsync(model, client, "get_expiring_items", "{}");

        Names(result).Should().Equal("vaj");
    }

    [Fact]
    public async Task The_pantry_tool_filters_by_category()
    {
        var model = new ScriptedChatModel();
        using var app = WithChat(model);
        var client = await ApiClient.SignedInAsync(app);
        await AddPantryItemAsync(client, "vaj", 100, "g", Today.AddDays(5));
        await AddPantryItemAsync(client, "paradicsom", 500, "g", Today.AddDays(5));

        var result = await ToolResultAsync(model, client, "get_pantry_items", "{\"category\":\"dairy\"}");

        Names(result).Should().Equal("vaj");
    }

    [Theory]
    [InlineData("get_pantry_items", "{\"category\":\"nincs\"}")]
    [InlineData("get_recommendations", "{\"max_results\":11}")]
    [InlineData("get_recommendations", "{\"max_results\":0}")]
    public async Task A_tool_refusing_its_arguments_is_LLM_INVALID_RESPONSE(string tool, string arguments)
    {
        using var app = WithChat(new ScriptedChatModel().CallThenAnswer(tool, arguments, "-"));
        var client = await ApiClient.SignedInAsync(app);

        var response = await client.PostAsync("/api/v1/chat", new { question = "Mi van?" });

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        (await ApiClient.ProblemAsync(response)).GetProperty("code").GetString().Should().Be("LLM_INVALID_RESPONSE");
    }

    [Fact]
    public async Task The_recommendation_tool_returns_at_most_max_results_recipes_per_group()
    {
        var model = new ScriptedChatModel();
        using var app = WithChat(model);
        var client = await ApiClient.SignedInAsync(app);
        foreach (var name in new[] { "vaj", "tejföl", "paradicsom", "cukor", "finomliszt", "csirkemell" })
        {
            await AddPantryItemAsync(client, name, 1, "kg", Today.AddDays(5));
        }

        var result = Parse(await ToolResultAsync(model, client, "get_recommendations", "{\"max_results\":1}"));

        var makeable = result.GetProperty("makeable").GetArrayLength();
        var almost = result.GetProperty("almostMakeable").GetArrayLength();
        makeable.Should().BeLessThanOrEqualTo(1);
        almost.Should().BeLessThanOrEqualTo(1);
        (makeable + almost).Should().BeGreaterThan(0, "the seed recipes use these ingredients");
    }

    // T1 review J1: the tool keeps the use case's order (expiry, then the Hungarian alphabet, V-33).
    [Fact]
    public async Task The_pantry_tool_keeps_the_hungarian_order_of_the_pantry_list()
    {
        var model = new ScriptedChatModel();
        using var app = WithChat(model);
        var client = await ApiClient.SignedInAsync(app);
        await AddPantryItemAsync(client, "zabpehely", 500, "g", Today.AddDays(5));
        await AddPantryItemAsync(client, "őszibarack", 3, "db", Today.AddDays(5));
        await AddPantryItemAsync(client, "vaj", 100, "g", Today.AddDays(1));

        var result = Parse(await ToolResultAsync(model, client, "get_pantry_items", "{}"));

        result.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("name").GetString())
            .Should().Equal("vaj", "őszibarack", "zabpehely");
    }

    // T1 review J5 (us6_minispec K5): at most 100 rows, and the model is told that more exist.
    [Fact]
    public async Task The_pantry_tool_returns_at_most_100_rows_and_says_so()
    {
        var model = new ScriptedChatModel();
        using var app = WithChat(model);
        var client = await ApiClient.SignedInAsync(app);
        for (var i = 0; i < 101; i++)
        {
            await AddPantryItemAsync(client, "vaj", 10, "g", Today.AddDays(5 + (i % 3)));
        }

        var result = Parse(await ToolResultAsync(model, client, "get_pantry_items", "{}"));

        result.GetProperty("items").GetArrayLength().Should().Be(KamraApp.McpServer.KamraTools.MaxRows);
        result.GetProperty("truncated").GetBoolean().Should().BeTrue();
    }

    // T1 review J5 (K5): makeable recipes without shortages, almost makeable ones with them; no ids.
    [Fact]
    public async Task The_recommendation_tool_result_is_compact_and_without_ids()
    {
        var model = new ScriptedChatModel();
        using var app = WithChat(model);
        var client = await ApiClient.SignedInAsync(app);
        await AddPantryItemAsync(client, "tojás", 10, "db", Today.AddDays(5));
        await AddPantryItemAsync(client, "vaj", 100, "g", Today.AddDays(5));
        await AddPantryItemAsync(client, "tej", 1, "l", Today.AddDays(5));

        var json = await ToolResultAsync(model, client, "get_recommendations", "{}");

        var result = Parse(json);
        var makeable = result.GetProperty("makeable").EnumerateArray().Single(r => r.GetProperty("name").GetString() == "Rántotta");
        makeable.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(
            "name", "servings", "soonExpiringIngredients", "expiredIngredients");
        var almost = result.GetProperty("almostMakeable").EnumerateArray().Single(r => r.GetProperty("name").GetString() == "Palacsinta");
        almost.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(
            "name", "servings", "soonExpiringIngredients", "expiredIngredients", "shortages");
        almost.GetProperty("shortages").EnumerateArray().First().EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(
            "name", "missing", "available", "required", "unit");
        GuidPattern().IsMatch(json).Should().BeFalse("us6_minispec K5: no ids go to the model");
    }

    // T1 review J5 (K5): a list row is the name, the amount and unit when given, and the tick.
    [Fact]
    public async Task The_shopping_list_tool_result_is_compact()
    {
        var model = new ScriptedChatModel();
        using var app = WithChat(model);
        var client = await ApiClient.SignedInAsync(app);
        await AddShoppingItemAsync(client, "cukor", 50, "dkg");
        await AddShoppingItemAsync(client, "vaj");

        var result = Parse(await ToolResultAsync(model, client, "get_shopping_list", "{}"));

        var rows = result.GetProperty("items").EnumerateArray().ToDictionary(r => r.GetProperty("name").GetString()!);
        rows["cukor"].EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("name", "amount", "unit", "checked");
        rows["cukor"].GetProperty("unit").GetString().Should().Be("dkg");
        rows["vaj"].EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("name", "checked");
        result.GetProperty("truncated").GetBoolean().Should().BeFalse();
    }

    // T1 review J3: a failure inside a tool (here the database) is the app's own error (500, logged), not the
    // model's (502); only refused arguments are LLM_INVALID_RESPONSE.
    [Fact]
    public async Task A_failing_tool_is_an_INTERNAL_ERROR_and_not_blamed_on_the_model()
    {
        var model = new ScriptedChatModel().CallThenAnswer("get_shopping_list", "{}", "-");
        using var app = WithChat(model, builder => builder.ConfigureTestServices(services =>
            services.AddScoped(_ => DispatchProxy.Create<IShoppingListRepository, FailingRepository>())));
        var client = await ApiClient.SignedInAsync(app);

        var response = await client.PostAsync("/api/v1/chat", new { question = "Mi van a listán?" });

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        (await ApiClient.ProblemAsync(response)).GetProperty("code").GetString().Should().Be("INTERNAL_ERROR");
        model.Calls.Should().ContainSingle("the model is not asked again after a failed tool");
    }

    // T1 review J8 (K9): without configuration, 30 questions per household in the window.
    [Fact]
    public async Task By_default_the_chat_allows_30_questions_per_household()
    {
        var model = new ScriptedChatModel().AnswerWith("Szia!");
        using var app = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Llm:Enabled", "true");
            builder.UseSetting("Llm:ApiKey", "test-key-not-used");
            builder.ConfigureTestServices(services => services.AddSingleton<IChatModel>(model));
        });
        var client = await ApiClient.SignedInAsync(app);

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 31; i++)
        {
            statuses.Add((await client.PostAsync("/api/v1/chat", new { question = "Szia" })).StatusCode);
        }

        statuses.Take(30).Should().OnlyContain(s => s == HttpStatusCode.OK);
        statuses[30].Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task The_chat_is_limited_per_household()
    {
        using var app = WithChat(new ScriptedChatModel().AnswerWith("Szia!"), builder => builder.UseSetting("RateLimiting:Chat:PermitLimit", "2"));
        var client = await ApiClient.SignedInAsync(app);
        var other = await ApiClient.SignedInAsync(app);

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 3; i++)
        {
            statuses.Add((await client.PostAsync("/api/v1/chat", new { question = "Szia" })).StatusCode);
        }

        var otherStatus = (await other.PostAsync("/api/v1/chat", new { question = "Szia" })).StatusCode;

        statuses.Should().Equal(HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.TooManyRequests);
        otherStatus.Should().Be(HttpStatusCode.OK);
    }

    // The content the MCP tool returned to the model for one question of this household.
    private static async Task<string> ToolResultAsync(ScriptedChatModel model, ApiClient client, string tool, string arguments)
    {
        model.CallThenAnswer(tool, arguments, "-");

        var response = await client.PostAsync("/api/v1/chat", new { question = "Mi van?" });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return model.ToolResults.Should().ContainSingle().Subject.Content;
    }

    // The shared app with the switch on (us6_minispec K8) and the model replaced; same database.
    private WebApplicationFactory<Program> WithChat(ScriptedChatModel model, Action<IWebHostBuilder>? configure = null) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Llm:Enabled", "true");
            builder.UseSetting("Llm:ApiKey", "test-key-not-used");
            builder.UseSetting("RateLimiting:Chat:PermitLimit", "1000");
            configure?.Invoke(builder);
            builder.ConfigureTestServices(services => services.AddSingleton<IChatModel>(model));
        });

    private static async Task AddPantryItemAsync(ApiClient client, string name, decimal amount, string unit, DateOnly expiry)
    {
        var response = await client.PostAsync("/api/v1/pantry-items", new
        {
            ingredientId = await IngredientIdAsync(client, name),
            amount,
            unit,
            expiryDate = expiry.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    private static async Task AddShoppingItemAsync(ApiClient client, string name, decimal? amount = null, string? unit = null)
    {
        var ingredientId = await IngredientIdAsync(client, name);
        var response = await client.PostAsync("/api/v1/shopping-list/items",
            amount is null ? new { ingredientId } : (object)new { ingredientId, amount, unit });
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    private static async Task<Guid> IngredientIdAsync(ApiClient client, string name) =>
        (await client.GetJsonAsync($"/api/v1/ingredients?search={Uri.EscapeDataString(name)}")).EnumerateArray()
            .Single(i => i.GetProperty("name").GetString() == name).GetProperty("id").GetGuid();

    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

    // The item names of a pantry or shopping list tool result, decoded from the JSON.
    internal static string[] Names(string json) =>
        [.. Parse(json).GetProperty("items").EnumerateArray().Select(item => item.GetProperty("name").GetString()!)];

    [GeneratedRegex("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")]
    private static partial Regex GuidPattern();

    // A shopping list repository whose database is down: every call fails.
    public class FailingRepository : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException("The database is not reachable.");
    }

    // A model that answers at once, or first calls one tool and then answers; it records what it got for the
    // last question. The script is set per question, so one signed-in client can ask several.
    internal sealed class ScriptedChatModel : IChatModel
    {
        private readonly ConcurrentQueue<IReadOnlyList<ChatTurn>> _calls = new();
        private Func<int, ChatModelReply> _reply = _ => new ChatModelReply("-", [], "raw-answer");

        public bool Enabled => true;

        public List<IReadOnlyList<ChatTurn>> Calls => [.. _calls];

        public IReadOnlyList<ChatTool> Tools { get; private set; } = [];

        public List<ToolResult> ToolResults => [.. _calls.SelectMany(t => t.OfType<ToolResultsTurn>()).SelectMany(t => t.Results)];

        public ScriptedChatModel AnswerWith(string text)
        {
            _calls.Clear();
            _reply = _ => new ChatModelReply(text, [], "raw-answer");
            return this;
        }

        public ScriptedChatModel CallThenAnswer(string tool, string arguments, string text)
        {
            _calls.Clear();
            _reply = call => call == 0
                ? new ChatModelReply(null, [new FunctionCall("c1", tool, arguments)], "raw-step")
                : new ChatModelReply(text, [], "raw-answer");
            return this;
        }

        public Task<ChatModelReply> CompleteAsync(DateOnly today, IReadOnlyList<ChatTurn> turns, IReadOnlyList<ChatTool> tools,
            CancellationToken cancellationToken)
        {
            Tools = tools;
            _calls.Enqueue([.. turns]);
            return Task.FromResult(_reply(turns.OfType<ModelStepTurn>().Count()));
        }
    }
}
