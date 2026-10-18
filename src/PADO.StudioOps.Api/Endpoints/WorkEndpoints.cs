using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PADO.StudioOps.Api.Data;
using PADO.StudioOps.Api.Domain;
using PADO.StudioOps.Api.Security;

namespace PADO.StudioOps.Api.Endpoints;

public static class WorkEndpoints
{
    public sealed record MilestoneInput(string Name, DateOnly DueDate, string MilestoneStatus);
    public sealed record TaskInput(int MilestoneId, int? AssignedEmployeeId, string Title, string Description, string Priority, string TaskStatus, decimal EstimatedHours, DateOnly? DueDate, int? Revision);
    public sealed record MoveInput(string Status, int Revision);
    private static bool Valid(MilestoneInput x) => ApiRules.Text(x.Name, 120) && x.DueDate != default && StudioOpsValues.Valid(x.MilestoneStatus, StudioOpsValues.MilestoneStatuses);
    private static bool Valid(TaskInput x) => ApiRules.Text(x.Title, 160) && (x.Description?.Length ?? int.MaxValue) <= 2000 && StudioOpsValues.Valid(x.Priority, StudioOpsValues.Priorities) && StudioOpsValues.Valid(x.TaskStatus, StudioOpsValues.TaskStatuses) && x.EstimatedHours is > 0 and <= 99999;

    public static void MapWork(this WebApplication app)
    {
        var milestones = app.MapGroup("/api/projects/{projectId:int}/milestones").RequireAuthorization().WithTags("Milestones");
        milestones.MapGet("/", async (int projectId, StudioOpsDbContext db, ClaimsPrincipal user, ProjectAccess access, CancellationToken ct) =>
        {
            if (!await access.CanRead(user, projectId, ct)) return Results.Forbid();
            return Results.Ok(await db.Milestones.AsNoTracking().Where(m => m.ProjectId == projectId).OrderBy(m => m.DueDate).Select(m => new { m.MilestoneId, m.ProjectId, m.Name, m.DueDate, m.MilestoneStatus }).ToListAsync(ct));
        });
        milestones.MapPost("/", async (int projectId, MilestoneInput x, StudioOpsDbContext db, ClaimsPrincipal user, ProjectAccess access, CancellationToken ct) =>
        {
            if (!await access.CanManage(user, projectId, ct)) return Results.Forbid();
            if (!Valid(x)) return Results.BadRequest();
            if (!await db.GameProjects.AnyAsync(p => p.ProjectId == projectId, ct)) return Results.NotFound();
            var m = new Milestone { ProjectId = projectId, Name = x.Name.Trim(), DueDate = x.DueDate, MilestoneStatus = x.MilestoneStatus };
            db.Milestones.Add(m); await db.SaveChangesAsync(ct);
            return Results.Created($"/api/projects/{projectId}/milestones/{m.MilestoneId}", new { m.MilestoneId });
        });
        milestones.MapPut("/{id:int}", async (int projectId, int id, MilestoneInput x, StudioOpsDbContext db, ClaimsPrincipal user, ProjectAccess access, CancellationToken ct) =>
        {
            if (!await access.CanManage(user, projectId, ct)) return Results.Forbid();
            if (!Valid(x)) return Results.BadRequest();
            var m = await db.Milestones.SingleOrDefaultAsync(m => m.MilestoneId == id && m.ProjectId == projectId, ct);
            if (m is null) return Results.NotFound();
            m.Name = x.Name.Trim(); m.DueDate = x.DueDate; m.MilestoneStatus = x.MilestoneStatus;
            await db.SaveChangesAsync(ct); return Results.NoContent();
        });
        milestones.MapDelete("/{id:int}", async (int projectId, int id, StudioOpsDbContext db, ClaimsPrincipal user, ProjectAccess access, CancellationToken ct) =>
        {
            if (!await access.CanManage(user, projectId, ct)) return Results.Forbid();
            var m = await db.Milestones.SingleOrDefaultAsync(m => m.MilestoneId == id && m.ProjectId == projectId, ct);
            if (m is null) return Results.NotFound();
            if (await db.Tasks.AnyAsync(t => t.MilestoneId == id, ct)) return Results.Conflict(new { error = "Move or delete milestone tasks first." });
            db.Milestones.Remove(m); await db.SaveChangesAsync(ct); return Results.NoContent();
        });

        var tasks = app.MapGroup("/api/projects/{projectId:int}/tasks").RequireAuthorization().WithTags("Tasks");
        tasks.MapGet("/", async (int projectId, string? search, string? status, string? priority, int? assigneeId, string? sort, StudioOpsDbContext db, ClaimsPrincipal user, ProjectAccess access, CancellationToken ct) =>
        {
            if (!await access.CanRead(user, projectId, ct)) return Results.Forbid();
            if (status is not null && !StudioOpsValues.Valid(status, StudioOpsValues.TaskStatuses)) return Results.BadRequest(new { error = "Invalid status." });
            if (priority is not null && !StudioOpsValues.Valid(priority, StudioOpsValues.Priorities)) return Results.BadRequest(new { error = "Invalid priority." });
            IQueryable<TaskItem> query = db.Tasks.AsNoTracking().Where(t => t.Milestone.ProjectId == projectId);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var escaped = search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
                if (escaped.Length > 100) return Results.BadRequest(new { error = "Search too long." });
                query = query.Where(t => EF.Functions.ILike(t.Title, "%" + escaped + "%", "\\") || EF.Functions.ILike(t.Description, "%" + escaped + "%", "\\"));
            }
            if (status is not null) query = query.Where(t => t.TaskStatus == status);
            if (priority is not null) query = query.Where(t => t.Priority == priority);
            if (assigneeId is not null) query = query.Where(t => t.AssignedEmployeeId == assigneeId);
            query = sort switch
            {
                "title" => query.OrderBy(t => t.Title),
                "deadline" => query.OrderBy(t => t.DueDate == null).ThenBy(t => t.DueDate),
                "priority" => query.OrderByDescending(t => t.Priority == "Critical" ? 4 : t.Priority == "High" ? 3 : t.Priority == "Medium" ? 2 : 1),
                "oldest" => query.OrderBy(t => t.CreatedAt),
                _ => query.OrderByDescending(t => t.UpdatedAt)
            };
            var data = await query.Take(500).Select(t => new { t.TaskId, t.MilestoneId, t.AssignedEmployeeId, t.Title, t.Description, t.Priority, t.TaskStatus, t.EstimatedHours, t.DueDate, t.CreatedAt, t.UpdatedAt, t.Revision }).ToListAsync(ct);
            return Results.Ok(data.Select(t => new { t.TaskId, t.MilestoneId, t.AssignedEmployeeId, t.Title, t.Description, t.Priority, t.TaskStatus, t.EstimatedHours, t.DueDate, t.CreatedAt, t.UpdatedAt, t.Revision, Column = StudioOpsValues.KanbanColumn(t.TaskStatus) }));
        });
        tasks.MapGet("/{id:int}", async (int projectId, int id, StudioOpsDbContext db, ClaimsPrincipal user, ProjectAccess access, CancellationToken ct) =>
        {
            if (!await access.CanRead(user, projectId, ct)) return Results.Forbid();
            var t = await db.Tasks.AsNoTracking().Where(t => t.TaskId == id && t.Milestone.ProjectId == projectId).Select(t => new { t.TaskId, t.MilestoneId, t.AssignedEmployeeId, t.Title, t.Description, t.Priority, t.TaskStatus, t.EstimatedHours, t.DueDate, t.Revision }).FirstOrDefaultAsync(ct);
            return t is null ? Results.NotFound() : Results.Ok(t);
        });
        tasks.MapPost("/", async (int projectId, TaskInput x, StudioOpsDbContext db, ClaimsPrincipal user, ProjectAccess access, CancellationToken ct) =>
        {
            if (!await access.CanRead(user, projectId, ct)) return Results.Forbid();
            if (!Valid(x)) return Results.BadRequest();
            if (!await db.Milestones.AnyAsync(m => m.MilestoneId == x.MilestoneId && m.ProjectId == projectId, ct)) return Results.BadRequest(new { error = "Milestone does not belong to project." });
            if (!await access.AssigneeBelongsToProject(projectId, x.AssignedEmployeeId, ct)) return Results.BadRequest(new { error = "Assignee must belong to project." });
            if (!await access.CanManage(user, projectId, ct) && (x.AssignedEmployeeId != ProjectAccess.EmployeeId(user) || !user.IsInRole("Developer"))) return Results.Forbid();
            var t = new TaskItem { MilestoneId = x.MilestoneId, AssignedEmployeeId = x.AssignedEmployeeId, Title = x.Title.Trim(), Description = x.Description?.Trim() ?? "", Priority = x.Priority, TaskStatus = x.TaskStatus, EstimatedHours = x.EstimatedHours, DueDate = x.DueDate };
            db.Tasks.Add(t); await db.SaveChangesAsync(ct);
            return Results.Created($"/api/projects/{projectId}/tasks/{t.TaskId}", new { t.TaskId, t.Revision });
        });
        tasks.MapPut("/{id:int}", async (int projectId, int id, TaskInput x, StudioOpsDbContext db, ClaimsPrincipal user, ProjectAccess access, CancellationToken ct) =>
        {
            var t = await db.Tasks.Include(t => t.Milestone).SingleOrDefaultAsync(t => t.TaskId == id && t.Milestone.ProjectId == projectId, ct);
            if (t is null) return Results.NotFound();
            if (!await access.CanEditTask(user, projectId, t.AssignedEmployeeId, ct)) return Results.Forbid();
            if (!Valid(x) || x.Revision is null || x.Revision <= 0) return Results.BadRequest();
            if (t.Revision != x.Revision) return Results.Conflict(new { error = "Task was updated by another user. Refresh board." });
            if (!await db.Milestones.AnyAsync(m => m.MilestoneId == x.MilestoneId && m.ProjectId == projectId, ct)) return Results.BadRequest();
            if (!await access.AssigneeBelongsToProject(projectId, x.AssignedEmployeeId, ct)) return Results.BadRequest();
            if (!await access.CanManage(user, projectId, ct) && (x.AssignedEmployeeId != t.AssignedEmployeeId || x.MilestoneId != t.MilestoneId)) return Results.Forbid();
            t.MilestoneId = x.MilestoneId; t.AssignedEmployeeId = x.AssignedEmployeeId; t.Title = x.Title.Trim(); t.Description = x.Description?.Trim() ?? "";
            t.Priority = x.Priority; t.TaskStatus = x.TaskStatus; t.EstimatedHours = x.EstimatedHours; t.DueDate = x.DueDate;
            t.UpdatedAt = DateTime.UtcNow; t.Revision++;
            await db.SaveChangesAsync(ct); return Results.Ok(new { t.TaskId, t.Revision });
        });
        tasks.MapPatch("/{id:int}/status", async (int projectId, int id, MoveInput x, StudioOpsDbContext db, ClaimsPrincipal user, ProjectAccess access, CancellationToken ct) =>
        {
            var t = await db.Tasks.Include(t => t.Milestone).SingleOrDefaultAsync(t => t.TaskId == id && t.Milestone.ProjectId == projectId, ct);
            if (t is null) return Results.NotFound();
            if (!await access.CanEditTask(user, projectId, t.AssignedEmployeeId, ct)) return Results.Forbid();
            if (!StudioOpsValues.Valid(x.Status, StudioOpsValues.TaskStatuses) || x.Revision <= 0) return Results.BadRequest();
            if (t.Revision != x.Revision) return Results.Conflict(new { error = "Task changed. Refresh board." });
            t.TaskStatus = x.Status; t.UpdatedAt = DateTime.UtcNow; t.Revision++;
            await db.SaveChangesAsync(ct); return Results.Ok(new { t.TaskId, t.Revision, t.TaskStatus });
        });
        tasks.MapDelete("/{id:int}", async (int projectId, int id, StudioOpsDbContext db, ClaimsPrincipal user, ProjectAccess access, CancellationToken ct) =>
        {
            var t = await db.Tasks.Include(t => t.Milestone).SingleOrDefaultAsync(t => t.TaskId == id && t.Milestone.ProjectId == projectId, ct);
            if (t is null) return Results.NotFound();
            if (!await access.CanManage(user, projectId, ct)) return Results.Forbid();
            db.Tasks.Remove(t); await db.SaveChangesAsync(ct); return Results.NoContent();
        });
    }
}
