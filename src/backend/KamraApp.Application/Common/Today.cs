namespace KamraApp.Application.Common;

// The product is Hungarian only, so "today" (soon expiring, estimated expiry) is the calendar day in
// Budapest, not the UTC date: between midnight and 1-2 a.m. the two differ.
public static class Today
{
    public static DateOnly Of(TimeProvider time) => throw new NotImplementedException();
}
