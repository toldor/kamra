using System.Text.Json.Nodes;
using KamraApp.Application.Chat;
using KamraApp.Application.Common;
using KamraApp.Domain.Cookings;
using KamraApp.Domain.Ingredients;
using KamraApp.Domain.Recipes;

namespace KamraApp.Unit.Tests;

// The test namespace KamraApp.Unit would hide the Domain's Unit type.
using Unit = KamraApp.Domain.Quantities.Unit;

// US-6 AskChat with a scripted model and fake tools (the LLM is never called in tests, AGENTS.md 6.):
// validation and the switch (us6_minispec K3, K8), the bounded tool loop (ADR-0010 4.: at most 3 model calls,
// at most 4 tool calls per round, unknown tool or refused arguments end the question), the model's own steps
// going back unchanged (thought signatures), and the tools used in the answer (K2).
public class AskChatTests
{
    // 00:30 in Budapest on Tuesday 2026-11-10, still 2026-11-09 in UTC: "today" is the Budapest day.
    private static readonly DateTimeOffset Now = new(2026, 11, 9, 23, 30, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 11, 10);

    private readonly ScriptedModel _model = new();
    private readonly FakeTools _tools = new();

    [Fact]
    public async Task A_switched_off_chat_is_LLM_DISABLED_and_nothing_is_called()
    {
        _model.Enabled = false;

        var error = await FluentActions.Awaiting(() => AskAsync("Mi jár le?")).Should().ThrowAsync<UnavailableException>();

        error.Which.Code.Should().Be("LLM_DISABLED");
        _model.Calls.Should().BeEmpty();
        _tools.ListCount.Should().Be(0);
    }

    [Fact]
    public void Availability_follows_the_switch()
    {
        _model.Enabled = false;
        Chat().Availability().Enabled.Should().BeFalse();

        _model.Enabled = true;
        Chat().Availability().Enabled.Should().BeTrue();
    }

    public static TheoryData<string, AskChatRequest?> InvalidRequests => new()
    {
        { "no request", null },
        { "no question", new AskChatRequest() },
        { "blank question", new AskChatRequest { Question = "   " } },
        { "question of 501 characters", new AskChatRequest { Question = new string('a', 501) } },
        { "11 earlier messages", new AskChatRequest { Question = "Mi van?", History = [.. Enumerable.Repeat(User("szia"), 11)] } },
        { "a message without a role", new AskChatRequest { Question = "Mi van?", History = [new ChatHistoryMessage { Text = "szia" }] } },
        { "a blank earlier message", new AskChatRequest { Question = "Mi van?", History = [User("  ")] } },
        { "an earlier message of 2001 characters", new AskChatRequest { Question = "Mi van?", History = [User(new string('a', 2001))] } },
        { "a missing earlier message", new AskChatRequest { Question = "Mi van?", History = [null] } },
    };

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task An_invalid_request_is_VALIDATION_FAILED_and_the_model_is_not_called(string because, AskChatRequest? request)
    {
        var error = await FluentActions.Awaiting(() => Chat().ExecuteAsync(request, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>(because);

        error.Which.Code.Should().Be("VALIDATION_FAILED");
        _model.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Ten_earlier_messages_and_a_question_of_500_characters_are_accepted()
    {
        _model.Reply(Text("Rendben."));

        var answer = await Chat().ExecuteAsync(new AskChatRequest
        {
            Question = new string('a', 500),
            History = [.. Enumerable.Repeat(User(new string('b', 2000)), 10)],
        }, CancellationToken.None);

        answer.Answer.Should().Be("Rendben.");
    }

    [Fact]
    public async Task A_direct_answer_is_one_model_call_with_the_history_the_question_and_the_tools()
    {
        _model.Reply(Text("  Szia! Miben segíthetek?  "));

        var answer = await AskAsync("Szia", User("Mi jár le?"), Assistant("A tej holnap."));

        answer.Answer.Should().Be("Szia! Miben segíthetek?");
        answer.ToolsUsed.Should().BeEmpty();
        answer.Recipe.Should().BeNull();
        var call = _model.Calls.Should().ContainSingle().Subject;
        call.Today.Should().Be(Today);
        call.Turns.Should().Equal(new UserTurn("Mi jár le?"), new AssistantTurn("A tej holnap."), new UserTurn("Szia"));
        // receptotlet_minispec Ü4: the MCP tools, then propose_recipe of the chat round.
        call.Tools.Should().Equal([.. _tools.Tools, ProposeRecipe.Tool]);
        _tools.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task A_tool_round_sends_the_models_step_back_unchanged_with_the_results()
    {
        _tools.Results["get_expiring_items"] = new ChatToolOutcome(false, "{\"items\":[{\"name\":\"tej\"}]}");
        _tools.Results["get_recommendations"] = new ChatToolOutcome(false, "{\"makeable\":[]}");
        _model.Reply(Step("step-1 {\"thoughtSignature\":\"abc\"}", Call("c1", "get_expiring_items", "{}"),
            Call("c2", "get_recommendations", "{\"max_results\":3}")));
        _model.Reply(Text("A tej holnap lejár."));

        var answer = await AskAsync("Mi jár le?");

        answer.Answer.Should().Be("A tej holnap lejár.");
        answer.ToolsUsed.Should().Equal("get_expiring_items", "get_recommendations");
        _tools.Calls.Should().Equal(("get_expiring_items", "{}"), ("get_recommendations", "{\"max_results\":3}"));
        _model.Calls.Should().HaveCount(2);
        var turns = _model.Calls[1].Turns;
        turns.Should().HaveCount(3);
        turns.Take(2).Should().Equal(new UserTurn("Mi jár le?"), new ModelStepTurn("step-1 {\"thoughtSignature\":\"abc\"}"));
        turns[2].Should().BeOfType<ToolResultsTurn>().Which.Results.Should().Equal(
            new ToolResult("c1", "get_expiring_items", "{\"items\":[{\"name\":\"tej\"}]}"),
            new ToolResult("c2", "get_recommendations", "{\"makeable\":[]}"));
    }

    // External review X7: a reply with a text and calls is a tool round; its text is not the answer.
    [Fact]
    public async Task A_reply_with_text_and_calls_runs_the_calls_and_answers_with_the_next_reply()
    {
        _model.Reply(new ChatModelReply("Megnézem…", [Call("c1", "get_expiring_items", "{}")], "step-1"));
        _model.Reply(Text("A tej holnap lejár."));

        var answer = await AskAsync("Mi jár le?");

        answer.Answer.Should().Be("A tej holnap lejár.");
        _tools.Calls.Should().ContainSingle();
    }

    [Fact]
    public async Task Two_tool_rounds_and_an_answer_are_allowed_and_each_tool_is_named_once()
    {
        _model.Reply(Step("step-1", Call("c1", "get_expiring_items", "{}")));
        _model.Reply(Step("step-2", Call("c2", "get_expiring_items", "{}"), Call("c3", "get_shopping_list", "{}")));
        _model.Reply(Text("Vegyél tejet."));

        var answer = await AskAsync("Kell tej?");

        answer.Answer.Should().Be("Vegyél tejet.");
        answer.ToolsUsed.Should().Equal("get_expiring_items", "get_shopping_list");
        _model.Calls.Should().HaveCount(3);
        _model.Calls[2].Turns.OfType<ModelStepTurn>().Select(t => t.RawContent).Should().Equal("step-1", "step-2");
        _model.Calls[2].Turns.OfType<ToolResultsTurn>().Should().HaveCount(2);
        _tools.ListCount.Should().Be(1, "the tools are listed once per question");
    }

    [Fact]
    public async Task A_third_tool_request_is_LLM_INVALID_RESPONSE_after_three_model_calls()
    {
        _model.Reply(Step("step-1", Call("c1", "get_expiring_items", "{}")));
        _model.Reply(Step("step-2", Call("c2", "get_pantry_items", "{}")));
        _model.Reply(Step("step-3", Call("c3", "get_shopping_list", "{}")));

        var error = await FluentActions.Awaiting(() => AskAsync("Mi van?")).Should().ThrowAsync<BadGatewayException>();

        error.Which.Code.Should().Be("LLM_INVALID_RESPONSE");
        _model.Calls.Should().HaveCount(AskChat.MaxModelCalls);
        _tools.Calls.Select(c => c.Name).Should().Equal("get_expiring_items", "get_pantry_items");
    }

    [Fact]
    public async Task Four_tool_calls_in_a_round_are_allowed()
    {
        _model.Reply(Step("step-1", Call("c1", "get_pantry_items", "{}"), Call("c2", "get_expiring_items", "{}"),
            Call("c3", "get_recommendations", "{}"), Call("c4", "get_shopping_list", "{}")));
        _model.Reply(Text("Minden rendben."));

        var answer = await AskAsync("Mi a helyzet?");

        answer.ToolsUsed.Should().HaveCount(4);
        _tools.Calls.Should().HaveCount(4);
    }

    [Fact]
    public async Task Five_tool_calls_in_a_round_are_LLM_INVALID_RESPONSE_and_no_tool_runs()
    {
        _model.Reply(Step("step-1", Call("c1", "get_pantry_items", "{}"), Call("c2", "get_expiring_items", "{}"),
            Call("c3", "get_recommendations", "{}"), Call("c4", "get_shopping_list", "{}"), Call("c5", "get_pantry_items", "{}")));

        var error = await FluentActions.Awaiting(() => AskAsync("Mi a helyzet?")).Should().ThrowAsync<BadGatewayException>();

        error.Which.Code.Should().Be("LLM_INVALID_RESPONSE");
        _tools.Calls.Should().BeEmpty();
        _model.Calls.Should().ContainSingle();
    }

    [Fact]
    public async Task An_unknown_tool_is_LLM_INVALID_RESPONSE_and_no_tool_of_the_round_runs()
    {
        _model.Reply(Step("step-1", Call("c1", "get_expiring_items", "{}"), Call("c2", "delete_pantry_items", "{}")));

        var error = await FluentActions.Awaiting(() => AskAsync("Töröld a tejet!")).Should().ThrowAsync<BadGatewayException>();

        error.Which.Code.Should().Be("LLM_INVALID_RESPONSE");
        _tools.Calls.Should().BeEmpty();
        _model.Calls.Should().ContainSingle();
    }

    [Fact]
    public async Task A_tool_refusing_its_arguments_is_LLM_INVALID_RESPONSE_and_the_model_is_not_called_again()
    {
        _tools.Results["get_pantry_items"] = new ChatToolOutcome(true, "An error occurred invoking 'get_pantry_items'.");
        _model.Reply(Step("step-1", Call("c1", "get_pantry_items", "{\"category\":\"nincs\"}")));

        var error = await FluentActions.Awaiting(() => AskAsync("Milyen tejtermék van?")).Should().ThrowAsync<BadGatewayException>();

        error.Which.Code.Should().Be("LLM_INVALID_RESPONSE");
        _model.Calls.Should().ContainSingle();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task An_answer_without_text_and_without_calls_is_LLM_INVALID_RESPONSE(string? text)
    {
        _model.Reply(new ChatModelReply(text, [], "raw"));

        var error = await FluentActions.Awaiting(() => AskAsync("Mi van?")).Should().ThrowAsync<BadGatewayException>();

        error.Which.Code.Should().Be("LLM_INVALID_RESPONSE");
    }

    [Fact]
    public async Task An_outage_of_the_model_is_passed_on()
    {
        _model.Reply(() => throw ChatErrors.LlmUnavailable());

        var error = await FluentActions.Awaiting(() => AskAsync("Mi van?")).Should().ThrowAsync<UnavailableException>();

        error.Which.Code.Should().Be("LLM_UNAVAILABLE");
    }

    // ---- receptotlet_minispec 2.-3.: propose_recipe ends the question with the answer and the recipe idea ----

    [Fact]
    public async Task A_proposed_recipe_is_the_answer_with_the_idea_and_it_never_reaches_the_tools()
    {
        _model.Reply(Step("step-1", Call("c1", "get_pantry_items", "{}")));
        _model.Reply(Step("step-2", Call("c2", ProposeRecipe.Name, Idea().ToJsonString())));

        var answer = await AskAsync("Adj egy receptet a cukkinihez.");

        answer.Answer.Should().Be("Itt egy ötlet a cukkinihez.");
        answer.ToolsUsed.Should().Equal("get_pantry_items");
        answer.Recipe.Should().BeEquivalentTo(new RecipeIdea("Rakott cukkini", 4,
            [new RecipeIdeaIngredient("cukkini", 1, Unit.Kg), new RecipeIdeaIngredient("tejföl", 2.5m, Unit.Dl)],
            "Szeleteld fel a cukkinit, rétegezd a tejföllel, és süsd meg."));
        _tools.Calls.Select(c => c.Name).Should().Equal("get_pantry_items");
        _model.Calls.Should().HaveCount(2);
    }

    [Fact]
    public async Task A_recipe_proposed_in_the_third_model_call_is_accepted_as_the_final_answer()
    {
        _model.Reply(Step("step-1", Call("c1", "get_pantry_items", "{}")));
        _model.Reply(Step("step-2", Call("c2", "get_expiring_items", "{}")));
        _model.Reply(Step("step-3", Call("c3", ProposeRecipe.Name, Idea().ToJsonString())));

        var answer = await AskAsync("Mit főzzek a lejárókból?");

        answer.Recipe.Should().NotBeNull();
        answer.ToolsUsed.Should().Equal("get_pantry_items", "get_expiring_items");
        _model.Calls.Should().HaveCount(AskChat.MaxModelCalls);
    }

    [Fact]
    public async Task A_recipe_idea_at_the_limits_of_a_hand_written_recipe_is_accepted()
    {
        var idea = Idea(i =>
        {
            i["name"] = new string('n', Recipe.MaxNameLength);
            i["servings"] = Cooking.MaxServings;
            i["instructions"] = new string('e', Recipe.MaxInstructionsLength);
            i["answer"] = new string('a', ProposeRecipe.AnswerMaxLength);
            i["ingredients"] = new JsonArray([.. Enumerable.Range(1, Recipe.MaxIngredients).Select(n => (JsonNode)Row(
                n == 1 ? new string('h', Ingredient.MaxNameLength) : $"hozzávaló {n}", n == 2 ? 0.001m : 100_000m, "db"))]);
        });
        _model.Reply(Step("step-1", Call("c1", ProposeRecipe.Name, idea.ToJsonString())));

        var answer = await AskAsync("Adj egy nagy receptet.");

        answer.Answer.Should().HaveLength(ProposeRecipe.AnswerMaxLength);
        answer.Recipe!.Name.Should().HaveLength(Recipe.MaxNameLength);
        answer.Recipe.Ingredients.Should().HaveCount(Recipe.MaxIngredients);
        answer.Recipe.Ingredients[1].Amount.Should().Be(0.001m);
    }

    [Fact]
    public async Task A_recipe_idea_without_instructions_text_is_accepted_like_a_hand_written_one()
    {
        _model.Reply(Step("step-1", Call("c1", ProposeRecipe.Name, Idea(i => i["instructions"] = "").ToJsonString())));

        var answer = await AskAsync("Adj egy receptet.");

        answer.Recipe!.Instructions.Should().BeEmpty();
    }

    public static TheoryData<string, string> InvalidIdeas => new()
    {
        { "not an object", "[]" },
        { "not JSON", "{\"name\":" },
        { "an unknown unit", Idea(i => i["ingredients"]![0]!["unit"] = "csipet").ToJsonString() },
        { "a missing unit", Idea(i => i["ingredients"]![0]!.AsObject().Remove("unit")).ToJsonString() },
        { "a name of 101 characters", Idea(i => i["name"] = new string('n', 101)).ToJsonString() },
        { "a blank name", Idea(i => i["name"] = "   ").ToJsonString() },
        { "a missing name", Idea(i => i.Remove("name")).ToJsonString() },
        { "0 servings", Idea(i => i["servings"] = 0).ToJsonString() },
        { "101 servings", Idea(i => i["servings"] = Cooking.MaxServings + 1).ToJsonString() },
        { "servings as text", Idea(i => i["servings"] = "négy").ToJsonString() },
        { "no ingredients", Idea(i => i["ingredients"] = new JsonArray()).ToJsonString() },
        { "51 ingredients", Idea(i => i["ingredients"] = new JsonArray([.. Enumerable.Range(1, 51).Select(n => (JsonNode)Row($"h{n}", 1, "db"))])).ToJsonString() },
        { "a missing ingredient row", Idea(i => i["ingredients"]!.AsArray().Add(null)).ToJsonString() },
        { "a blank ingredient name", Idea(i => i["ingredients"]![0]!["name"] = " ").ToJsonString() },
        { "an ingredient name of 101 characters", Idea(i => i["ingredients"]![0]!["name"] = new string('h', 101)).ToJsonString() },
        { "an amount of 0", Idea(i => i["ingredients"]![0]!["amount"] = 0).ToJsonString() },
        { "an amount with 4 decimals", Idea(i => i["ingredients"]![0]!["amount"] = 0.0005m).ToJsonString() },
        { "an amount above 100 000", Idea(i => i["ingredients"]![0]!["amount"] = 100_001).ToJsonString() },
        { "instructions of 4001 characters", Idea(i => i["instructions"] = new string('e', 4001)).ToJsonString() },
        { "missing instructions", Idea(i => i.Remove("instructions")).ToJsonString() },
        { "a missing answer", Idea(i => i.Remove("answer")).ToJsonString() },
        { "a blank answer", Idea(i => i["answer"] = "  ").ToJsonString() },
        { "an answer of 2001 characters", Idea(i => i["answer"] = new string('a', 2001)).ToJsonString() },
    };

    [Theory]
    [MemberData(nameof(InvalidIdeas))]
    public async Task A_recipe_idea_outside_the_schema_is_LLM_INVALID_RESPONSE(string because, string arguments)
    {
        _model.Reply(Step("step-1", Call("c1", ProposeRecipe.Name, arguments)));

        var error = await FluentActions.Awaiting(() => AskAsync("Adj egy receptet.")).Should().ThrowAsync<BadGatewayException>(because);

        error.Which.Code.Should().Be("LLM_INVALID_RESPONSE");
        _model.Calls.Should().ContainSingle();
    }

    [Fact]
    public async Task Propose_recipe_with_another_call_in_the_round_is_LLM_INVALID_RESPONSE_and_no_tool_runs()
    {
        _model.Reply(Step("step-1", Call("c1", "get_pantry_items", "{}"), Call("c2", ProposeRecipe.Name, Idea().ToJsonString())));

        var error = await FluentActions.Awaiting(() => AskAsync("Adj egy receptet.")).Should().ThrowAsync<BadGatewayException>();

        error.Which.Code.Should().Be("LLM_INVALID_RESPONSE");
        _tools.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Two_recipe_ideas_in_a_round_are_LLM_INVALID_RESPONSE()
    {
        _model.Reply(Step("step-1", Call("c1", ProposeRecipe.Name, Idea().ToJsonString()), Call("c2", ProposeRecipe.Name, Idea().ToJsonString())));

        var error = await FluentActions.Awaiting(() => AskAsync("Adj két receptet.")).Should().ThrowAsync<BadGatewayException>();

        error.Which.Code.Should().Be("LLM_INVALID_RESPONSE");
    }

    // The model sees the same limits that the server checks.
    [Fact]
    public void The_schema_of_propose_recipe_states_the_limits_of_the_check()
    {
        var tool = ProposeRecipe.Tool;
        var schema = JsonNode.Parse(tool.InputSchemaJson)!;
        var properties = schema["properties"]!;
        var row = properties["ingredients"]!["items"]!;

        tool.Name.Should().Be("propose_recipe");
        tool.Description.Should().NotBeNullOrWhiteSpace();
        Strings(schema["required"]).Should().BeEquivalentTo("name", "servings", "ingredients", "instructions", "answer");
        ((int)properties["name"]!["maxLength"]!).Should().Be(Recipe.MaxNameLength);
        ((int)properties["servings"]!["minimum"]!, (int)properties["servings"]!["maximum"]!).Should().Be((1, Cooking.MaxServings));
        ((int)properties["ingredients"]!["minItems"]!, (int)properties["ingredients"]!["maxItems"]!).Should().Be((1, Recipe.MaxIngredients));
        ((int)properties["instructions"]!["maxLength"]!).Should().Be(Recipe.MaxInstructionsLength);
        ((int)properties["answer"]!["maxLength"]!).Should().Be(ProposeRecipe.AnswerMaxLength);
        Strings(row["required"]).Should().BeEquivalentTo("name", "amount", "unit");
        ((int)row["properties"]!["name"]!["maxLength"]!).Should().Be(Ingredient.MaxNameLength);
        Strings(row["properties"]!["unit"]!["enum"]).Should().Equal("g", "dkg", "kg", "ml", "dl", "l", "db");
    }

    // A valid idea with untidy spaces; change transforms a copy.
    private static JsonObject Idea(Action<JsonObject>? change = null)
    {
        var idea = new JsonObject
        {
            ["name"] = "  Rakott   cukkini ",
            ["servings"] = 4,
            ["ingredients"] = new JsonArray(Row(" cukkini ", 1, "kg"), Row("tejföl", 2.5m, "dl")),
            ["instructions"] = "Szeleteld fel a cukkinit, rétegezd a tejföllel, és süsd meg.",
            ["answer"] = " Itt egy ötlet a cukkinihez. ",
        };
        change?.Invoke(idea);
        return idea;
    }

    private static JsonObject Row(string name, decimal amount, string unit) => new()
    {
        ["name"] = name,
        ["amount"] = amount,
        ["unit"] = unit,
    };

    private static string?[] Strings(JsonNode? array) => [.. array!.AsArray().Select(n => (string?)n)];

    private Task<AskChatResponse> AskAsync(string question, params ChatHistoryMessage[] history) =>
        Chat().ExecuteAsync(new AskChatRequest { Question = question, History = history }, CancellationToken.None);

    private AskChat Chat() => new(_model, _tools, new FixedTime(Now));

    private static ChatHistoryMessage User(string text) => new() { Role = ChatRole.User, Text = text };

    private static ChatHistoryMessage Assistant(string text) => new() { Role = ChatRole.Assistant, Text = text };

    private static ChatModelReply Text(string text) => new(text, [], $"raw:{text}");

    private static ChatModelReply Step(string raw, params FunctionCall[] calls) => new(null, calls, raw);

    private static FunctionCall Call(string id, string name, string args) => new(id, name, args);

    private sealed class ScriptedModel : IChatModel
    {
        private readonly Queue<Func<ChatModelReply>> _replies = new();

        public bool Enabled { get; set; } = true;

        public List<(DateOnly Today, IReadOnlyList<ChatTurn> Turns, IReadOnlyList<ChatTool> Tools)> Calls { get; } = [];

        public void Reply(ChatModelReply reply) => _replies.Enqueue(() => reply);

        public void Reply(Func<ChatModelReply> reply) => _replies.Enqueue(reply);

        public Task<ChatModelReply> CompleteAsync(DateOnly today, IReadOnlyList<ChatTurn> turns, IReadOnlyList<ChatTool> tools,
            CancellationToken cancellationToken)
        {
            Calls.Add((today, [.. turns], tools));
            return Task.FromResult(_replies.Dequeue()());
        }
    }

    private sealed class FakeTools : IChatTools
    {
        public List<ChatTool> Tools { get; } =
        [
            new("get_pantry_items", "Készlet", "{}"),
            new("get_expiring_items", "Hamarosan lejáró", "{}"),
            new("get_recommendations", "Ajánlás", "{}"),
            new("get_shopping_list", "Bevásárlólista", "{}"),
        ];

        public Dictionary<string, ChatToolOutcome> Results { get; } = [];

        public List<(string Name, string Args)> Calls { get; } = [];

        public int ListCount { get; private set; }

        public Task<IReadOnlyList<ChatTool>> ListAsync(CancellationToken cancellationToken)
        {
            ListCount++;
            return Task.FromResult<IReadOnlyList<ChatTool>>(Tools);
        }

        public Task<ChatToolOutcome> CallAsync(string name, string argumentsJson, CancellationToken cancellationToken)
        {
            Calls.Add((name, argumentsJson));
            return Task.FromResult(Results.GetValueOrDefault(name, new ChatToolOutcome(false, $"{{\"tool\":\"{name}\"}}")));
        }
    }
}
