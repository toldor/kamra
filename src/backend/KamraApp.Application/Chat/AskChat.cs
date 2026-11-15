using KamraApp.Application.Common;

namespace KamraApp.Application.Chat;

// US-6: one question of the read-only chat. The model may call the MCP tools of the own household; the loop is
// bounded (ADR-0010 4.): at most 3 model calls and at most 4 tool calls per round. A round with an unknown tool,
// too many calls or a refused call ends the question before any of its tools runs or the model is called again.
public sealed class AskChat(IChatModel model, IChatTools tools, TimeProvider time)
{
    public const int MaxModelCalls = 3;
    public const int MaxCallsPerRound = 4;

    public async Task<AskChatResponse> ExecuteAsync(AskChatRequest? request, CancellationToken cancellationToken)
    {
        var valid = RequestValidator.Validate(request);
        if (!model.Enabled)
        {
            throw ChatErrors.LlmDisabled();
        }

        var available = await tools.ListAsync(cancellationToken);
        var names = available.Select(t => t.Name).ToHashSet(StringComparer.Ordinal);
        // receptotlet_minispec Ü4: the model may also give a recipe idea; it is not an MCP tool and never runs.
        IReadOnlyList<ChatTool> offered = [.. available, ProposeRecipe.Tool];
        // K3, ADR-0010 5.: earlier messages go back as text only; the question is the last user turn.
        var turns = (valid.History ?? []).Select(m => m!.Role == ChatRole.User ? (ChatTurn)new UserTurn(m.Text!) : new AssistantTurn(m.Text!))
            .Append(new UserTurn(valid.Question!.Trim()))
            .ToList();
        var used = new List<string>();
        var today = Today.Of(time);

        for (var call = 1; ; call++)
        {
            var reply = await model.CompleteAsync(today, turns, offered, cancellationToken);
            if (reply.Calls.Count == 0)
            {
                return string.IsNullOrWhiteSpace(reply.Text)
                    ? throw ChatErrors.LlmInvalidResponse()
                    : new AskChatResponse(reply.Text.Trim(), used);
            }

            // receptotlet_minispec 3.: a recipe idea is the final answer, alone in its round, in the last model call too.
            if (reply.Calls.Any(c => c.Name == ProposeRecipe.Name))
            {
                var idea = reply.Calls.Count == 1 ? ProposeRecipe.Read(reply.Calls[0].ArgumentsJson) : null;
                return idea is var (answer, recipe) ? new AskChatResponse(answer, used, recipe) : throw ChatErrors.LlmInvalidResponse();
            }

            if (call == MaxModelCalls || reply.Calls.Count > MaxCallsPerRound || reply.Calls.Any(c => !names.Contains(c.Name)))
            {
                throw ChatErrors.LlmInvalidResponse();
            }

            var results = new List<ToolResult>(reply.Calls.Count);
            foreach (var functionCall in reply.Calls)
            {
                var outcome = await tools.CallAsync(functionCall.Name, functionCall.ArgumentsJson, cancellationToken);
                if (outcome.IsError)
                {
                    throw ChatErrors.LlmInvalidResponse();
                }

                results.Add(new ToolResult(functionCall.Id, functionCall.Name, outcome.Content));
                if (!used.Contains(functionCall.Name))
                {
                    used.Add(functionCall.Name);
                }
            }

            // The model's step goes back exactly as received (thought signatures), then the results.
            turns.Add(new ModelStepTurn(reply.RawContent));
            turns.Add(new ToolResultsTurn(results));
        }
    }

    public ChatAvailability Availability() => new(model.Enabled);
}
