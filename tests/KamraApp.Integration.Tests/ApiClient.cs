using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace KamraApp.Integration.Tests;

// A browser-like client: https (the auth cookie is Secure), cookie jar, and the antiforgery token
// header that the SPA sends. The token is bound to the signed-in user, so it is refreshed after
// login, registration and logout - the same thing the SPA has to do.
public sealed class ApiClient
{
    public const string ValidPassword = "correct horse battery staple";

    private ApiClient(HttpClient http) => Http = http;

    public HttpClient Http { get; }

    public static async Task<ApiClient> CreateAsync(WebApplicationFactory<Program> factory)
    {
        var client = new ApiClient(factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
        }));
        await client.RefreshAntiforgeryTokenAsync();
        return client;
    }

    public static string NewEmail() => $"user-{Guid.NewGuid():N}@example.com";

    public async Task RefreshAntiforgeryTokenAsync()
    {
        var response = await Http.GetFromJsonAsync<JsonElement>("/api/v1/auth/antiforgery", TestContext.Current.CancellationToken);
        Http.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        Http.DefaultRequestHeaders.Add("X-XSRF-TOKEN", response.GetProperty("requestToken").GetString());
    }

    public async Task<HttpResponseMessage> PostAsync(string path, object? body = null)
    {
        var response = await Http.PostAsJsonAsync(path, body, TestContext.Current.CancellationToken);
        if (response.IsSuccessStatusCode && path.StartsWith("/api/v1/auth/", StringComparison.Ordinal))
        {
            await RefreshAntiforgeryTokenAsync();
        }

        return response;
    }

    public Task<HttpResponseMessage> RegisterAsync(string email, string password = ValidPassword) =>
        PostAsync("/api/v1/auth/register", new { email, password });

    public Task<HttpResponseMessage> LoginAsync(string email, string password = ValidPassword) =>
        PostAsync("/api/v1/auth/login", new { email, password });

    public Task<HttpResponseMessage> MeAsync() =>
        Http.GetAsync("/api/v1/auth/me", TestContext.Current.CancellationToken);

    public static async Task<JsonElement> ProblemAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
}
