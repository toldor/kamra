using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KamraApp.Application.ItemProposals;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ScriptedParser = KamraApp.Integration.Tests.ItemProposalApiTests.ScriptedParser;

namespace KamraApp.Integration.Tests;

// US-2 adversarial tests (Antigravity, 2026-11-08; the cases the API tests did not cover yet). Prompt injection is modelled
// as a deceived model: the scripted parser returns the malicious output, the API must not let it through.
[Collection("api")]
public class AdversarialItemProposalApiTests(KamraApiFactory factory)
{
    // A non-positive amount from the model is dropped (the manual form's rule), not proposed or saved.
    [Theory]
    [InlineData(-999)]
    [InlineData(0)]
    public async Task Prompt_injection_non_positive_amount_is_nulled_not_saved(decimal badAmount)
    {
        var parser = new ScriptedParser();
        parser.On("tej", new ParsedItem("tej", "tej", badAmount, "liter", null));
        using var app = WithLlm(parser);
        var client = await ApiClient.SignedInAsync(app);

        var response = await client.PostAsync("/api/v1/item-proposals", new { text = "tej" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var item = (await Json(response)).GetProperty("items")[0];
        item.GetProperty("amount").ValueKind.Should().Be(JsonValueKind.Null, $"amount {badAmount} is invalid and must be treated as missing");
        (await client.GetJsonAsync("/api/v1/pantry-items")).GetArrayLength().Should().Be(0);
    }

    // A long, unrecognised expiry text from the model is cut to 100 characters, not stored whole and not a 500.
    [Fact]
    public async Task Prompt_injection_very_long_expiry_text_is_truncated_not_crashed()
    {
        var longExpiry = "péntekig " + new string('x', 500);
        var parser = new ScriptedParser();
        parser.On("tej", new ParsedItem("tej", "tej", 1m, "liter", longExpiry));
        using var app = WithLlm(parser);
        var client = await ApiClient.SignedInAsync(app);

        var response = await client.PostAsync("/api/v1/item-proposals", new { text = "tej" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var item = (await Json(response)).GetProperty("items")[0];
        item.GetProperty("expiryText").GetString().Should().Be(longExpiry[..100]);
        item.GetProperty("expiryEstimated").GetBoolean().Should().BeTrue();
    }

    // An LLM outage (503) or an invalid answer (502) leaks no stack trace, path, class, key or the user's sentence.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Llm_error_body_contains_no_stack_trace_path_key_or_user_sentence(bool unavailable)
    {
        const string userSentence = "2 liter tej és egy üveg pesztó";
        var parser = new ScriptedParser();
        parser.Fail(userSentence, unavailable ? ItemProposalErrors.LlmUnavailable() : ItemProposalErrors.LlmInvalidResponse());
        using var app = WithLlm(parser);
        var client = await ApiClient.SignedInAsync(app);

        var response = await client.PostAsync("/api/v1/item-proposals", new { text = userSentence });

        response.StatusCode.Should().Be(unavailable ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.BadGateway);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().NotContainAny(
            "Exception", "StackTrace", "at KamraApp", "C:\\", "/app/", "ApiKey", "api-key", "test-key-not-used",
            userSentence, "GeminiIngredientParser", "Infrastructure");
        (await ApiClient.ProblemAsync(response)).GetProperty("code").GetString().Should()
            .Be(unavailable ? "LLM_UNAVAILABLE" : "LLM_INVALID_RESPONSE");
    }

    private WebApplicationFactory<Program> WithLlm(ScriptedParser parser) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Llm:Enabled", "true");
            builder.UseSetting("Llm:ApiKey", "test-key-not-used");
            builder.UseSetting("RateLimiting:ItemProposals:PermitLimit", "1000");
            builder.ConfigureTestServices(services => services.AddSingleton<IIngredientParser>(parser));
        });

    private static async Task<JsonElement> Json(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
}
