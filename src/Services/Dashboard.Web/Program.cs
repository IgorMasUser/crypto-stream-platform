using Elastic.Clients.Elasticsearch;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using TradingApp.Dashboard.Web.Identity;
using TradingApp.Dashboard.Web.Services;
using TradingApp.Elastic.Configuration;
using TradingApp.Elastic.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();

builder.Services.Configure<ElasticOptions>(builder.Configuration.GetSection("Elastic"));
builder.Services.AddElasticClient();
builder.Services.AddSingleton<AnalyticsService>();

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

builder.Services.AddDbContext<AppIdentityDbContext>(opt =>
    opt.UseNpgsql(connectionString)
       .UseSnakeCaseNamingConvention());

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit           = true;
        options.Password.RequiredLength         = 8;
        options.Password.RequireUppercase       = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.User.RequireUniqueEmail         = true;
    })
    .AddEntityFrameworkStores<AppIdentityDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath         = "/login";
    options.LogoutPath        = "/logout";
    options.AccessDeniedPath  = "/unauthorized";
    options.ExpireTimeSpan    = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly   = true;
    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
});

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
    await db.Database.MigrateAsync();

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var config      = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    await SeedData.SeedAsync(userManager, roleManager, config);
}

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Error");

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.MapGet("/api/analytics", async (ElasticsearchClient client, IConfiguration config, CancellationToken ct) =>
{
    var index = config.GetValue<string>("Elastic:IndexPrefix") ?? "analytics-index";
    var resp = await client.SearchAsync<TradingApp.Contracts.Events.AggregatedMarketAnalyticsEvent>(
        s => s.Index(index).Size(20), ct);
    return Results.Ok(resp.Documents);
}).RequireAuthorization();

await app.RunAsync();
