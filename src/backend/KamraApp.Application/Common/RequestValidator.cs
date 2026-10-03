using System.ComponentModel.DataAnnotations;

namespace KamraApp.Application.Common;

// ADR-0004: validation runs in the use case (DataAnnotations), so the API and the MCP host validate
// the same way. Error keys are camelCase to match the JSON field names.
public static class RequestValidator
{
    public static T Validate<T>(T? request) where T : class
    {
        if (request is null)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [""] = ["A kérés hiányos vagy hibás formátumú."],
            });
        }

        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true))
        {
            throw new ValidationException(results
                .SelectMany(r => r.MemberNames.DefaultIfEmpty("").Select(member => (Field: CamelCase(member), Message: r.ErrorMessage ?? "")))
                .GroupBy(e => e.Field)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Message).ToArray()));
        }

        return request;
    }

    private static string CamelCase(string name) =>
        name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
