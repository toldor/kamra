using System.Globalization;
using KamraApp.Domain.ItemProposals;

namespace KamraApp.Unit.Tests;

// The test namespace KamraApp.Unit would hide the Domain's Unit type.
using Unit = KamraApp.Domain.Quantities.Unit;

// US-2 in the Domain: the expiry words to a date (us2_minispec K6), the unit words to the fixed units (K7, ADR-0002)
// and the states of an item proposal (K2, ADR-0007, ADR-0008 4.). The LLM only extracts; these rules decide.
public class ItemProposalTests
{
    // A Saturday, so "szombat" is today and "a hét napja, ha ma van, a mai" (scope_contract.md US-2) is tested.
    private static readonly DateOnly Today = new(2026, 11, 7);
    private static readonly DateTimeOffset Now = new(2026, 11, 7, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid HouseholdId = Guid.CreateVersion7();

    [Theory]
    [InlineData("ma", "2026-11-07")]
    [InlineData("holnap", "2026-11-08")]
    [InlineData("holnapig", "2026-11-08")]
    [InlineData("holnapra", "2026-11-08")]
    [InlineData("holnapután", "2026-11-09")]
    [InlineData("holnaputanig", "2026-11-09")]
    [InlineData("Holnapután", "2026-11-09")]
    public void Relative_days_count_from_the_day_of_submission(string text, string expected) =>
        ExpiryText.Resolve(text, Today).Should().Be(DateOnly.Parse(expected, CultureInfo.InvariantCulture));

    [Theory]
    [InlineData("szombat", "2026-11-07")]      // today is Saturday: today
    [InlineData("szombatig", "2026-11-07")]
    [InlineData("vasárnap", "2026-11-08")]
    [InlineData("hétfőn", "2026-11-09")]
    [InlineData("hetfoig", "2026-11-09")]
    [InlineData("kedden", "2026-11-10")]
    [InlineData("szerdáig", "2026-11-11")]
    [InlineData("csutortokig", "2026-11-12")]
    [InlineData("péntekig", "2026-11-13")]
    [InlineData("Pénteken", "2026-11-13")]
    public void A_weekday_is_the_next_such_day_or_today(string text, string expected) =>
        ExpiryText.Resolve(text, Today).Should().Be(DateOnly.Parse(expected, CultureInfo.InvariantCulture));

    [Theory]
    [InlineData("nov. 20.", "2026-11-20")]
    [InlineData("nov. 20-án", "2026-11-20")]
    [InlineData("november 20-ig", "2026-11-20")]
    [InlineData("NOV 20", "2026-11-20")]
    [InlineData("nov. 7.", "2026-11-07")]       // today
    [InlineData("dec. 31-ig", "2026-12-31")]
    [InlineData("11.08.", "2026-11-08")]
    [InlineData("11.08", "2026-11-08")]
    [InlineData("11.08-ig", "2026-11-08")]
    [InlineData("10.03", "2027-10-03")]         // already past this year: the next one
    [InlineData("okt. 3.", "2027-10-03")]
    [InlineData("jan. 5.", "2027-01-05")]
    [InlineData("január 5-én", "2027-01-05")]
    [InlineData("szept. 1.", "2027-09-01")]
    [InlineData("marcius 2.", "2027-03-02")]
    [InlineData("2026.11.15.", "2026-11-15")]
    [InlineData("2026.11.15-ig", "2026-11-15")]
    [InlineData("2025.11.15.", "2025-11-15")]   // a year is taken as written, even in the past
    public void An_absolute_date_without_a_year_is_the_nearest_one_from_today(string text, string expected) =>
        ExpiryText.Resolve(text, Today).Should().Be(DateOnly.Parse(expected, CultureInfo.InvariantCulture));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("jövő hétfőig")]
    [InlineData("két nap múlva")]
    [InlineData("3 nap múlva")]
    [InlineData("egy hétig jó")]
    [InlineData("hamar")]
    [InlineData("02.30.")]   // not a date
    [InlineData("13.08.")]   // no 13th month
    [InlineData("nov. 31.")]
    [InlineData("2026.02.30.")]
    public void Anything_else_is_not_recognised_so_the_expiry_is_estimated(string? text) =>
        ExpiryText.Resolve(text, Today).Should().BeNull();

    [Theory]
    [InlineData("g", Unit.G)]
    [InlineData("gramm", Unit.G)]
    [InlineData("gr", Unit.G)]
    [InlineData("dkg", Unit.Dkg)]
    [InlineData("deka", Unit.Dkg)]
    [InlineData("deko", Unit.Dkg)]
    [InlineData("dekagramm", Unit.Dkg)]
    [InlineData("kg", Unit.Kg)]
    [InlineData("kiló", Unit.Kg)]
    [InlineData("kilo", Unit.Kg)]
    [InlineData("KILÓ", Unit.Kg)]
    [InlineData("kilogramm", Unit.Kg)]
    [InlineData("ml", Unit.Ml)]
    [InlineData("milliliter", Unit.Ml)]
    [InlineData("dl", Unit.Dl)]
    [InlineData("deci", Unit.Dl)]
    [InlineData("deciliter", Unit.Dl)]
    [InlineData("l", Unit.L)]
    [InlineData("liter", Unit.L)]
    [InlineData(" Liter ", Unit.L)]
    [InlineData("db", Unit.Db)]
    [InlineData("darab", Unit.Db)]
    public void Unit_words_map_to_the_fixed_units(string word, Unit expected) =>
        UnitWords.Map(word).Should().Be(expected);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("doboz")]
    [InlineData("csomag")]
    [InlineData("üveg")]
    [InlineData("pohár")]
    [InlineData("tucat")]
    [InlineData("szelet")]   // ADR-0002: not a unit of the system
    public void Other_words_are_no_unit(string? word) =>
        UnitWords.Map(word).Should().BeNull();

    [Fact]
    public void A_new_proposal_is_pending_with_its_submission_time()
    {
        var ingredientId = Guid.CreateVersion7();

        var proposal = ItemProposal.Create(HouseholdId, "2 liter tejet", ingredientId, 2m, Unit.L, Today.AddDays(7), true, null, Now);

        proposal.Id.Should().NotBeEmpty();
        proposal.HouseholdId.Should().Be(HouseholdId);
        proposal.Text.Should().Be("2 liter tejet");
        proposal.IngredientId.Should().Be(ingredientId);
        proposal.Amount.Should().Be(2m);
        proposal.Unit.Should().Be(Unit.L);
        proposal.ExpiryDate.Should().Be(Today.AddDays(7));
        proposal.ExpiryEstimated.Should().BeTrue();
        proposal.State.Should().Be(ItemProposalState.Pending);
        proposal.SubmittedAt.Should().Be(Now);
        proposal.ResolvedAt.Should().BeNull();
        proposal.PantryItemId.Should().BeNull();
    }

    [Fact]
    public void A_proposal_may_miss_the_ingredient_the_amount_the_unit_and_the_expiry()
    {
        var proposal = ItemProposal.Create(HouseholdId, "egy üveg pesztó", null, null, null, null, false, null, Now);

        proposal.IngredientId.Should().BeNull();
        proposal.Amount.Should().BeNull();
        proposal.Unit.Should().BeNull();
        proposal.ExpiryDate.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_proposal_needs_the_text_it_came_from(string text)
    {
        var create = () => ItemProposal.Create(HouseholdId, text, null, null, null, null, false, null, Now);

        create.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void A_long_text_is_cut_to_the_column_length()
    {
        var proposal = ItemProposal.Create(HouseholdId, new string('a', 250), null, null, null, null, false, null, Now);

        proposal.Text.Should().HaveLength(ItemProposal.TextMaxLength);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void An_amount_must_be_positive(int amount)
    {
        var create = () => ItemProposal.Create(HouseholdId, "tej", null, amount, Unit.L, null, false, null, Now);

        create.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Approving_records_the_pantry_item_and_the_time()
    {
        var proposal = NewProposal();
        var pantryItemId = Guid.CreateVersion7();
        var later = Now.AddSeconds(30);

        proposal.Approve(pantryItemId, later).Should().Be(ItemProposalTransition.Changed);

        proposal.State.Should().Be(ItemProposalState.Approved);
        proposal.PantryItemId.Should().Be(pantryItemId);
        proposal.ResolvedAt.Should().Be(later);
    }

    [Fact]
    public void Approving_again_is_a_repeat_and_keeps_the_first_pantry_item()
    {
        var proposal = NewProposal();
        var first = Guid.CreateVersion7();
        proposal.Approve(first, Now);

        proposal.Approve(Guid.CreateVersion7(), Now.AddMinutes(1)).Should().Be(ItemProposalTransition.Repeated);

        proposal.PantryItemId.Should().Be(first);
        proposal.ResolvedAt.Should().Be(Now);
    }

    [Fact]
    public void Rejecting_records_the_time_and_no_pantry_item()
    {
        var proposal = NewProposal();

        proposal.Reject(Now).Should().Be(ItemProposalTransition.Changed);

        proposal.State.Should().Be(ItemProposalState.Rejected);
        proposal.ResolvedAt.Should().Be(Now);
        proposal.PantryItemId.Should().BeNull();
    }

    [Fact]
    public void Rejecting_again_is_a_repeat()
    {
        var proposal = NewProposal();
        proposal.Reject(Now);

        proposal.Reject(Now.AddMinutes(1)).Should().Be(ItemProposalTransition.Repeated);

        proposal.ResolvedAt.Should().Be(Now);
    }

    [Fact]
    public void Approving_a_rejected_proposal_is_a_conflict()
    {
        var proposal = NewProposal();
        proposal.Reject(Now);

        proposal.Approve(Guid.CreateVersion7(), Now).Should().Be(ItemProposalTransition.Conflict);

        proposal.State.Should().Be(ItemProposalState.Rejected);
        proposal.PantryItemId.Should().BeNull();
    }

    [Fact]
    public void Rejecting_an_approved_proposal_is_a_conflict()
    {
        var proposal = NewProposal();
        var pantryItemId = Guid.CreateVersion7();
        proposal.Approve(pantryItemId, Now);

        proposal.Reject(Now).Should().Be(ItemProposalTransition.Conflict);

        proposal.State.Should().Be(ItemProposalState.Approved);
        proposal.PantryItemId.Should().Be(pantryItemId);
    }

    private static ItemProposal NewProposal() =>
        ItemProposal.Create(HouseholdId, "2 liter tejet", Guid.CreateVersion7(), 2m, Unit.L, null, false, null, Now);
}
