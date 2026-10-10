namespace KamraApp.Domain.Pantry;

// CONTEXT.md: "soon expiring" = expires today, tomorrow or the day after; an expired item is not.
public static class Expiry
{
    public const int SoonExpiringDays = 2;

    public static bool IsExpired(DateOnly expiryDate, DateOnly today) => throw new NotImplementedException();

    public static bool IsSoonExpiring(DateOnly expiryDate, DateOnly today) => throw new NotImplementedException();
}
