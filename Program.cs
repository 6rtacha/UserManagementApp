using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using UserManagementApp.Data;
using UserManagementApp.Middleware;
using UserManagementApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add MVC Controllers and Razor Views
builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient<IEmailService, BrevoEmailService>();  

// Configure Antiforgery (CSRF protection for AJAX header)
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});  

// Configure PostgreSQL connection (Supports standard and URI formats)
var rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var connectionString = rawConnectionString != null 
    ? string.Join("", rawConnectionString.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)).Trim()
    : null;

if (!string.IsNullOrWhiteSpace(connectionString) && 
    (connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) || 
     connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)))
{
    try
    {
        var cleanUrl = connectionString.Split('?')[0].Trim();
        var schemeEnd = cleanUrl.IndexOf("://", StringComparison.Ordinal);

        if (schemeEnd >= 0)
        {
            var withoutScheme = cleanUrl.Substring(schemeEnd + 3);
            var atIndex = withoutScheme.LastIndexOf('@');

            if (atIndex >= 0)
            {
                var userInfo = withoutScheme.Substring(0, atIndex);
                var hostAndPath = withoutScheme.Substring(atIndex + 1);

                var userPass = userInfo.Split(':', 2);
                var username = userPass.Length > 0 ? Uri.UnescapeDataString(userPass[0]) : "";
                var password = userPass.Length > 1 ? Uri.UnescapeDataString(userPass[1]) : "";

                var firstSlash = hostAndPath.IndexOf('/');
                var hostPort = firstSlash >= 0 ? hostAndPath.Substring(0, firstSlash) : hostAndPath;
                var database = firstSlash >= 0 ? hostAndPath.Substring(firstSlash + 1) : "";

                var hostPortParts = hostPort.Split(':', 2);
                var host = hostPortParts[0].Trim();
                var port = hostPortParts.Length > 1 && int.TryParse(hostPortParts[1], out _) ? hostPortParts[1].Trim() : "5432";

                connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;";
            }
        }
    }
    catch
    {
        // Fallback to original connection string if parsing fails
    }
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configure Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });

var app = builder.Build();

// Configure HTTP request pipeline
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Custom middleware to terminate session for blocked/deleted users
app.UseMiddleware<UserStatusMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();