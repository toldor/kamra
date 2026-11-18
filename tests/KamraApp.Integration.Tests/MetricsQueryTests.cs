using KamraApp.Domain.Cookings;
using KamraApp.Domain.Households;
using KamraApp.Domain.Ingredients;
using KamraApp.Domain.ItemProposals;
using KamraApp.Domain.Pantry;
using KamraApp.Domain.Quantities;
using KamraApp.Domain.Recommendations;
using KamraApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace KamraApp.Integration.Tests;

// CAP-12 (metrics.md, Mérési terv 1.): docs/01_product/metrics.sql against synthetic data with hand-calculated
// results. Every metric gets its own database: the queries read all households, so shared data would leak in.
public sealed class MetricsQueryTests(MetricsDatabases databases) : IClassFixture<MetricsDatabases>
{
    private static readonly DateOnly AsOf = new(2026, 11, 16);

    private static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task North_star_counts_saved_cookings_per_active_household_and_week()
    {
        await using var data = await Scenario.CreateAsync(databases);
        var tomi = data.Household(At(10, 1));
        var betti = data.Household(At(10, 1));
        var dora = data.Household(At(10, 1));
        var feri = data.Household(At(10, 1));
        var soon = Expiry.SoonExpiringDays;

        data.Cook(tomi, At(11, 3), Day(11, 3).AddDays(soon)); // N1: the last soon-expiring day
        data.Cook(tomi, At(11, 6), new DateOnly(2027, 1, 1)); // N2: not soon
        data.Cook(betti, At(11, 7), Day(11, 7).AddDays(soon + 1)); // N3: one day too late
        data.Cook(tomi, At(11, 9, 0, 30), Day(11, 9)); // N4: still Nov 8 in UTC, but in the second week
        data.Cook(tomi, At(11, 10), Day(11, 10), Day(11, 11)); // N5: two soon items, one saved cooking
        data.Cook(tomi, At(11, 12), Day(11, 11)); // N6: already expired
        data.Cook(tomi, At(11, 16, 0, 30), Day(11, 16)); // N7: still Nov 15 in UTC, but after the window

        data.Item(dora, 100, Day(12, 1), At(11, 13)); // an entry makes the household active
        var old = data.Item(feri, 100, Day(12, 1), At(10, 2));
        data.Change(old, 50, MovementReason.Consumed, At(11, 13)); // a manual decrease does not

        var weeks = (await data.ReadAsync(AsOf)).NorthStar;

        weeks.Should().Equal(
            new NorthStarWeek(Day(11, 2), 1, 2, 0.50m),
            new NorthStarWeek(Day(11, 9), 2, 2, 1.00m));
    }

    [Fact]
    public async Task Waste_is_the_average_wasted_share_of_the_items_expired_in_the_window()
    {
        await using var data = await Scenario.CreateAsync(databases);
        var home = data.Household(At(10, 1));
        var added = At(10, 20);

        var a = data.Item(home, 1000, Day(11, 5), added);
        data.Change(a, 0, MovementReason.Consumed, At(11, 1)); // 0%

        var b = data.Item(home, 400, Day(11, 6), added);
        data.Change(b, 300, MovementReason.Discarded, At(11, 2));
        data.Change(b, 0, MovementReason.Consumed, At(11, 3)); // 25%

        var c = data.Item(home, 500, Day(11, 7), added);
        data.Change(c, 250, MovementReason.Consumed, At(11, 4));
        data.Change(c, 0, MovementReason.Consumed, At(11, 17)); // after the window: 250 was still there, 50%

        var d = data.Item(home, 200, Day(11, 8), added);
        data.Change(d, 100, MovementReason.Corrected, At(10, 21));
        data.Change(d, 0, MovementReason.Discarded, At(11, 5)); // the original is 100: 100%

        var e = data.Item(home, 300, Day(11, 9), added);
        data.Change(e, 0, MovementReason.Corrected, At(10, 21)); // a recording error: left out

        var f = data.Item(home, 200, Day(11, 5), added);
        data.Change(f, 0, MovementReason.Consumed, At(11, 7)); // eaten after the expiry: 0%

        data.Item(home, 100, Day(11, 1), added); // before the window
        data.Item(home, 100, Day(11, 16), added); // has not expired on the evaluation day

        var edited = data.Item(home, 100, Day(11, 5), added);
        edited.UpdateDetails(Category.Dairy, Day(11, 20), Day(10, 20), At(11, 3)); // Ü1: its current expiry counts

        var waste = (await data.ReadAsync(AsOf)).Waste;

        waste.Should().Be(new WasteResult(5, 35.0m));
    }

    [Fact]
    public async Task Retention_counts_entries_not_items_in_the_first_14_days()
    {
        await using var data = await Scenario.CreateAsync(databases);

        var kept = data.Household(At(10, 20));
        data.Item(kept, 100, Day(12, 1), At(10, 21));
        data.Item(kept, 100, Day(12, 1), At(10, 22));
        data.Sentence(kept, At(10, 23), 60, 60, 60); // three items, one entry

        var oneSentence = data.Household(At(10, 25));
        data.Sentence(oneSentence, At(10, 26), 60, 60, 60);
        data.Item(oneSentence, 100, Day(12, 1), At(10, 27));
        data.RejectedSentence(oneSentence, At(10, 28));

        var late = data.Household(At(10, 28, 10));
        data.Item(late, 100, Day(12, 1), At(10, 29));
        data.Item(late, 100, Day(12, 1), At(10, 30));
        data.Item(late, 100, Day(12, 1), At(11, 11, 10)); // exactly 14 days later: outside

        var tooNew = data.Household(At(11, 10));
        for (var i = 0; i < 5; i++)
        {
            data.Item(tooNew, 100, Day(12, 1), At(11, 11));
        }

        var retention = (await data.ReadAsync(AsOf)).Retention;

        retention.Should().Be(new RetentionResult(3, 1, 33.3m));
    }

    [Fact]
    public async Task Entry_time_is_measured_per_sentence_to_its_last_approval()
    {
        await using var data = await Scenario.CreateAsync(databases);
        var home = data.Household(At(10, 1));

        data.Sentence(home, At(11, 3), 20);
        data.Sentence(home, At(11, 4), 10, 30); // two items: 30 seconds
        data.Sentence(home, At(11, 5), 40);
        data.Sentence(home, At(11, 6), 100);
        data.RejectedSentence(home, At(11, 7));
        data.Sentence(home, At(11, 1, 23, 59), 5); // approved before the window
        data.Sentence(home, At(11, 16), 5); // approved after it

        var time = (await data.ReadAsync(AsOf)).EntryTime;

        time.Should().Be(new EntryTimeResult(4, 35.0m, 82.0m));
    }

    [Fact]
    public async Task An_empty_database_gives_zero_counts_and_no_rates()
    {
        await using var data = await Scenario.CreateAsync(databases);

        var result = await data.ReadAsync(AsOf);

        result.NorthStar.Should().Equal(
            new NorthStarWeek(Day(11, 2), 0, 0, null),
            new NorthStarWeek(Day(11, 9), 0, 0, null));
        result.Waste.Should().Be(new WasteResult(0, null));
        result.Retention.Should().Be(new RetentionResult(0, 0, null));
        result.EntryTime.Should().Be(new EntryTimeResult(0, null, null));
    }

    [Fact]
    public async Task Without_a_date_the_queries_use_the_current_day_in_Budapest()
    {
        await using var data = await Scenario.CreateAsync(databases);
        var today = KamraApp.Application.Common.Today.Of(DateTimeOffset.UtcNow);
        var thisMonday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));

        var weeks = (await data.ReadAsync(asOf: null)).NorthStar;

        weeks.Select(w => w.WeekStart).Should().Equal(thisMonday.AddDays(-14), thisMonday.AddDays(-7));
    }

    private static DateOnly Day(int month, int day) => new(2026, month, day);

    // A Budapest wall-clock time as the UTC instant the application stores.
    private static DateTimeOffset At(int month, int day, int hour = 12, int minute = 0)
    {
        var budapest = TimeZoneInfo.FindSystemTimeZoneById("Europe/Budapest");
        var local = new DateTime(2026, month, day, hour, minute, 0, DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, budapest));
    }

    private sealed record NorthStarWeek(DateOnly WeekStart, long SavedCookings, long ActiveHouseholds, decimal? PerHousehold);

    private sealed record WasteResult(long Items, decimal? WastedPercent);

    private sealed record RetentionResult(long Eligible, long Retained, decimal? RetainedPercent);

    private sealed record EntryTimeResult(long Sentences, decimal? P50Seconds, decimal? P90Seconds);

    private sealed record MetricsResult(
        IReadOnlyList<NorthStarWeek> NorthStar, WasteResult Waste, RetentionResult Retention, EntryTimeResult EntryTime);

    // The synthetic data, built with the Domain's own factories at fixed times.
    private sealed class Scenario : IAsyncDisposable
    {
        private readonly string _connectionString;
        private readonly KamraDbContext _db;
        private readonly Ingredient _milk;
        private readonly Guid _recipeId;

        private Scenario(string connectionString, KamraDbContext db, Ingredient milk, Guid recipeId)
        {
            _connectionString = connectionString;
            _db = db;
            _milk = milk;
            _recipeId = recipeId;
        }

        public static async Task<Scenario> CreateAsync(MetricsDatabases databases)
        {
            var connectionString = await databases.CreateDatabaseAsync(Cancellation);
            var db = MetricsDatabases.Context(connectionString);
            var milk = await db.Ingredients.SingleAsync(i => i.SeedKey == "tej", Cancellation);
            var recipeId = await db.Recipes.Select(r => r.Id).FirstAsync(Cancellation);
            return new Scenario(connectionString, db, milk, recipeId);
        }

        public Guid Household(DateTimeOffset registeredAt)
        {
            var userId = Guid.CreateVersion7();
            _db.Users.Add(new AppUser
            {
                Id = userId,
                UserName = $"{userId}@example.com",
                NormalizedUserName = $"{userId}@EXAMPLE.COM",
                Email = $"{userId}@example.com",
                NormalizedEmail = $"{userId}@EXAMPLE.COM",
            });
            var household = Domain.Households.Household.Create(userId, registeredAt);
            _db.Households.Add(household);
            return household.Id;
        }

        public PantryItem Item(Guid household, decimal millilitres, DateOnly expiry, DateTimeOffset at)
        {
            var (item, movement) = PantryItem.Create(household, _milk, millilitres, Unit.Ml, Category.Dairy, expiry,
                KamraApp.Application.Common.Today.Of(at), at);
            _db.AddRange(item, movement);
            return item;
        }

        public void Change(PantryItem item, decimal millilitres, MovementReason reason, DateTimeOffset at) =>
            _db.Add(item.ChangeQuantity(millilitres, Unit.Ml, reason, at)!);

        // A cooking that uses up one fresh 100 ml item per given expiry date.
        public void Cook(Guid household, DateTimeOffset at, params DateOnly[] expiries)
        {
            var items = expiries.Select(e => Item(household, 100, e, at.AddDays(-30))).ToList();
            var amount = 100m * items.Count;
            var recipe = new RecipeCandidate(_recipeId, "Próba", 1, [new RecipeNeed(_milk.Id, _milk.Name, amount, Unit.Ml, IsStaple: false)]);
            var (cooking, movements) = Cooking.Create(household, recipe, 1, Guid.CreateVersion7(),
                new Dictionary<Guid, decimal> { [_milk.Id] = amount }, items, at);
            _db.Add(cooking);
            _db.AddRange(movements);
        }

        // One submitted sentence: an approved item per delay, each saved that many seconds after the submission.
        public void Sentence(Guid household, DateTimeOffset submittedAt, params int[] approvalDelaysInSeconds)
        {
            foreach (var delay in approvalDelaysInSeconds)
            {
                var approvedAt = submittedAt.AddSeconds(delay);
                var proposal = Proposal(household, submittedAt);
                proposal.Approve(Item(household, 100, Day(12, 1), approvedAt).Id, approvedAt);
                _db.Add(proposal);
            }
        }

        public void RejectedSentence(Guid household, DateTimeOffset submittedAt)
        {
            var proposal = Proposal(household, submittedAt);
            proposal.Reject(submittedAt.AddSeconds(15));
            _db.Add(proposal);
        }

        public async Task<MetricsResult> ReadAsync(DateOnly? asOf)
        {
            await _db.SaveChangesAsync(Cancellation);

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(Cancellation);
            if (asOf is { } day)
            {
                await using var set = new NpgsqlCommand("SELECT set_config('kamra.as_of', @day, false)", connection);
                set.Parameters.AddWithValue("day", day.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
                await set.ExecuteScalarAsync(Cancellation);
            }

            var sql = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "metrics.sql"), Cancellation);
            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(Cancellation);

            var weeks = new List<NorthStarWeek>();
            while (await reader.ReadAsync(Cancellation))
            {
                weeks.Add(new NorthStarWeek(reader.GetFieldValue<DateOnly>(1), reader.GetInt64(2), reader.GetInt64(3), Number(reader, 4)));
            }

            await NextRowAsync(reader);
            var waste = new WasteResult(reader.GetInt64(1), Number(reader, 2));
            await NextRowAsync(reader);
            var retention = new RetentionResult(reader.GetInt64(1), reader.GetInt64(2), Number(reader, 3));
            await NextRowAsync(reader);
            var time = new EntryTimeResult(reader.GetInt64(1), Number(reader, 2), Number(reader, 3));
            return new MetricsResult(weeks, waste, retention, time);
        }

        public async ValueTask DisposeAsync() => await _db.DisposeAsync();

        private static async Task NextRowAsync(NpgsqlDataReader reader)
        {
            (await reader.NextResultAsync(Cancellation)).Should().BeTrue();
            (await reader.ReadAsync(Cancellation)).Should().BeTrue();
        }

        private static decimal? Number(NpgsqlDataReader reader, int column) =>
            reader.IsDBNull(column) ? null : reader.GetDecimal(column);

        private ItemProposal Proposal(Guid household, DateTimeOffset submittedAt) =>
            ItemProposal.Create(household, "tej", _milk.Id, 100, Unit.Ml, Day(12, 1), expiryEstimated: false, expiryText: null, submittedAt);
    }
}

// One PostgreSQL container with a migrated template; each test gets its own empty copy.
public sealed class MetricsDatabases : IAsyncLifetime
{
    private const string Template = "metrics_template";
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18").Build();

    public static KamraDbContext Context(string connectionString) =>
        new(new DbContextOptionsBuilder<KamraDbContext>().UseNpgsql(connectionString).Options);

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        await ExecuteAsync($"""CREATE DATABASE "{Template}" """);
        await using var db = Context(ConnectionString(Template));
        await db.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync() => await _postgres.DisposeAsync();

    public async Task<string> CreateDatabaseAsync(CancellationToken cancellationToken)
    {
        var name = $"metrics_{Guid.NewGuid():N}";
        await ExecuteAsync($"""CREATE DATABASE "{name}" TEMPLATE "{Template}" """, cancellationToken);
        return ConnectionString(name);
    }

    // No pooling: a pooled connection to the template would block copying it.
    private string ConnectionString(string database) =>
        new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString())
        {
            Database = database,
            Pooling = false,
        }.ConnectionString;

    private async Task ExecuteAsync(string sql, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(ConnectionString("postgres"));
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
