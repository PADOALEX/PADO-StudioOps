using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PADO.StudioOps.Api.Data;
using PADO.StudioOps.Api.Domain;
using PADO.StudioOps.Api.Security;

namespace PADO.StudioOps.Api.Endpoints;

public static class ProjectEndpoints
{
    public sealed record ProjectInput(int StudioId, int? ProjectLeadId, string Title, string Genre, string ProjectStatus, DateOnly StartDate, DateOnly? PlannedReleaseDate, decimal Budget);
    public sealed record MemberInput(int EmployeeId, string RoleInProject);
    public sealed record BuildInput(string Version, string Platform, int? CreatedByEmployeeId, bool IsStable);
    private static bool Valid(ProjectInput x) => ApiRules.Text(x.Title, 120) && ApiRules.Text(x.Genre, 60) && StudioOpsValues.Valid(x.ProjectStatus, StudioOpsValues.ProjectStatuses) && x.Budget >= 0 && x.StartDate != default && ApiRules.ReleaseAfterStart(x.PlannedReleaseDate, x.StartDate);

    public static void MapProjects(this WebApplication app)
    {
        var projects = app.MapGroup("/api/projects").RequireAuthorization().WithTags("Projects");
        projects.MapGet("/", async (StudioOpsDbContext db, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var employee = ProjectAccess.EmployeeId(user);
            var query = db.GameProjects.AsNoTracking().AsQueryable();
            if (!ProjectAccess.IsAdmin(user)) query = query.Where(p => db.ProjectMembers.Any(m => m.ProjectId == p.ProjectId && m.EmployeeId == employee));
            return Results.Ok(await query.OrderBy(p => p.Title).Select(p => new { p.ProjectId, p.StudioId, StudioName = p.Studio.Name, p.Title, p.Genre, p.ProjectStatus, p.StartDate, p.PlannedReleaseDate, p.Budget, p.ProjectLeadId }).ToListAsync(ct));
        });
        projects.MapGet("/{projectId:int}", async (int projectId, StudioOpsDbContext db, ClaimsPrincipal user, ProjectAccess access, CancellationToken ct) =>
        {
            if (!await access.CanRead(user, projectId, ct)) return Results.Forbid();
            var p = await db.GameProjects.AsNoTracking().Where(p => p.ProjectId == projectId).Select(p => new { p.ProjectId, p.StudioId, StudioName = p.Studio.Name, p.Title, p.Genre, p.ProjectStatus, p.StartDate, p.PlannedReleaseDate, p.Budget, p.ProjectLeadId }).FirstOrDefaultAsync(ct);
            return p is null ? Results.NotFound() : Results.Ok(p);
        });
        projects.MapPost("/", async (ProjectInput x, StudioOpsDbContext db, CancellationToken ct) =>
        {
            if (!Valid(x) || !await db.Studios.AnyAsync(s => s.StudioId == x.StudioId, ct)) return Results.BadRequest();
            if (x.ProjectLeadId is not null && !await db.Employees.AnyAsync(e => e.EmployeeId == x.ProjectLeadId && e.StudioId == x.StudioId, ct)) return Results.BadRequest(new { error = "Lead must work at this studio." });
            var p = new GameProject { StudioId = x.StudioId, ProjectLeadId = x.ProjectLeadId, Title = x.Title.Trim(), Genre = x.Genre.Trim(), ProjectStatus = x.ProjectStatus, StartDate = x.StartDate, PlannedReleaseDate = x.PlannedReleaseDate, Budget = x.Budget };
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            db.GameProjects.Add(p); await db.SaveChangesAsync(ct);
            if (x.ProjectLeadId is not null)
            {
                db.ProjectMembers.Add(new ProjectMember { ProjectId = p.ProjectId, EmployeeId = x.ProjectLeadId.Value, RoleInProject = "Project Lead" });
                await db.SaveChangesAsync(ct);
            }
            await transaction.CommitAsync(ct);
            return Results.Created($"/api/projects/{p.ProjectId}", new { p.ProjectId });
        }).RequireAuthorization("AdminOnly");
        projects.MapPut("/{projectId:int}", async (int projectId, ProjectInput x, StudioOpsDbContext db, ClaimsPrincipal user, ProjectAccess access, CancellationToken ct) =>
        {
            if (!await access.CanManage(user, projectId, ct)) return Results.Forbid();
            if (!Valid(x)) return Results.BadRequest();
            var p = await db.GameProjects.FindAsync([projectId], ct);
            if (p is null) return Results.NotFound();
            if (p.StudioId != x.StudioId) return Results.BadRequest(new { error = "Changing a project studio is not supported." });
            if (x.ProjectLeadId is not null && !await db.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.EmployeeId == x.ProjectLeadId, ct)) return Results.BadRequest(new { error = "Lead must first be a project member." });
            p.Title = x.Title.Trim(); p.Genre = x.Genre.Trim(); p.ProjectStatus = x.ProjectStatus; p.ProjectLeadId = x.ProjectLeadId; p.StartDate = x.StartDate; p.PlannedReleaseDate = x.PlannedReleaseDate; p.Budget = x.Budget;
            await db.SaveChangesAsync(ct); return Results.NoContent();
        });
        projects.MapDelete("/{projectId:int}", async (int projectId, StudioOpsDbContext db, CancellationToken ct) =>
        {
            var p = await db.GameProjects.FindAsync([projectId], ct);
            if (p is null) return Results.NotFound();
            db.GameProjects.Remove(p); await db.SaveChangesAsync(ct); return Results.NoContent();
        }).RequireAuthorization("AdminOnly");

        projects.MapGet("/{projectId:int}/members", async (int projectId, StudioOpsDbContext db, ClaimsPrincipal user, ProjectAccess access, CancellationToken ct) =>
        {
            if (!await access.CanRead(user, projectId, ct)) return Results.Forbid();
            return Results.Ok(await db.ProjectMembers.AsNoTracking().Where(m => m.ProjectId == projectId).OrderBy(m => m.Employee.LastName)
                .Select(m => new { m.EmployeeId, m.Employee.FirstName, m.Employee.LastName, m.Employee.Position, m.RoleInProject, m.JoinedAt }).ToListAsync(ct));
        });
        projects.MapPost("/{projectId:int}/members", async (int projectId, MemberInput x, StudioOpsDbContext db, ClaimsPrincipal user, ProjectAccess access, CancellationToken ct) =>
        {
            if (!await access.CanManage(user, projectId, ct)) return Results.Forbid();
            var p = await db.GameProjects.FindAsync([projectId], ct);
            if (p is null) return Results.NotFound();
            if (!ApiRules.Text(x.RoleInProject, 80) || !await db.Employees.AnyAsync(e => e.EmployeeId == x.EmployeeId && e.StudioId == p.StudioId, ct)) return Results.BadRequest();
            if (await db.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.EmployeeId == x.EmployeeId, ct)) return Results.Conflict();
            db.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, EmployeeId = x.EmployeeId, RoleInProject = x.RoleInProject.Trim() });
            await db.SaveChangesAsync(ct); return Results.Created($"/api/projects/{projectId}/members", new { x.EmployeeId });
        });
        projects.MapDelete("/{projectId:int}/members/{employeeId:int}", async (int projectId, int employeeId, StudioOpsDbContext db, ClaimsPrincipal user, ProjectAccess access, CancellationToken ct) =>
        {
            if (!await access.CanManage(user, projectId, ct)) return Results.Forbid();
            var member = await db.ProjectMembers.FindAsync([projectId, employeeId], ct);
            if (member is null) return Results.NotFound();
            if (await db.Tasks.AnyAsync(t => t.Milestone.ProjectId == projectId && t.AssignedEmployeeId == employeeId, ct)) return Results.Conflict(new { error = "Unassign employee tasks first." });
            var project = await db.GameProjects.FindAsync([projectId], ct);
            if (project?.ProjectLeadId == employeeId) return Results.Conflict(new { error = "Change project lead first." });
            db.ProjectMembers.Remove(member); await db.SaveChangesAsync(ct); return Results.NoContent();
        });

        projects.MapGet("/{projectId:int}/overview", async (int projectId, StudioOpsDbContext db, ClaimsPrincipal user, ProjectAccess access, CancellationToken ct) =>
        {
            if (!await access.CanRead(user, projectId, ct)) return Results.Forbid();
            var p = await db.GameProjects.AsNoTracking().Where(x => x.ProjectId == projectId).Select(x => new { x.Title, x.ProjectStatus, x.PlannedReleaseDate }).FirstOrDefaultAsync(ct);
            if (p is null) return Results.NotFound();
            var total = await db.Tasks.CountAsync(t => t.Milestone.ProjectId == projectId, ct);
            var done = await db.Tasks.CountAsync(t => t.Milestone.ProjectId == projectId && t.TaskStatus == "Done", ct);
            var openMilestones = await db.Milestones.AsNoTracking().Where(m => m.ProjectId == projectId && m.MilestoneStatus != "Completed").OrderBy(m => m.DueDate)
                .Select(m => new { m.MilestoneId, m.Name, m.DueDate, m.MilestoneStatus }).ToListAsync(ct);
            var builds = await db.GameBuilds.AsNoTracking().Where(b => b.ProjectId == projectId).OrderByDescending(b => b.BuildDate)
                .Select(b => new { b.BuildId, b.Version, b.Platform, b.IsStable, b.BuildDate, b.CreatedByEmployeeId }).Take(30).ToListAsync(ct);
            return Results.Ok(new { p.Title, p.ProjectStatus, p.PlannedReleaseDate, totalTasks = total, completedTasks = done, completionPercent = total == 0 ? 0 : Math.Round(100m * done / total, 1), openMilestones, builds });
        });
        projects.MapPost("/{projectId:int}/builds", async (int projectId, BuildInput x, StudioOpsDbContext db, ClaimsPrincipal user, ProjectAccess access, CancellationToken ct) =>
        {
            if (!await access.CanManage(user, projectId, ct)) return Results.Forbid();
            if (!await db.GameProjects.AnyAsync(p => p.ProjectId == projectId, ct)) return Results.NotFound();
            if (!ApiRules.Text(x.Version, 30) || !ApiRules.Text(x.Platform, 40) || !await access.AssigneeBelongsToProject(projectId, x.CreatedByEmployeeId, ct)) return Results.BadRequest();
            var build = new GameBuild { ProjectId = projectId, Version = x.Version.Trim(), Platform = x.Platform.Trim(), CreatedByEmployeeId = x.CreatedByEmployeeId, IsStable = x.IsStable };
            db.GameBuilds.Add(build); await db.SaveChangesAsync(ct); return Results.Created($"/api/projects/{projectId}/overview", new { build.BuildId });
        });
    }
}
