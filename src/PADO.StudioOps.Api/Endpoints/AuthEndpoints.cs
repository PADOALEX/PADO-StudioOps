using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PADO.StudioOps.Api.Data;
using PADO.StudioOps.Api.Domain;
using PADO.StudioOps.Api.Security;

namespace PADO.StudioOps.Api.Endpoints;

public static class AuthEndpoints
{
    public sealed record LoginRequest(string Email, string Password);
    public sealed record CreateUserRequest(string Email, string Password, string Role, int? EmployeeId);

    public static void MapAuth(this WebApplication app)
    {
        var auth = app.MapGroup("/api/auth").WithTags("Authentication");
        auth.MapGet("/csrf", (IAntiforgery csrf, HttpContext http) =>
        {
            var tokens = csrf.GetAndStoreTokens(http);
            http.Response.Headers.CacheControl = "no-store";
            return Results.Ok(new { token = tokens.RequestToken });
        }).AllowAnonymous();

        auth.MapPost("/login", async (LoginRequest request, StudioOpsDbContext db, IPasswordHasher<AppUser> hasher, HttpContext http, CancellationToken ct) =>
        {
            var email = request.Email?.Trim().ToLowerInvariant();
            var user = await db.AppUsers.AsNoTracking().SingleOrDefaultAsync(x => x.Email == email && x.IsActive, ct);
            if (user is null || hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password ?? "") == PasswordVerificationResult.Failed)
                return Results.Unauthorized();
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, user.AppUserId.ToString()), new(ClaimTypes.Name, user.Email), new(ClaimTypes.Role, user.Role) };
            if (user.EmployeeId is not null) claims.Add(new("employee_id", user.EmployeeId.Value.ToString()));
            await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
                new AuthenticationProperties { IsPersistent = false, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) });
            return Results.Ok(new { user.Email, user.Role, user.EmployeeId });
        }).RequireRateLimiting("login").AllowAnonymous();

        auth.MapPost("/logout", async (HttpContext http) =>
        {
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.NoContent();
        }).RequireAuthorization();

        auth.MapGet("/me", (ClaimsPrincipal user) => Results.Ok(new
        {
            email = user.FindFirstValue(ClaimTypes.Name),
            role = user.FindFirstValue(ClaimTypes.Role),
            employeeId = ProjectAccess.EmployeeId(user)
        })).RequireAuthorization();

        app.MapPost("/api/admin/users", async (CreateUserRequest request, StudioOpsDbContext db, IPasswordHasher<AppUser> hasher, CancellationToken ct) =>
        {
            if (!ApiRules.Email(request.Email) || !StudioOpsValues.Valid(request.Role, StudioOpsValues.Roles) || request.Password is null || request.Password.Length < 12 || request.Password.Length > 128)
                return Results.BadRequest(new { error = "Valid email, role and 12–128 character password are required." });
            if (request.Role != "Admin" && request.EmployeeId is null)
                return Results.BadRequest(new { error = "Non-admin accounts require an employee." });
            if (request.EmployeeId is not null && !await db.Employees.AnyAsync(x => x.EmployeeId == request.EmployeeId, ct)) return Results.BadRequest(new { error = "Employee does not exist." });
            var user = new AppUser { Email = request.Email.Trim().ToLowerInvariant(), Role = request.Role, EmployeeId = request.EmployeeId };
            user.PasswordHash = hasher.HashPassword(user, request.Password);
            db.AppUsers.Add(user);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/admin/users/{user.AppUserId}", new { user.AppUserId, user.Email, user.Role, user.EmployeeId });
        }).RequireAuthorization("AdminOnly").WithTags("Administration");
    }
}
