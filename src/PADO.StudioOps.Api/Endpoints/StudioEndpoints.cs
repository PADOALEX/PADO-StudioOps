using Microsoft.EntityFrameworkCore;
using PADO.StudioOps.Api.Data;
using PADO.StudioOps.Api.Domain;
using PADO.StudioOps.Api.Security;

namespace PADO.StudioOps.Api.Endpoints;

public static class StudioEndpoints
{
    public sealed record StudioInput(string Name, string Country, short FoundedYear, string? Website);
    public sealed record EmployeeInput(int? StudioId, string FirstName, string LastName, string Email, string Position, DateOnly HireDate, decimal Salary);
    private static bool Valid(StudioInput x) => ApiRules.Text(x.Name, 100) && ApiRules.Text(x.Country, 60) && x.FoundedYear is >= 1970 and <= 2100 && (x.Website is null || x.Website.Length <= 200);
    private static bool Valid(EmployeeInput x) => ApiRules.Text(x.FirstName, 50) && ApiRules.Text(x.LastName, 50) && ApiRules.Email(x.Email) && ApiRules.Text(x.Position, 80) && x.Salary >= 0 && x.HireDate != default;

    public static void MapStudiosAndEmployees(this WebApplication app)
    {
        var studios = app.MapGroup("/api/studios").RequireAuthorization("AdminOnly").WithTags("Studios");
        studios.MapGet("/", async (StudioOpsDbContext db, CancellationToken ct) => Results.Ok(await db.Studios.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct)));
        studios.MapGet("/{id:int}", async (int id, StudioOpsDbContext db, CancellationToken ct) =>
            await db.Studios.AsNoTracking().FirstOrDefaultAsync(x => x.StudioId == id, ct) is { } x ? Results.Ok(x) : Results.NotFound());
        studios.MapPost("/", async (StudioInput x, StudioOpsDbContext db, CancellationToken ct) =>
        {
            if (!Valid(x)) return Results.BadRequest();
            var entity = new Studio { Name = x.Name.Trim(), Country = x.Country.Trim(), FoundedYear = x.FoundedYear, Website = x.Website?.Trim() };
            db.Studios.Add(entity); await db.SaveChangesAsync(ct);
            return Results.Created($"/api/studios/{entity.StudioId}", entity);
        });
        studios.MapPut("/{id:int}", async (int id, StudioInput x, StudioOpsDbContext db, CancellationToken ct) =>
        {
            if (!Valid(x)) return Results.BadRequest();
            var entity = await db.Studios.FindAsync([id], ct);
            if (entity is null) return Results.NotFound();
            entity.Name = x.Name.Trim(); entity.Country = x.Country.Trim(); entity.FoundedYear = x.FoundedYear; entity.Website = x.Website?.Trim();
            await db.SaveChangesAsync(ct); return Results.Ok(entity);
        });
        studios.MapDelete("/{id:int}", async (int id, StudioOpsDbContext db, CancellationToken ct) =>
        {
            var entity = await db.Studios.FindAsync([id], ct);
            if (entity is null) return Results.NotFound();
            if (await db.GameProjects.AnyAsync(p => p.StudioId == id, ct)) return Results.Conflict(new { error = "Remove studio projects first." });
            db.Studios.Remove(entity); await db.SaveChangesAsync(ct); return Results.NoContent();
        });

        var employees = app.MapGroup("/api/employees").RequireAuthorization("AdminOnly").WithTags("Employees");
        employees.MapGet("/", async (StudioOpsDbContext db, CancellationToken ct) => Results.Ok(await db.Employees.AsNoTracking().OrderBy(x => x.LastName).Take(500).Select(x => new { x.EmployeeId, x.StudioId, x.FirstName, x.LastName, x.Email, x.Position, x.HireDate, x.Salary }).ToListAsync(ct)));
        employees.MapGet("/{id:int}", async (int id, StudioOpsDbContext db, CancellationToken ct) =>
            await db.Employees.AsNoTracking().Where(x => x.EmployeeId == id).Select(x => new { x.EmployeeId, x.StudioId, x.FirstName, x.LastName, x.Email, x.Position, x.HireDate, x.Salary }).FirstOrDefaultAsync(ct) is { } x ? Results.Ok(x) : Results.NotFound());
        employees.MapPost("/", async (EmployeeInput x, StudioOpsDbContext db, CancellationToken ct) =>
        {
            if (!Valid(x) || (x.StudioId is not null && !await db.Studios.AnyAsync(s => s.StudioId == x.StudioId, ct))) return Results.BadRequest();
            var entity = new Employee { StudioId = x.StudioId, FirstName = x.FirstName.Trim(), LastName = x.LastName.Trim(), Email = x.Email.Trim().ToLowerInvariant(), Position = x.Position.Trim(), HireDate = x.HireDate, Salary = x.Salary };
            db.Employees.Add(entity); await db.SaveChangesAsync(ct);
            return Results.Created($"/api/employees/{entity.EmployeeId}", new { entity.EmployeeId });
        });
        employees.MapPut("/{id:int}", async (int id, EmployeeInput x, StudioOpsDbContext db, CancellationToken ct) =>
        {
            if (!Valid(x) || (x.StudioId is not null && !await db.Studios.AnyAsync(s => s.StudioId == x.StudioId, ct))) return Results.BadRequest();
            var entity = await db.Employees.FindAsync([id], ct);
            if (entity is null) return Results.NotFound();
            // Moving an employee with active memberships can break project scoping.
            if (entity.StudioId != x.StudioId && await db.ProjectMembers.AnyAsync(m => m.EmployeeId == id, ct)) return Results.Conflict(new { error = "Remove employee from projects before changing studio." });
            entity.StudioId = x.StudioId; entity.FirstName = x.FirstName.Trim(); entity.LastName = x.LastName.Trim(); entity.Email = x.Email.Trim().ToLowerInvariant(); entity.Position = x.Position.Trim(); entity.HireDate = x.HireDate; entity.Salary = x.Salary;
            await db.SaveChangesAsync(ct); return Results.NoContent();
        });
        employees.MapDelete("/{id:int}", async (int id, StudioOpsDbContext db, CancellationToken ct) =>
        {
            var entity = await db.Employees.FindAsync([id], ct);
            if (entity is null) return Results.NotFound();
            if (await db.ProjectMembers.AnyAsync(m => m.EmployeeId == id, ct) || await db.AppUsers.AnyAsync(u => u.EmployeeId == id, ct))
                return Results.Conflict(new { error = "Unassign project memberships and deactivate/remove the login first." });
            db.Employees.Remove(entity); await db.SaveChangesAsync(ct); return Results.NoContent();
        });
    }
}
