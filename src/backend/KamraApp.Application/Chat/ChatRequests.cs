using System.ComponentModel.DataAnnotations;

namespace KamraApp.Application.Chat;

public enum ChatRole
{
    User,
    Assistant,
}

// us6_minispec K3: an earlier message of the conversation, as text only. The client keeps the conversation;
// the server stores nothing (ADR-0010 5.).
public sealed class ChatHistoryMessage
{
    public ChatRole? Role { get; init; }

    public string? Text { get; init; }
}

// us6_minispec K3: the question is required and at most 500 characters; at most 10 earlier messages, each a
// user or assistant text of 1-2000 characters (an answer can be longer than a question).
public sealed class AskChatRequest : IValidatableObject
{
    public const int QuestionMaxLength = 500;
    public const int HistoryMaxCount = 10;
    public const int HistoryTextMaxLength = 2000;
    public const string QuestionMessage = "Írd le a kérdésed.";
    public const string QuestionLengthMessage = "Legfeljebb 500 karaktert írj.";
    public const string HistoryMessage = "Az előzmény legfeljebb 10 üzenet lehet, mindegyik szerepkörrel és legfeljebb 2000 karakteres szöveggel.";

    [Required(ErrorMessage = QuestionMessage)]
    public string? Question { get; init; }

    public IReadOnlyList<ChatHistoryMessage?>? History { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Question))
        {
            yield return new ValidationResult(QuestionMessage, [nameof(Question)]);
        }
        else if (Question.Length > QuestionMaxLength)
        {
            yield return new ValidationResult(QuestionLengthMessage, [nameof(Question)]);
        }

        if (History is { } history && (history.Count > HistoryMaxCount || history.Any(m =>
                m?.Role is null || string.IsNullOrWhiteSpace(m.Text) || m.Text.Length > HistoryTextMaxLength)))
        {
            yield return new ValidationResult(HistoryMessage, [nameof(History)]);
        }
    }
}
