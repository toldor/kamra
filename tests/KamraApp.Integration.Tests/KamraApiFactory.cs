using System.Diagnostics.CodeAnalysis;
using KamraApp.Application.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace KamraApp.Integration.Tests;

public sealed class KamraApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureServices(services =>
            services.AddControllers().AddApplicationPart(typeof(TestErrorsController).Assembly));
}

// Test-only endpoints that trigger the error paths; registered only by KamraApiFactory.
[ApiController]
[SuppressMessage("Performance", "CA1822", Justification = "MVC actions must be instance methods to be discovered.")]
[Route("api/v1/test-errors")]
public sealed class TestErrorsController : ControllerBase
{
    public const string SecretDetail = "connection string Password=hunter2 at Npgsql.Internal";

    [HttpGet("unexpected")]
    public IActionResult ThrowUnexpected() => throw new InvalidOperationException(SecretDetail);

    [HttpGet("conflict")]
    public IActionResult ThrowConflict() => throw new TestConflictException();
}

public sealed class TestConflictException()
    : AppException(ErrorCategory.Conflict, "TEST_CONFLICT", "Teszt ütközés.");
