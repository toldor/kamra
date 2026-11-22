using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace KamraApp.Integration.Tests;

// Adversarial tests written by Antigravity (Claude Sonnet 4.6) against the T1 forwarded headers and invite code
// (docs/07_ai/review_prompts.md, template 2). Integrated verbatim apart from compile fixes and the approved changes:
// (review numbering) tests 1 and 2 now check the rate-limit partition, not only "no 500"; test 3 (a duplicate of
// ForwardedHeadersTests) and test 7 (a duplicate of InviteCodeTests and AdversarialAuthTests) were left out.
[Collection("api")]
public class AdversarialProxyAndInviteTests(KamraApiFactory factory)
{
    // ── Állandók ──────────────────────────────────────────────────────────────────
    private const string ProxyNetwork = "10.0.0.0/8";
    private const string Proxy = "10.0.0.5";
    private const string WrongPassword = "a wrong but long password";
    private const string Code = "demo-invite-code-adversarial-1";   // ≥16 karakter
    private const string InviteCodeMsg = "Hibás vagy hiányzó meghívókód. Kérd el attól, aki a címet adta.";

    // 1. – X-Forwarded-For: több cím vesszővel (láncolt proxyk)
    /// <summary>
    /// Támadás: a kliens egy vesszővel elválasztott X-Forwarded-For láncot küld
    /// (pl. "1.2.3.4, 5.6.7.8"), hogy egy megbízható proxy-cím neve mögé bújjon.
    /// Az alkalmazás ForwardLimit = 1, ezért csak a lánc JOBB OLDALI (utolsó) elemét
    /// veszi át; az ügyfél által beillesztett bal oldali cím nem számít ügyfél-IP-nek.
    ///
    /// Elvárt viselkedés: a rate-limiter az egyes ügyfél-IP-ket (a lánc utolsó tagja)
    /// külön kezeli, nem a hamisítottat. Az API 401-et ad, nem 500-at.
    /// </summary>
    [Fact]
    public async Task Chained_XFF_with_multiple_addresses_does_not_let_client_spoof_trusted_proxy()
    {
        var client = await ClientAsync(ProxyNetwork);

        // The left part is client-controlled; with ForwardLimit = 1 only the rightmost entry is the client.
        for (var i = 0; i < 3; i++)
        {
            (await LoginWithXffAsync(client, Proxy, "1.2.3.4, 203.0.113.1")).Should().Be(HttpStatusCode.Unauthorized);
        }

        (await LoginWithXffAsync(client, Proxy, "203.0.113.1")).Should().Be(HttpStatusCode.TooManyRequests,
            because: "a lánc utolsó tagja a kliens, a bal oldali hamis cím nem ad új keretet");
        (await LoginWithXffAsync(client, Proxy, "203.0.113.9")).Should().Be(HttpStatusCode.Unauthorized,
            because: "más kliens saját keretet kap");
    }

    // 2. – X-Forwarded-For: IPv6 ügyfélcím a megbízható proxyból
    /// <summary>
    /// Támadás: az ügyfél IPv6-os X-Forwarded-For fejlécet küld (pl. "2001:db8::1").
    /// Elvárt viselkedés: a middleware elfogadja az IPv6-os ügyfélcímet, a rate-limiter
    /// az IPv6-os stringet használja partíciókulcsként; a válasz 401 (nem 500).
    /// </summary>
    [Fact]
    public async Task IPv6_client_address_in_XFF_is_accepted_and_rate_limited_independently()
    {
        var client = await ClientAsync(ProxyNetwork);

        for (var i = 0; i < 3; i++)
        {
            (await LoginWithXffAsync(client, Proxy, "2001:db8::1")).Should().Be(HttpStatusCode.Unauthorized,
                because: "IPv6-os cím esetén sem lehet 500-as hiba");
        }

        (await LoginWithXffAsync(client, Proxy, "2001:db8::1")).Should().Be(HttpStatusCode.TooManyRequests);
        (await LoginWithXffAsync(client, Proxy, "2001:db8::2")).Should().Be(HttpStatusCode.Unauthorized,
            because: "két különböző IPv6-os cím egymástól független limittel bír");
    }

    // 3. – X-Forwarded-Proto furcsa értékekkel (ambiguous / többszörös)
    /// <summary>
    /// Támadás: az X-Forwarded-Proto fejléc értéke "https, http", "HTTPS" vagy "ftp".
    /// A teszt elsősorban azt ellenőrzi, hogy a szerver nem ad 500-at.
    /// </summary>
    [Theory]
    [InlineData("https, http")]
    [InlineData("HTTPS")]
    [InlineData("ftp")]
    public async Task Unusual_XForwardedProto_values_from_trusted_proxy_do_not_crash_the_server(string protoValue)
    {
        var app = App(ProxyNetwork);
        var http = app.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost"),
        });
        http.DefaultRequestHeaders.Add(TestRemoteAddress.Header, Proxy);
        http.DefaultRequestHeaders.Add("X-Forwarded-Proto", protoValue);

        using var response = await http.GetAsync("/api/v1/auth/antiforgery", TestContext.Current.CancellationToken);

        ((int)response.StatusCode).Should().BeLessThan(500,
            because: $"'{protoValue}' X-Forwarded-Proto értékre a szerver nem omolhat össze");
    }

    // 4. – Meghívókód: kis- és nagybetű, whitespace, Unicode
    /// <summary>
    /// Támadás: a kódot kis- vagy nagybetűsen, whitespace-szel körülvéve, illetve Unicode karakterrel
    /// megváltoztatva próbálja beküldeni. Elvárt: eltérő kód → 400 VALIDATION_FAILED; pontos kód → 204.
    /// </summary>
    [Theory]
    [InlineData("DEMO-INVITE-CODE-ADVERSARIAL-1", false)]
    [InlineData(" demo-invite-code-adversarial-1", false)]
    [InlineData("demo-invite-code-adversarial-1 ", false)]
    [InlineData("demo-invite-code-adversarial-¹", false)]
    [InlineData("demo-invite-code-adversarial-1", true)]
    public async Task Invite_code_comparison_is_case_and_whitespace_sensitive(string sentCode, bool shouldSucceed)
    {
        var client = await ApiClient.CreateAsync(WithCode());

        var response = await client.PostAsync("/api/v1/auth/register",
            new { email = ApiClient.NewEmail(), password = ApiClient.ValidPassword, inviteCode = sentCode });

        if (shouldSucceed)
        {
            response.StatusCode.Should().Be(HttpStatusCode.NoContent,
                because: "a pontos kód helyes regisztrációt eredményez");
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
                because: "az eltérő (eset/whitespace/unicode) kód elutasítandó");
            var problem = await ApiClient.ProblemAsync(response);
            problem.GetProperty("code").GetString().Should().Be("VALIDATION_FAILED");
            problem.GetProperty("errors").GetProperty("inviteCode")[0].GetString().Should().Be(InviteCodeMsg);
        }
    }

    // 5. – Meghívókód: 64 és 65 karakter hosszú kód (határ)
    /// <summary>
    /// Elvárt viselkedés: a szerver nem omlik össze; helyes kód → 204, helytelen → 400.
    /// </summary>
    [Fact]
    public async Task Boundary_length_invite_codes_are_handled_without_crash()
    {
        var longCode = new string('x', 64);
        var app = factory.WithWebHostBuilder(b => b.UseSetting("Auth:InviteCode", longCode));
        var client = await ApiClient.CreateAsync(app);

        var okResponse = await client.PostAsync("/api/v1/auth/register",
            new { email = ApiClient.NewEmail(), password = ApiClient.ValidPassword, inviteCode = longCode });
        okResponse.StatusCode.Should().Be(HttpStatusCode.NoContent,
            because: "64 karakteres pontos kód elfogadandó");

        var wrongCode = new string('x', 65);
        var badResponse = await client.PostAsync("/api/v1/auth/register",
            new { email = ApiClient.NewEmail(), password = ApiClient.ValidPassword, inviteCode = wrongCode });
        badResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            because: "65 karakteres kód (1-gyel hosszabb) nem egyezhet meg a 64 karakteressel");
    }

    // 6. – Szivárgás: a meghívókód nem jelenik meg semmilyen válaszban
    /// <summary>
    /// Elvárt viselkedés: sem a beküldött (rossz) kód, sem a konfigurált kód szövege
    /// nem jelenhet meg semmilyen válasz törzsében.
    /// </summary>
    [Fact]
    public async Task The_invite_code_is_never_reflected_in_any_response_body()
    {
        var app = WithCode();
        var sentWrongCode = "attacker-sent-wrong-code-1234";

        var registerClient = await ApiClient.CreateAsync(app);
        var registerResp = await registerClient.PostAsync("/api/v1/auth/register",
            new { email = ApiClient.NewEmail(), password = ApiClient.ValidPassword, inviteCode = sentWrongCode });
        var registerBody = await registerResp.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        registerBody.Should().NotContain(sentWrongCode, because: "a beküldött helytelen kód nem tükrözhető vissza");
        registerBody.Should().NotContain(Code, because: "a konfigurált kód nem szivároghat ki hibaüzenetben");

        var optionsClient = await ApiClient.CreateAsync(app);
        var optionsResp = await optionsClient.Http.GetAsync("/api/v1/auth/registration", TestContext.Current.CancellationToken);
        var optionsBody = await optionsResp.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        optionsBody.Should().NotContain(Code, because: "az options végpont csak a boolean inviteCodeRequired-t adja vissza");

        var successClient = await ApiClient.CreateAsync(app);
        var successResp = await successClient.PostAsync("/api/v1/auth/register",
            new { email = ApiClient.NewEmail(), password = ApiClient.ValidPassword, inviteCode = Code });
        var successBody = await successResp.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        successResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
        successBody.Should().NotContain(Code, because: "a sikeres regisztrációs válasz sem tartalmazza a kódot");
    }

    // ── Segédmetódusok ────────────────────────────────────────────────────────────

    private WebApplicationFactory<Program> App(string? knownNetworks) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RateLimiting:Auth:PermitLimit", "3");
            if (knownNetworks is not null)
            {
                builder.UseSetting("ForwardedHeaders:KnownNetworks", knownNetworks);
            }

            builder.ConfigureServices(services =>
                services.AddSingleton<IStartupFilter, TestRemoteAddress>());
        });

    private Task<ApiClient> ClientAsync(string? knownNetworks) => ApiClient.CreateAsync(App(knownNetworks));

    private WebApplicationFactory<Program> WithCode() =>
        factory.WithWebHostBuilder(b => b.UseSetting("Auth:InviteCode", Code));

    private static async Task<HttpStatusCode> LoginWithXffAsync(ApiClient client, string remote, string forwardedFor)
    {
        client.Http.DefaultRequestHeaders.Remove(TestRemoteAddress.Header);
        client.Http.DefaultRequestHeaders.Remove("X-Forwarded-For");
        client.Http.DefaultRequestHeaders.Add(TestRemoteAddress.Header, remote);
        client.Http.DefaultRequestHeaders.Add("X-Forwarded-For", forwardedFor);
        using var response = await client.LoginAsync(ApiClient.NewEmail(), WrongPassword);
        return response.StatusCode;
    }
}
