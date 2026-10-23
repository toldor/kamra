using System.Globalization;
using KamraApp.Application.Common;
using KamraApp.Domain.Pantry;

namespace KamraApp.Unit.Tests;

// US-1: estimated expiry from the category (data_model.md, Kategórialista) and the "soon expiring"
// window (CONTEXT.md: today, tomorrow or the day after; an expired item is not soon expiring).
public class ExpiryTests
{
    private static readonly DateOnly Day = new(2026, 10, 10);

    [Theory]
    [InlineData(Category.Dairy, 7)]
    [InlineData(Category.Cheese, 21)]
    [InlineData(Category.Eggs, 21)]
    [InlineData(Category.FreshMeatFish, 1)]
    [InlineData(Category.ProcessedMeat, 7)]
    [InlineData(Category.Vegetables, 3)]
    [InlineData(Category.Fruit, 2)]
    [InlineData(Category.Bakery, 1)]
    [InlineData(Category.DryGoods, 90)]
    [InlineData(Category.CannedAndSauces, 90)]
    [InlineData(Category.Frozen, 90)]
    [InlineData(Category.Beverages, 21)]
    [InlineData(Category.PreparedFood, 3)]
    public void Expiry_is_estimated_from_the_category(Category category, int days)
    {
        Categories.EstimateExpiry(category, Day).Should().Be(Day.AddDays(days));
    }

    [Fact]
    public void Other_category_has_no_estimate()
    {
        Categories.EstimateExpiry(Category.Other, Day).Should().BeNull();
    }

    [Theory]
    [InlineData(-1, false, true)]
    [InlineData(0, true, false)]
    [InlineData(1, true, false)]
    [InlineData(2, true, false)]
    [InlineData(3, false, false)]
    public void Soon_expiring_is_today_tomorrow_or_the_day_after(int daysFromToday, bool soonExpiring, bool expired)
    {
        var expiryDate = Day.AddDays(daysFromToday);

        Expiry.IsSoonExpiring(expiryDate, Day).Should().Be(soonExpiring);
        Expiry.IsExpired(expiryDate, Day).Should().Be(expired);
    }

    [Theory]
    [InlineData("2026-10-09T21:30:00Z", "2026-10-09")] // 23:30 in Budapest (CEST, UTC+2)
    [InlineData("2026-10-09T23:30:00Z", "2026-10-10")] // 01:30 next day in Budapest
    [InlineData("2026-12-31T23:30:00Z", "2027-01-01")] // 00:30 in Budapest (CET, UTC+1)
    [InlineData("2026-12-31T22:30:00Z", "2026-12-31")] // 23:30 in Budapest
    public void Today_is_the_calendar_day_in_Budapest(string utcNow, string expected)
    {
        var time = new FixedTime(DateTimeOffset.Parse(utcNow, CultureInfo.InvariantCulture));

        Today.Of(time).Should().Be(DateOnly.Parse(expected, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Today_of_an_instant_is_the_Budapest_day()
    {
        // The entry day of a pantry item created at 23:30 UTC is the next day in Budapest.
        Today.Of(new DateTimeOffset(2026, 10, 9, 23, 30, 0, TimeSpan.Zero)).Should().Be(new DateOnly(2026, 10, 10));
    }
}
