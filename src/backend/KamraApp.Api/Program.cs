using System.Reflection;
using KamraApp.Api.Auth;
using KamraApp.Api.ErrorHandling;
using KamraApp.Infrastructure;
using Serilog;
using Serilog.Context;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// ADR-0011: JSON logs on stdout; levels come from the "Serilog" configuration section.
builder.Services.AddSerilog((services, logger) => logger
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(new RenderedCompactJsonFormatter()));

// ADR-0004: validation runs in the use cases, so MVC's automatic model validation is switched off.
// The antiforgery filters are registered by the "with views" variant; no views are used.
builder.Services.AddControllersWithViews().ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true);
// Build-time OpenAPI generation (ADR-0007) starts the app without deployment configuration; only then
// does it get a placeholder connection string, so the fail-fast check stays strict at runtime.
if (Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider")
{
    builder.Configuration["ConnectionStrings:Default"] ??= "Host=localhost;Database=openapi-generation";
}

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddKamraAuthentication(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<AppExceptionHandler>();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = AppExceptionHandler.Customize);

var app = builder.Build();

// The correlation id is always server-generated; a client-supplied value would allow log injection.
// Headers are set on OnStarting because the exception handler clears the response before writing the error.
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Correlation-Id"] = context.TraceIdentifier;
        context.Response.Headers.ContentSecurityPolicy = "default-src 'self'";
        context.Response.Headers.XContentTypeOptions = "nosniff";
        return Task.CompletedTask;
    });
    using (LogContext.PushProperty("correlationId", context.TraceIdentifier))
    {
        await next(context);
    }
});
app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();

// ADR-0006: the built SPA (src/frontend, `npm run build`) is served from wwwroot on the same origin.
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();

app.Run();
