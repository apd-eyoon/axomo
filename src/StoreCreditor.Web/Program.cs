using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using StoreCreditor.Data;
using StoreCreditor.Data.Entities;
using StoreCreditor.Services;
using StoreCreditor.Services.Jobs;
using StoreCreditor.Services.Options;
using StoreCreditor.Web.Authorization;
using StoreCreditor.Web.Middleware;
using StoreCreditor.Web.Options;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddProblemDetails();
builder.Services.Configure<StoreCreditorAuthenticationOptions>(builder.Configuration.GetSection("Authentication"));

builder.Services.AddDbContext<StoreCreditorDbContext>(options => options.UseSqlServer(connectionString));
builder.Services
    .AddDataProtection()
    .PersistKeysToDbContext<StoreCreditorDbContext>()
    .SetApplicationName("StoreCreditor");
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedEmail = false;
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 12;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
    })
    .AddEntityFrameworkStores<StoreCreditorDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.Cookie.Name = "StoreCreditor.Auth";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

if (IsMicrosoftEntraConfigured(builder.Configuration))
{
    builder.Services
        .AddAuthentication()
        .AddOpenIdConnect("MicrosoftEntraId", "Microsoft Entra ID", options =>
        {
            var entra = builder.Configuration.GetSection("MicrosoftEntraId");
            var tenantId = entra["TenantId"]!;

            options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
            options.ClientId = entra["ClientId"]!;
            options.ClientSecret = entra["ClientSecret"];
            options.CallbackPath = entra["CallbackPath"] ?? "/signin-oidc";
            options.SignedOutCallbackPath = entra["SignedOutCallbackPath"] ?? "/signout-callback-oidc";
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.SaveTokens = true;
            options.SignInScheme = IdentityConstants.ExternalScheme;
            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");
            options.TokenValidationParameters = new TokenValidationParameters
            {
                NameClaimType = "name"
            };
            options.Events = new OpenIdConnectEvents
            {
                OnRedirectToIdentityProvider = context =>
                {
                    context.ProtocolMessage.RedirectUri = entra["RedirectUri"]
                        ?? BuildRedirectUri(context.Request, options.CallbackPath);
                    return Task.CompletedTask;
                },
                OnRedirectToIdentityProviderForSignOut = context =>
                {
                    context.ProtocolMessage.PostLogoutRedirectUri = entra["PostLogoutRedirectUri"]
                        ?? BuildRedirectUri(context.Request, "/Identity/Account/Login");
                    return Task.CompletedTask;
                },
                OnRemoteFailure = context =>
                {
                    context.HandleResponse();
                    var message = Uri.EscapeDataString(context.Failure?.Message ?? "Microsoft Entra ID sign-in failed.");
                    context.Response.Redirect($"/Identity/Account/Login?externalError={message}");
                    return Task.CompletedTask;
                }
            };
        });
}

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));

builder.Services.AddStoreCreditorData();
builder.Services.AddStoreCreditorServices(builder.Configuration);

builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.FromSeconds(15),
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));
builder.Services.AddHangfireServer();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseMiddleware<ProblemDetailsExceptionMiddleware>();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapBlazorHub();
app.MapRazorPages();
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireAdminAuthorizationFilter()]
});

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await SeedIdentityAsync(services);
    var jobOptions = services.GetRequiredService<IOptionsMonitor<HangfireJobOptions>>();
    var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("HangfireRecurringJobs");
    RegisterRecurringJobs(jobOptions.CurrentValue, logger);
    var jobOptionsChangeSubscription = jobOptions.OnChange((options, _) => RegisterRecurringJobs(options, logger));
    if (jobOptionsChangeSubscription is not null)
    {
        app.Lifetime.ApplicationStopping.Register(jobOptionsChangeSubscription.Dispose);
    }
}

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

await app.RunAsync();

static async Task SeedIdentityAsync(IServiceProvider services)
{
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "Admin", "Operator" })
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

static void RegisterRecurringJob<TJob>(
    string recurringJobId,
    System.Linq.Expressions.Expression<Func<TJob, Task>> methodCall,
    string? configuredCron,
    string fallbackCron,
    ILogger logger)
{
    var cron = NormalizeCron(configuredCron, fallbackCron);

    try
    {
        RecurringJob.AddOrUpdate(recurringJobId, methodCall, cron);
    }
    catch (ArgumentException exception)
    {
        logger.LogWarning(
            exception,
            "Invalid cron expression '{Cron}' for recurring job '{RecurringJobId}'. Falling back to '{FallbackCron}'.",
            cron,
            recurringJobId,
            fallbackCron);
        RecurringJob.AddOrUpdate(recurringJobId, methodCall, fallbackCron);
    }
}

static void RegisterRecurringJobs(HangfireJobOptions jobOptions, ILogger logger)
{
    RegisterRecurringJob<EmployeeImportJob>(
        "employee-import",
        job => job.RunAsync(CancellationToken.None),
        jobOptions.EmployeeImportCron,
        Cron.Daily(2),
        logger);
    RegisterRecurringJob<StoreCreditJob>(
        "store-credit",
        job => job.RunAsync(CancellationToken.None),
        jobOptions.StoreCreditCron,
        Cron.MinuteInterval(15),
        logger);
}

static string NormalizeCron(string? configuredCron, string fallbackCron)
{
    if (string.IsNullOrWhiteSpace(configuredCron))
    {
        return fallbackCron;
    }

    var cron = configuredCron.Trim();
    var parts = cron.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    return parts.Length == 4 ? $"{cron} *" : cron;
}

static bool IsMicrosoftEntraConfigured(IConfiguration configuration)
{
    var entra = configuration.GetSection("MicrosoftEntraId");
    return !string.IsNullOrWhiteSpace(entra["TenantId"])
        && !string.IsNullOrWhiteSpace(entra["ClientId"])
        && !string.IsNullOrWhiteSpace(entra["ClientSecret"]);
}

static string BuildRedirectUri(HttpRequest request, PathString path) =>
    $"{request.Scheme}://{request.Host}{request.PathBase}{path}";
