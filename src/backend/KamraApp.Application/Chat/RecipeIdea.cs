using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Nodes;
using KamraApp.Application.Pantry;
using KamraApp.Domain.Cookings;
using KamraApp.Domain.Ingredients;
using KamraApp.Domain.Quantities;
using KamraApp.Domain.Recipes;

namespace KamraApp.Application.Chat;

// receptotlet_minispec 2.-3.: a recipe idea of the chat, for the "Új recept" form; nothing is saved here.
public sealed record RecipeIdea(string Name, int Servings, IReadOnlyList<RecipeIdeaIngredient> Ingredients, string Instructions);

public sealed record RecipeIdeaIngredient(string Name, decimal Amount, Unit Unit);

// receptotlet_minispec Ü4: propose_recipe is a function of the chat round only, not an MCP tool: it reads and
// writes nothing, the server checks its arguments against the limits of a hand-written recipe and passes them on
// (AGENTS.md 5.: LLM output is checked before use). The schema states the same limits for the model.
public static class ProposeRecipe
{
    public const string Name = "propose_recipe";

    // An answer goes back as an earlier message of the next question (AskChatRequest.HistoryTextMaxLength).
    public const int AnswerMaxLength = AskChatRequest.HistoryTextMaxLength;

    private const string Description =
        "Give a recipe idea when the user asks for a recipe. Use it instead of a text answer; the user reviews the idea on the "
        + "new recipe form and saves it. It saves nothing.";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static ChatTool Tool { get; } = new(Name, Description, Schema().ToJsonString());

    // The answer and the recipe, or null when the arguments are outside the schema.
    public static (string Answer, RecipeIdea Recipe)? Read(string argumentsJson)
    {
        Arguments? arguments;
        try
        {
            arguments = JsonSerializer.Deserialize<Arguments>(argumentsJson, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        if (arguments is null || !Validator.TryValidateObject(arguments, new ValidationContext(arguments), null, validateAllProperties: true))
        {
            return null;
        }

        return (arguments.Answer!.Trim(), new RecipeIdea(Ingredient.Tidy(arguments.Name!), arguments.Servings!.Value,
            [.. arguments.Ingredients!.Select(row => new RecipeIdeaIngredient(Ingredient.Tidy(row!.Name!), row.Amount!.Value,
                PantryInput.Parse<Unit>(row.Unit!)))],
            arguments.Instructions!));
    }

    private static JsonObject Schema() => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["name"] = Text(Recipe.MaxNameLength, "The name of the dish, in Hungarian."),
            ["servings"] = new JsonObject
            {
                ["type"] = "integer",
                ["minimum"] = 1,
                ["maximum"] = Cooking.MaxServings,
            },
            ["ingredients"] = new JsonObject
            {
                ["type"] = "array",
                ["minItems"] = 1,
                ["maxItems"] = Recipe.MaxIngredients,
                ["items"] = new JsonObject
                {
                    ["type"] = "object",
                    ["properties"] = new JsonObject
                    {
                        ["name"] = Text(Ingredient.MaxNameLength, "The ingredient, in Hungarian, in the singular (for example \"tojás\")."),
                        ["amount"] = new JsonObject
                        {
                            ["type"] = "number",
                            ["minimum"] = 0.001,
                            ["maximum"] = PantryInput.MaxAmount,
                            // Code review R3: the same rule as a hand-written amount (PantryInput.IsValidAmount).
                            ["description"] = "The amount for the given servings, with at most 3 decimals (for example 0.333).",
                        },
                        ["unit"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["enum"] = new JsonArray([.. Enum.GetNames<Unit>().Select(unit => (JsonNode)unit.ToLowerInvariant())]),
                        },
                    },
                    ["required"] = new JsonArray("name", "amount", "unit"),
                },
            },
            ["instructions"] = Text(Recipe.MaxInstructionsLength, "The steps, in Hungarian, as plain text."),
            ["answer"] = Text(AnswerMaxLength, "A short message in Hungarian shown above the idea."),
        },
        ["required"] = new JsonArray("name", "servings", "ingredients", "instructions", "answer"),
    };

    private static JsonObject Text(int maxLength, string description) => new()
    {
        ["type"] = "string",
        ["maxLength"] = maxLength,
        ["description"] = description,
    };

    // The arguments as the model sent them; a wrong JSON type fails the deserializing, the rest is checked here.
    private sealed class Arguments : IValidatableObject
    {
        public string? Name { get; init; }

        public int? Servings { get; init; }

        public IReadOnlyList<Row?>? Ingredients { get; init; }

        public string? Instructions { get; init; }

        public string? Answer { get; init; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var rows = Ingredients ?? [];
            var valid = Ingredient.Tidy(Name ?? "").Length is > 0 and <= Recipe.MaxNameLength
                && Servings is >= 1 and <= Cooking.MaxServings
                && rows.Count is > 0 and <= Recipe.MaxIngredients
                && rows.All(row => row is { Amount: { } amount }
                    && Ingredient.Tidy(row.Name ?? "").Length is > 0 and <= Ingredient.MaxNameLength
                    && PantryInput.IsValidAmount(amount, minimum: 0.001m)
                    && PantryInput.TryParse<Unit>(row.Unit, out _))
                && Instructions is { Length: <= Recipe.MaxInstructionsLength }
                && Answer?.Trim() is { Length: > 0 and <= AnswerMaxLength };
            if (!valid)
            {
                yield return new ValidationResult("The recipe idea is outside the schema.");
            }
        }
    }

    private sealed class Row
    {
        public string? Name { get; init; }

        public decimal? Amount { get; init; }

        public string? Unit { get; init; }
    }
}
