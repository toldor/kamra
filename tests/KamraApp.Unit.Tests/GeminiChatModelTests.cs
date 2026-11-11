using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using KamraApp.Application.Chat;
using KamraApp.Application.Common;
using KamraApp.Infrastructure.Llm;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KamraApp.Unit.Tests;

// US-6 (us6_minispec K6-K8, K10; ADR-0010 4.): the Gemini chat adapter against a scripted HTTP handler (never the
// real API, AGENTS.md 6.). The request it sends (prompt chat.v1 with today's date, the conversation, the MCP tools
// as function declarations), the model's step sent back unchanged, the function calls it reads, the US-2 retry rules
// (QA-3) and a log without any text.
public sealed class GeminiChatModelTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 11, 10); // a Tuesday
    private static readonly ChatTool Expiring = new("get_expiring_items", "Hamarosan lejáró tételek", """{"type":"object","properties":{}}""");
    private static readonly ChatTool Recommendations = new("get_recommendations", "Ajánlás",
        """{"type":"object","properties":{"max_results":{"type":["integer","null"],"minimum":1,"maximum":10}}}""");

    private readonly ScriptedHandler _handler = new();
    private readonly CapturingLogger<GeminiChatModel> _logger = new();

    [Fact]
    public void The_adapter_is_on_and_the_chat_model_defaults_to_the_quick_entry_model()
    {
        Model().Enabled.Should().BeTrue();
        new LlmOptions().ChatModel.Should().BeNull("K8: Llm:ChatModel defaults to Llm:Model");
    }

    [Fact]
    public async Task It_sends_the_prompt_with_today_the_conversation_and_the_tools_to_the_configured_model()
    {
        _handler.Answer(Ok(Text("Szia!")));

        await Model().CompleteAsync(Today, [new UserTurn("Mi jár le?"), new AssistantTurn("A tej holnap."), new UserTurn("És még?")],
            [Expiring, Recommendations], CancellationToken.None);

        var request = _handler.Requests.Should().ContainSingle().Which;
        request.Url.Should().Be("https://llm.test/v1beta/models/gemini-3.1-flash-lite:generateContent");
        request.ApiKey.Should().Be("secret-test-key");
        var body = JsonNode.Parse(request.Body)!;
        var prompt = (string)body["system_instruction"]!["parts"]![0]!["text"]!;
        prompt.Should().StartWith("You are the read-only assistant of \"Kamra\"").And.Contain("Today is 2026-11-10 (Tuesday) in Budapest.")
            .And.NotContain("{today}").And.NotContain("{weekday}");
        var contents = body["contents"]!.AsArray();
        contents.Select(c => (string)c!["role"]!).Should().Equal("user", "model", "user");
        contents.Select(c => (string)c!["parts"]![0]!["text"]!).Should().Equal("Mi jár le?", "A tej holnap.", "És még?");
        var declarations = body["tools"]![0]!["functionDeclarations"]!.AsArray();
        declarations.Select(d => (string)d!["name"]!).Should().Equal("get_expiring_items", "get_recommendations");
        ((string)declarations[0]!["description"]!).Should().Be("Hamarosan lejáró tételek");
        JsonNode.DeepEquals(declarations[1]!["parametersJsonSchema"], JsonNode.Parse(Recommendations.InputSchemaJson)).Should().BeTrue(
            "the MCP schema goes to the model unchanged (parametersJsonSchema accepts JSON Schema)");
        var config = body["generationConfig"]!;
        ((string)config["thinkingConfig"]!["thinkingLevel"]!).Should().Be("minimal");
        ((int)config["maxOutputTokens"]!).Should().Be(1024);
    }

    [Fact]
    public async Task The_chat_model_setting_overrides_the_model()
    {
        _handler.Answer(Ok(Text("Szia!")));

        await Model(chatModel: "gemini-3.6-flash").CompleteAsync(Today, [new UserTurn("Szia")], [], CancellationToken.None);

        _handler.Requests.Single().Url.Should().Be("https://llm.test/v1beta/models/gemini-3.6-flash:generateContent");
    }

    [Fact]
    public async Task A_text_answer_is_returned_with_its_raw_content_and_the_thoughts_left_out()
    {
        var content = new JsonObject
        {
            ["role"] = "model",
            ["parts"] = new JsonArray(new JsonObject { ["text"] = "gondolkodom", ["thought"] = true }, new JsonObject { ["text"] = "A tej holnap lejár." }),
        };
        _handler.Answer(Ok(content));

        var reply = await Model().CompleteAsync(Today, [new UserTurn("Mi jár le?")], [Expiring], CancellationToken.None);

        reply.Text.Should().Be("A tej holnap lejár.");
        reply.Calls.Should().BeEmpty();
        JsonNode.DeepEquals(JsonNode.Parse(reply.RawContent), content).Should().BeTrue();
    }

    [Fact]
    public async Task Function_calls_are_read_with_their_ids_and_arguments()
    {
        var content = new JsonObject
        {
            ["role"] = "model",
            ["parts"] = new JsonArray(
                new JsonObject
                {
                    ["functionCall"] = new JsonObject { ["id"] = "c1", ["name"] = "get_recommendations", ["args"] = new JsonObject { ["max_results"] = 3 } },
                    ["thoughtSignature"] = "c2lnbmF0dXJl",
                },
                new JsonObject { ["functionCall"] = new JsonObject { ["name"] = "get_expiring_items" } }),
        };
        _handler.Answer(Ok(content));

        var reply = await Model().CompleteAsync(Today, [new UserTurn("Mit főzzek?")], [Expiring, Recommendations], CancellationToken.None);

        reply.Calls.Should().HaveCount(2);
        reply.Calls[0].Id.Should().Be("c1");
        reply.Calls[0].Name.Should().Be("get_recommendations");
        JsonNode.DeepEquals(JsonNode.Parse(reply.Calls[0].ArgumentsJson), new JsonObject { ["max_results"] = 3 }).Should().BeTrue();
        reply.Calls[1].Should().Be(new FunctionCall(null, "get_expiring_items", "{}"));
        ((string)JsonNode.Parse(reply.RawContent)!["parts"]![0]!["thoughtSignature"]!).Should().Be("c2lnbmF0dXJl");
    }

    [Fact]
    public async Task The_models_step_goes_back_unchanged_and_the_results_as_function_responses()
    {
        const string step = """{"role":"model","parts":[{"functionCall":{"id":"c1","name":"get_expiring_items","args":{}},"thoughtSignature":"c2ln"}]}""";
        _handler.Answer(Ok(Text("A tej holnap lejár.")));

        await Model().CompleteAsync(Today,
            [
                new UserTurn("Mi jár le?"),
                new ModelStepTurn(step),
                new ToolResultsTurn([new ToolResult("c1", "get_expiring_items", """{"items":[{"name":"tej"}],"truncated":false}""")]),
            ],
            [Expiring], CancellationToken.None);

        var contents = JsonNode.Parse(_handler.Requests.Single().Body)!["contents"]!.AsArray();
        JsonNode.DeepEquals(contents[1], JsonNode.Parse(step)).Should().BeTrue("ADR-0010 4.: exactly as received, thought signature included");
        ((string)contents[2]!["role"]!).Should().Be("user");
        var response = contents[2]!["parts"]![0]!["functionResponse"]!;
        ((string)response["id"]!).Should().Be("c1");
        ((string)response["name"]!).Should().Be("get_expiring_items");
        JsonNode.DeepEquals(response["response"], JsonNode.Parse("""{"items":[{"name":"tej"}],"truncated":false}""")).Should().BeTrue();
    }

    [Theory]
    [InlineData("nem JSON")]
    [InlineData("[1,2]")]
    public async Task A_tool_result_that_is_not_a_JSON_object_is_wrapped_in_one(string content)
    {
        _handler.Answer(Ok(Text("Rendben.")));

        await Model().CompleteAsync(Today,
            [new UserTurn("Mi van?"), new ModelStepTurn("""{"role":"model","parts":[]}"""), new ToolResultsTurn([new ToolResult(null, "get_expiring_items", content)])],
            [Expiring], CancellationToken.None);

        var response = JsonNode.Parse(_handler.Requests.Single().Body)!["contents"]![2]!["parts"]![0]!["functionResponse"]!["response"]!;
        response.GetValueKind().Should().Be(System.Text.Json.JsonValueKind.Object, "the API requires a JSON object");
        response["result"].Should().NotBeNull();
    }

    [Fact]
    public async Task A_timeout_is_retried_once()
    {
        _handler.Hang();
        _handler.Answer(Ok(Text("Szia!")));

        (await Ask()).Text.Should().Be("Szia!");
        _handler.Requests.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task An_outage_is_retried_once(HttpStatusCode status)
    {
        _handler.Answer(new HttpResponseMessage(status));
        _handler.Answer(Ok(Text("Szia!")));

        (await Ask()).Text.Should().Be("Szia!");
        _handler.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task A_network_error_is_retried_once()
    {
        _handler.Fail(new HttpRequestException("connection refused"));
        _handler.Answer(Ok(Text("Szia!")));

        (await Ask()).Text.Should().Be("Szia!");
        _handler.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task Two_outages_are_LLM_UNAVAILABLE()
    {
        _handler.Answer(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        _handler.Hang();

        var error = await FluentActions.Awaiting(Ask).Should().ThrowAsync<UnavailableException>();

        error.Which.Code.Should().Be("LLM_UNAVAILABLE");
        _handler.Requests.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.PaymentRequired)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task A_refused_request_is_LLM_UNAVAILABLE_without_a_retry(HttpStatusCode status)
    {
        _handler.Answer(new HttpResponseMessage(status));

        var error = await FluentActions.Awaiting(Ask).Should().ThrowAsync<UnavailableException>();

        error.Which.Code.Should().Be("LLM_UNAVAILABLE");
        _handler.Requests.Should().ContainSingle();
    }

    [Theory]
    [InlineData("nem is JSON")]
    [InlineData("""{"candidates":[]}""")]
    [InlineData("""{"candidates":[{"finishReason":"STOP"}]}""")]
    [InlineData("""{"candidates":[{"content":{"role":"model","parts":[{"text":"Szia"}]},"finishReason":"MAX_TOKENS"}]}""")]
    [InlineData("""{"candidates":[{"content":{"role":"model","parts":[]},"finishReason":"MALFORMED_FUNCTION_CALL"}]}""")]
    [InlineData("""{"candidates":[{"content":{"role":"model","parts":[]},"finishReason":"MISSING_THOUGHT_SIGNATURE"}]}""")]
    [InlineData("""{"candidates":[{"content":{"role":"model","parts":[{"text":"Szia"}]},"finishReason":"SAFETY"}]}""")]
    [InlineData("""{"candidates":[{"content":{"role":"model","parts":[{"functionCall":{"args":{}}}]},"finishReason":"STOP"}]}""")]
    [InlineData("""{"candidates":[{"content":{"role":"model","parts":[{"functionCall":{"name":"get_expiring_items","args":[1]}}]},"finishReason":"STOP"}]}""")]
    public async Task An_answer_outside_the_expected_shape_is_LLM_INVALID_RESPONSE_without_a_retry(string body)
    {
        _handler.Answer(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") });

        var error = await FluentActions.Awaiting(Ask).Should().ThrowAsync<BadGatewayException>();

        error.Which.Code.Should().Be("LLM_INVALID_RESPONSE");
        _handler.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task The_callers_cancellation_is_not_an_outage()
    {
        _handler.Hang();
        using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await FluentActions.Awaiting(() => Model().CompleteAsync(Today, [new UserTurn("Szia")], [], cancel.Token))
            .Should().ThrowAsync<OperationCanceledException>();

        _handler.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task The_log_has_the_model_the_prompt_and_the_tokens_but_never_any_text_or_the_key()
    {
        _handler.Answer(Ok(Text("A tejföl holnap lejár.")));

        await Model().CompleteAsync(Today,
            [new UserTurn("Mi jár le?"), new ModelStepTurn("""{"role":"model","parts":[]}"""),
             new ToolResultsTurn([new ToolResult("c1", "get_expiring_items", """{"items":[{"name":"paradicsom"}]}""")])],
            [Expiring], CancellationToken.None);

        var entry = _logger.Entries.Should().ContainSingle().Which;
        entry.Should().Contain("gemini-3.1-flash-lite").And.Contain(GeminiChatModel.PromptVersion).And.Contain("512").And.Contain("64");
        _logger.Entries.Should().NotContain(e => e.Contains("tejföl", StringComparison.Ordinal) || e.Contains("Mi jár le", StringComparison.Ordinal)
            || e.Contains("paradicsom", StringComparison.Ordinal) || e.Contains("secret-test-key", StringComparison.Ordinal));
    }

    public void Dispose() => _handler.Dispose();

    private Task<ChatModelReply> Ask() => Model().CompleteAsync(Today, [new UserTurn("Szia")], [Expiring], CancellationToken.None);

    private GeminiChatModel Model(string? chatModel = null) =>
        new(new HttpClient(_handler) { BaseAddress = new Uri("https://llm.test/") },
            Options.Create(new LlmOptions
            {
                Enabled = true,
                ApiKey = "secret-test-key",
                BaseUrl = new Uri("https://llm.test/"),
                TimeoutSeconds = 1,
                ChatModel = chatModel,
            }),
            _logger);

    private static JsonObject Text(string text) => new()
    {
        ["role"] = "model",
        ["parts"] = new JsonArray(new JsonObject { ["text"] = text }),
    };

    // A Gemini generateContent answer with the given content and token counts.
    private static HttpResponseMessage Ok(JsonObject content)
    {
        var body = new JsonObject
        {
            ["candidates"] = new JsonArray(new JsonObject { ["content"] = content.DeepClone(), ["finishReason"] = "STOP" }),
            ["usageMetadata"] = new JsonObject { ["promptTokenCount"] = 512, ["candidatesTokenCount"] = 64 },
        };
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json") };
    }
}
