using KamraApp.Application.Auth;
using KamraApp.Application.Chat;
using KamraApp.Application.Common;
using KamraApp.Application.Cookings;
using KamraApp.Application.Ingredients;
using KamraApp.Application.ItemProposals;
using KamraApp.Application.Pantry;
using KamraApp.Application.Recipes;
using KamraApp.Application.Recommendations;
using KamraApp.Application.ShoppingList;
using KamraApp.Infrastructure.Identity;
using KamraApp.Infrastructure.Llm;
using KamraApp.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace KamraApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // QA-7: a missing connection string stops the app at startup instead of failing on the first request.
        services.AddOptions<DatabaseOptions>()
            .Configure(options => options.ConnectionString = configuration.GetConnectionString("Default") ?? "")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // ADR-0011: Npgsql "Include Error Detail" and EnableSensitiveDataLogging stay off, so data
        // values never reach exception messages or logs.
        services.AddDbContext<KamraDbContext>((provider, options) =>
            options.UseNpgsql(provider.GetRequiredService<IOptions<DatabaseOptions>>().Value.ConnectionString));

        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");
        services.AddScoped<IngredientRepository>();
        services.AddScoped<IIngredientRepository>(provider => provider.GetRequiredService<IngredientRepository>());
        services.AddScoped<IOwnIngredientRepository>(provider => provider.GetRequiredService<IngredientRepository>());
        services.AddScoped<IPantryRepository, PantryRepository>();
        services.AddScoped<RecipeRepository>();
        services.AddScoped<IRecipeRepository>(provider => provider.GetRequiredService<RecipeRepository>());
        services.AddScoped<IRecipeCatalog>(provider => provider.GetRequiredService<RecipeRepository>());
        services.AddScoped<ICookingRepository, CookingRepository>();
        services.AddScoped<IShoppingListRepository, ShoppingListRepository>();
        services.AddScoped<IIngredientLocks, IngredientLocks>();
        services.AddScoped<IItemProposalRepository, ItemProposalRepository>();

        // us2_minispec K10, ADR-0009: off by default; when on, a missing key stops the app at startup.
        services.AddOptions<LlmOptions>()
            .Bind(configuration.GetSection(LlmOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        if (configuration.GetValue<bool>($"{LlmOptions.Section}:{nameof(LlmOptions.Enabled)}"))
        {
            // The adapter has its own per-attempt timeout (QA-3); the client's own one is only a backstop.
            services.AddHttpClient<IIngredientParser, GeminiIngredientParser>(client => client.Timeout = TimeSpan.FromMinutes(1));
        }
        else
        {
            services.AddSingleton<IIngredientParser, DisabledIngredientParser>();
        }

        // US-6: the Gemini chat adapter comes in T2 (us6_minispec K6); until then the chat answers LLM_DISABLED.
        services.AddSingleton<IChatModel, DisabledChatModel>();

        // ADR-0006: NIST SP 800-63B-4 password length without composition rules; lockout after 5
        // failures for 5 minutes; one account per e-mail (the e-mail is also the user name).
        services.AddIdentityCore<AppUser>(options =>
            {
                options.Password.RequiredLength = 15;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredUniqueChars = 1;
                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters = "";
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<KamraDbContext>()
            .AddSignInManager();
        services.AddScoped<IIdentityService, IdentityService>();
        // Validate the cookie's security stamp on every request, so a logout (new stamp) revokes the
        // session immediately instead of after the default 30 minutes. Cost: one user lookup per request.
        services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero);

        // ADR-0006: cookie and antiforgery keys must survive restarts and deploys (Docker volume).
        var dataProtection = services.AddDataProtection().SetApplicationName("Kamra");
        var keysPath = configuration["DataProtection:KeysPath"];
        if (!string.IsNullOrWhiteSpace(keysPath))
        {
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        }

        return services;
    }
}
