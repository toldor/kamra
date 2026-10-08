namespace KamraApp.Application.Common;

// ADR-0004 / ADR-0006: the household of the signed-in user, taken from the authentication cookie's
// claim - never from request data or LLM output.
public interface ICurrentHousehold
{
    Guid HouseholdId { get; }
}

public static class KamraClaimTypes
{
    public const string HouseholdId = "household_id";
}
