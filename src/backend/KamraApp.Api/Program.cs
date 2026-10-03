using KamraApp.Api.ErrorHandling;
using Serilog;
using Serilog.Context;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// ADR-0011: JSON logs on stdout; levels come from the "Serilog" configuration section.
builder.Services.AddSerilog((services, logger) => logger
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(new RenderedCompactJsonFormatter()));

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddExceptionHandler<AppExceptionHandler>();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    context.ProblemDetails.Extensions["correlationId"] = context.HttpContext.TraceIdentifier;
    // Framework-generated errors (e.g. unknown route) get a stable code too (ADR-0007).
    if (!context.ProblemDetails.Extensions.ContainsKey("code"))
    {
        context.ProblemDetails.Extensions["code"] = context.ProblemDetails.Status switch
        {
            StatusCodes.Status404NotFound => "NOT_FOUND",
            StatusCodes.Status405MethodNotAllowed => "METHOD_NOT_ALLOWED",
            _ => AppExceptionHandler.InternalErrorCode,
        };
    }
});

var app = builder.Build();

// The correlation id is always server-generated; a client-supplied value would allow log injection.
// Headers are set on OnStarting because the exception handler clears the response before writing the error.
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Correlation-Id"] = context.TraceIdentifier;
        context.Response.Headers.ContentSecurityPolicy = "default-src 'self'";
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

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
