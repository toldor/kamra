namespace KamraApp.Application.Common;

// The product is Hungarian only, so "today" (soon expiring, estimated expiry) is the calendar day in
// Budapest, not the UTC date: between midnight and 1-2 a.m. the two differ.
public static class Today
{
    private static readonly TimeZoneInfo Budapest = TimeZoneInfo.FindSystemTimeZoneById("Europe/Budapest");

    public static DateOnly Of(TimeProvider time) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(time.GetUtcNow(), Budapest).DateTime);

    // The Budapest calendar day of any instant, e.g. the entry day of a pantry item from its CreatedAt.
    public static DateOnly Of(DateTimeOffset instant) => throw new NotImplementedException();
}
