using KamraApp.Domain.Quantities;

namespace KamraApp.Application.Chat;

// receptotlet_minispec 2.-3.: a recipe idea of the chat, for the "Új recept" form; nothing is saved here.
public sealed record RecipeIdea(string Name, int Servings, IReadOnlyList<RecipeIdeaIngredient> Ingredients, string Instructions);

public sealed record RecipeIdeaIngredient(string Name, decimal Amount, Unit Unit);

// receptotlet_minispec Ü4: propose_recipe is a function of the chat round only, not an MCP tool: it reads and
// writes nothing, the server checks its arguments against the limits of a hand-written recipe and passes them on.
public static class ProposeRecipe
{
    public const string Name = "propose_recipe";

    // An answer goes back as an earlier message of the next question (AskChatRequest.HistoryTextMaxLength).
    public const int AnswerMaxLength = AskChatRequest.HistoryTextMaxLength;

    public static ChatTool Tool => throw new NotImplementedException();

    // The answer and the recipe, or null when the arguments are outside the schema.
    public static (string Answer, RecipeIdea Recipe)? Read(string argumentsJson) => throw new NotImplementedException();
}
