using System.ComponentModel.DataAnnotations;

namespace KamraApp.Infrastructure.Llm;

// us2_minispec K10, ADR-0009: the "Llm" configuration section. Off by default; when on, the key is required, so a
// missing key stops the app at startup (AGENTS.md 5., fail fast) instead of failing on the first sentence.
public sealed class LlmOptions : IValidatableObject
{
    public const string Section = "Llm";

    public bool Enabled { get; set; }

    // From the environment (Llm__ApiKey), never from a committed file (AGENTS.md 7.).
    public string? ApiKey { get; set; }

    public string Model { get; set; } = "gemini-3.1-flash-lite";

    // us6_minispec K8: the chat's model; null means the same as Model.
    public string? ChatModel { get; set; }

    // The e2e stack points this at the LLM stub (us2_minispec K16).
    public Uri BaseUrl { get; set; } = new("https://generativelanguage.googleapis.com/");

    // QA-3: 15 seconds per attempt, one retry.
    [Range(1, 120)]
    public int TimeoutSeconds { get; set; } = 15;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Enabled && string.IsNullOrWhiteSpace(ApiKey))
        {
            yield return new ValidationResult("Llm:Enabled is true, but Llm:ApiKey is missing.", [nameof(ApiKey)]);
        }
    }
}
