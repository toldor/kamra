using System.ComponentModel.DataAnnotations;

namespace KamraApp.Application.ItemProposals;

// us2_minispec K5: the sentence is required and at most 500 characters (cost and abuse).
public sealed class SubmitItemProposalsRequest : IValidatableObject
{
    public const int TextMaxLength = 500;
    public const string TextMessage = "Írd le, mit vettél.";
    public const string TextLengthMessage = "Legfeljebb 500 karaktert írj.";

    [Required(ErrorMessage = TextMessage)]
    public string? Text { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Text))
        {
            yield return new ValidationResult(TextMessage, [nameof(Text)]);
        }
        else if (Text.Length > TextMaxLength)
        {
            yield return new ValidationResult(TextLengthMessage, [nameof(Text)]);
        }
    }
}
