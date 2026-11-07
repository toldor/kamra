using System.Globalization;
using System.Text;

namespace KamraApp.Domain.ItemProposals;

internal static class Words
{
    // Lower case, no accents, single spaces: "Holnapután" and "holnaputan" are the same word (users type on phones).
    public static string Fold(string? text)
    {
        var decomposed = (text ?? "").Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var plain = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                plain.Append(ch);
            }
        }

        return string.Join(' ', plain.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
