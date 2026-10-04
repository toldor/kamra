using KamraApp.Domain.Households;

namespace KamraApp.Unit.Tests;

public class HouseholdTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 3, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_assigns_a_version_7_id_owner_and_creation_time()
    {
        var owner = Guid.CreateVersion7();

        var household = Household.Create(owner, Now);

        household.Id.Version.Should().Be(7, "ADR-0005: GUID v7 generated in the Domain");
        household.OwnerUserId.Should().Be(owner);
        household.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public void Create_rejects_an_empty_owner()
    {
        var create = () => Household.Create(Guid.Empty, Now);

        create.Should().Throw<ArgumentException>();
    }
}
