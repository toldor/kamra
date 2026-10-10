using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using KamraApp.Api.Auth;
using KamraApp.Api.ErrorHandling;
using KamraApp.Application.Ingredients;
using KamraApp.Application.Pantry;
using KamraApp.Infrastructure;
using Scalar.AspNetCore;
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
// Enums travel as camelCase names ("dkg", "dairy"), numbers only as JSON numbers (not "200"). The OpenAPI
// generator reads the HTTP JSON options, MVC serializes with its own, so both are configured (V-26).
var enumsAsNames = new JsonStringEnumConverter(JsonNamingPolicy.CamelCase);
builder.Services.AddControllersWithViews()
    .ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true)
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(enumsAsNames);
        options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict;
    });
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(enumsAsNames);
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
});
// Build-time OpenAPI generation (ADR-0007) starts the app without deployment configuration; only then
// does it get a placeholder connection string, so the fail-fast check stays strict at runtime.
if (Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider")
{
    builder.Configuration["ConnectionStrings:Default"] ??= "Host=localhost;Database=openapi-generation";
}

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddKamraAuthentication(builder.Configuration);
// US-1 use cases (ADR-0004: one class per use case, no mediator).
builder.Services.AddScoped<ListIngredients>();
builder.Services.AddScoped<ListPantryItems>();
builder.Services.AddScoped<AddPantryItem>();
builder.Services.AddScoped<UpdatePantryItem>();
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
        // Only the Scalar page (Development) needs more: its inline scripts carry a per-request nonce, and
        // its UI sets inline styles, which a nonce cannot cover (V-27).
        context.Response.Headers.ContentSecurityPolicy = context.Items[ScalarOptions.NonceHttpContextItemKey] is string nonce
            ? $"default-src 'self'; script-src 'self' 'nonce-{nonce}'; style-src 'self' 'unsafe-inline'"
            : "default-src 'self'";
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
// ADR-0007 (developer decision): the API reference and the OpenAPI document exist in Development only.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithNonce().DisableDefaultFonts());
}

app.MapControllers();

app.Run();
