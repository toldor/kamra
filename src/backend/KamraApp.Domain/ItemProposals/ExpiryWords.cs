using System.Globalization;
using System.Text.RegularExpressions;

namespace KamraApp.Domain.ItemProposals;

// US-2 / ADR-0009: the LLM returns the expiry words as the user wrote them; the date comes from here, so the
// rules of the scope contract are deterministic and tested. Null: not a recognised form, the caller estimates.
// Recognised (us2_minispec K6): ma, holnap, holnapután; a weekday (the next one, or today); "nov. 20.",
// "november 20", "11.08.", "2026.11.15." - with case endings (-ig, -ra, -n, -án...) and without accents.
public static partial class ExpiryWords
{
    // Case endings after the word: holnap-ig, holnap-ra, kedd-en, szerdá-n, szombat-on.
    private static readonly string[] Endings = ["", "ig", "ra", "re", "n", "on", "en", "an", "in"];

    private static readonly (string Word, DayOfWeek Day)[] Weekdays =
    [
        ("hetfo", DayOfWeek.Monday),
        ("kedd", DayOfWeek.Tuesday),
        ("szerda", DayOfWeek.Wednesday),
        ("csutortok", DayOfWeek.Thursday),
        ("pentek", DayOfWeek.Friday),
        ("szombat", DayOfWeek.Saturday),
        ("vasarnap", DayOfWeek.Sunday),
    ];

    private static readonly Dictionary<string, int> Months = new(StringComparer.Ordinal)
    {
        ["jan"] = 1,
        ["januar"] = 1,
        ["feb"] = 2,
        ["febr"] = 2,
        ["februar"] = 2,
        ["mar"] = 3,
        ["marc"] = 3,
        ["marcius"] = 3,
        ["apr"] = 4,
        ["aprilis"] = 4,
        ["maj"] = 5,
        ["majus"] = 5,
        ["jun"] = 6,
        ["junius"] = 6,
        ["jul"] = 7,
        ["julius"] = 7,
        ["aug"] = 8,
        ["augusztus"] = 8,
        ["szep"] = 9,
        ["szept"] = 9,
        ["szeptember"] = 9,
        ["okt"] = 10,
        ["oktober"] = 10,
        ["nov"] = 11,
        ["november"] = 11,
        ["dec"] = 12,
        ["december"] = 12,
    };

    public static DateOnly? Resolve(string? text, DateOnly today)
    {
        var folded = Words.Fold(text);
        if (folded.Length == 0)
        {
            return null;
        }

        if (Is(folded, "ma"))
        {
            return today;
        }

        // "holnaputan" before "holnap": the longer word first.
        if (Is(folded, "holnaputan"))
        {
            return today.AddDays(2);
        }

        if (Is(folded, "holnap"))
        {
            return today.AddDays(1);
        }

        foreach (var (word, day) in Weekdays)
        {
            if (Is(folded, word))
            {
                return today.AddDays(((int)day - (int)today.DayOfWeek + 7) % 7);
            }
        }

        // "nov. 20-án", "11.08-ig", and without the hyphen "nov. 20án": the case ending does not change the date.
        var date = CaseEnding().Replace(folded.Split('-')[0], "$1").TrimEnd('.', ' ');

        if (FullDate().Match(date) is { Success: true } full)
        {
            return Date(Number(full, 1), Number(full, 2), Number(full, 3));
        }

        if (NumericDate().Match(date) is { Success: true } numeric)
        {
            return Nearest(Number(numeric, 1), Number(numeric, 2), today);
        }

        if (NamedDate().Match(date) is { Success: true } named && Months.TryGetValue(named.Groups[1].Value, out var month))
        {
            return Nearest(month, Number(named, 2), today);
        }

        return null;
    }

    private static bool Is(string folded, string word) =>
        folded.StartsWith(word, StringComparison.Ordinal) && Endings.Contains(folded[word.Length..]);

    // No year given: the nearest such day from today on (scope_contract.md US-2, us2_minispec K6).
    private static DateOnly? Nearest(int month, int day, DateOnly today) =>
        Date(today.Year, month, day) is { } thisYear && thisYear >= today ? thisYear : Date(today.Year + 1, month, day);

    private static DateOnly? Date(int year, int month, int day) =>
        month is >= 1 and <= 12 && day >= 1 && day <= DateTime.DaysInMonth(year, month) ? new DateOnly(year, month, day) : null;

    private static int Number(Match match, int group) => int.Parse(match.Groups[group].Value, CultureInfo.InvariantCulture);

    [GeneratedRegex(@"(\d)[a-z]+$")]
    private static partial Regex CaseEnding();

    [GeneratedRegex(@"^(\d{4})\. ?(\d{1,2})\. ?(\d{1,2})$")]
    private static partial Regex FullDate();

    [GeneratedRegex(@"^(\d{1,2})\. ?(\d{1,2})$")]
    private static partial Regex NumericDate();

    [GeneratedRegex(@"^([a-z]+)\.? ?(\d{1,2})$")]
    private static partial Regex NamedDate();
}
