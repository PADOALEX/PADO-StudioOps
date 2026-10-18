using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PADO.StudioOps.Api.Data;
using PADO.StudioOps.Api.Domain;

namespace PADO.StudioOps.Api.Security;

public sealed class ProjectAccess(StudioOpsDbContext db)
{
    public static int? EmployeeId(ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue("employee_id"), out var id) ? id : null;
    public static bool IsAdmin(ClaimsPrincipal user) => user.IsInRole("Admin");
    public static bool IsManager(ClaimsPrincipal user) => user.IsInRole("ProjectManager");

    public async Task<bool> CanRead(ClaimsPrincipal user, int projectId, CancellationToken ct = default)
    {
        if (IsAdmin(user)) return true;
        var id = EmployeeId(user);
        return id is not null && await db.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.EmployeeId == id, ct);
    }
    public async Task<bool> CanManage(ClaimsPrincipal user, int projectId, CancellationToken ct = default) =>
        IsAdmin(user) || (IsManager(user) && await CanRead(user, projectId, ct));

    public async Task<bool> CanEditTask(ClaimsPrincipal user, int projectId, int? assignee, CancellationToken ct = default) =>
        await CanManage(user, projectId, ct) ||
        (user.IsInRole("Developer") && assignee is not null && EmployeeId(user) == assignee && await CanRead(user, projectId, ct));

    public async Task<bool> AssigneeBelongsToProject(int projectId, int? employeeId, CancellationToken ct = default) =>
        employeeId is null || await db.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.EmployeeId == employeeId, ct);
}

public static class ApiRules
{
    public static bool Text(string? value, int length) => !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= length;
    public static bool Email(string? value) => Text(value, 120) && value!.Contains('@') && !value.Any(char.IsWhiteSpace);
    public static bool ReleaseAfterStart(DateOnly? release, DateOnly start) => release is null || release >= start;
}
