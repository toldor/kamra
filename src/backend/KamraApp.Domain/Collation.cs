using System.Globalization;

namespace KamraApp.Domain;

public static class Collation
{
    // Hungarian alphabetical order ("á" right after "a") for every list the user reads.
    public static readonly StringComparer Hungarian = StringComparer.Create(CultureInfo.GetCultureInfo("hu-HU"), ignoreCase: false);
}
