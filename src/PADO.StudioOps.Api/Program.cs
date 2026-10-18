using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PADO.StudioOps.Api.Data;
using PADO.StudioOps.Api.Domain;
using PADO.StudioOps.Api.Endpoints;
using PADO.StudioOps.Api.Security;
using PADO.StudioOps.Api.Services;

var builder = WebApplication.CreateBuilder(args);
// Resolve the final host configuration from DI. WebApplicationFactory supplies its
// isolated test connection string after the entry point starts building the host.
builder.Services.AddDbContext<StudioOpsDbContext>((services, options) =>
{
    var connection = services.GetRequiredService<IConfiguration>().GetConnectionString("StudioOps");
    if (string.IsNullOrWhiteSpace(connection))
        throw new InvalidOperationException("ConnectionStrings:StudioOps is required. Use environment variables or user-secrets.");
    options.UseNpgsql(connection);
});
builder.Services.AddScoped<ProjectAccess>();
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddAntiforgery(options => { options.HeaderName = "X-CSRF-TOKEN"; options.Cookie.Name = "StudioOps.Csrf"; options.Cookie.HttpOnly = true; options.Cookie.SameSite = SameSiteMode.Strict; });
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o =>
{
    o.Cookie.Name = "StudioOps.Auth";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Strict;
    o.Cookie.SecurePolicy = builder.Configuration.GetValue<bool>("STUDIOOPS_COOKIE_SECURE") ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
    o.ExpireTimeSpan = TimeSpan.FromHours(8);
    o.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; };
    o.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; };
    // Revalidate each cookie against the active DB account to enforce deactivation,
    // role changes and employee reassignment without waiting eight hours.
    o.Events.OnValidatePrincipal = async ctx =>
    {
        var userId = ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = ctx.Principal?.FindFirstValue(ClaimTypes.Role);
        var employeeId = ctx.Principal?.FindFirstValue("employee_id");
        var email = ctx.Principal?.FindFirstValue(ClaimTypes.Name);
        var validId = int.TryParse(userId, out var id);
        var current = validId
            ? await ctx.HttpContext.RequestServices.GetRequiredService<StudioOpsDbContext>()
                .AppUsers.AsNoTracking().Where(u => u.AppUserId == id)
                .Select(u => new { u.IsActive, u.Role, u.EmployeeId, u.Email })
                .FirstOrDefaultAsync(ctx.HttpContext.RequestAborted)
            : null;
        if (current is null || !current.IsActive || current.Role != role ||
            current.EmployeeId?.ToString() != employeeId || current.Email != email)
        {
            ctx.RejectPrincipal();
            await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    };
});
builder.Services.AddAuthorization(o => o.AddPolicy("AdminOnly", p => p.RequireRole("Admin")));
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("login", ctx => RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ =>
        new FixedWindowRateLimiterOptions { PermitLimit = 8, Window = TimeSpan.FromMinutes(2), QueueLimit = 0 }));
});

var app = builder.Build();
app.UseExceptionHandler();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Validate antiforgery tokens on every state-changing API request, including login.
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/api") &&
        !HttpMethods.IsGet(ctx.Request.Method) && !HttpMethods.IsHead(ctx.Request.Method) && !HttpMethods.IsOptions(ctx.Request.Method))
    {
        var antiforgery = ctx.RequestServices.GetRequiredService<IAntiforgery>();
        try { await antiforgery.ValidateRequestAsync(ctx); }
        catch (AntiforgeryValidationException)
        {
            ctx.Response.StatusCode = 400;
            await ctx.Response.WriteAsJsonAsync(new { error = "Invalid CSRF token. Reload the page." });
            return;
        }
    }
    await next(ctx);
});
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapGet("/health/ready", async (StudioOpsDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct) ? Results.Ok(new { status = "ready" }) : Results.StatusCode(503)).AllowAnonymous();
app.MapAuth();
app.MapStudiosAndEmployees();
app.MapProjects();
app.MapWork();

if (builder.Configuration.GetValue<bool>("STUDIOOPS_AUTO_MIGRATE"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<StudioOpsDbContext>().Database.MigrateAsync();
}
if (builder.Configuration.GetValue<bool>("STUDIOOPS_BOOTSTRAP_ADMIN"))
    await DemoSeed.SeedAsync(app.Services, builder.Configuration);

app.Run();

public partial class Program { }
