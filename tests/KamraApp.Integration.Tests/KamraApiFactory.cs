using System.Diagnostics.CodeAnalysis;
using KamraApp.Application.Common;
using KamraApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace KamraApp.Integration.Tests;

// One Postgres container (same image as Docker Compose, ADR-0005) and one app instance for the whole
// "api" collection; migrations run once, tests isolate themselves with unique data.
public sealed class KamraApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18").Build();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<KamraDbContext>().Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", _postgres.GetConnectionString());
        builder.ConfigureServices(services =>
            services.AddControllers().AddApplicationPart(typeof(TestErrorsController).Assembly));
    }
}

[CollectionDefinition("api")]
public sealed class ApiTestGroup : ICollectionFixture<KamraApiFactory>;

// Test-only endpoints that trigger the error paths; registered only by KamraApiFactory.
[ApiController]
[SuppressMessage("Performance", "CA1822", Justification = "MVC actions must be instance methods to be discovered.")]
[Route("api/v1/test-errors")]
public sealed class TestErrorsController : ControllerBase
{
    public const string SecretDetail = "connection string Password=hunter2 at Npgsql.Internal";

    [HttpGet("unexpected")]
    public IActionResult ThrowUnexpected() => throw new InvalidOperationException(SecretDetail);

    [HttpGet("bad-request")]
    public IActionResult ThrowBadRequest() => throw new BadHttpRequestException(SecretDetail);

    [HttpGet("conflict")]
    public IActionResult ThrowConflict() => throw new TestConflictException();
}

public sealed class TestConflictException()
    : AppException(ErrorCategory.Conflict, "TEST_CONFLICT", "Teszt ütközés.");
