namespace PADO.StudioOps.Api.Domain;

public static class StudioOpsValues
{
    public static readonly string[] ProjectStatuses = ["Concept", "Pre-production", "Production", "Testing", "Released", "Cancelled"];
    public static readonly string[] MilestoneStatuses = ["Planned", "In Progress", "Completed", "Delayed"];
    public static readonly string[] TaskStatuses = ["To Do", "In Progress", "Code Review", "Testing", "Done"];
    public static readonly string[] Priorities = ["Low", "Medium", "High", "Critical"];
    public static readonly string[] Roles = ["Admin", "ProjectManager", "Developer"];
    public static bool Valid(string value, string[] allowed) => allowed.Contains(value, StringComparer.Ordinal);
    public static string KanbanColumn(string status) => status switch
    {
        "Done" => "done",
        "In Progress" or "Code Review" or "Testing" => "in-progress",
        _ => "todo"
    };
}

public sealed class Studio
{
    public int StudioId { get; set; }
    public string Name { get; set; } = "";
    public string Country { get; set; } = "";
    public short FoundedYear { get; set; }
    public string? Website { get; set; }
}
public sealed class Employee
{
    public int EmployeeId { get; set; }
    public int? StudioId { get; set; }
    public Studio? Studio { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Position { get; set; } = "";
    public DateOnly HireDate { get; set; }
    public decimal Salary { get; set; }
    public EmployeeProfile? Profile { get; set; }
}
public sealed class EmployeeProfile
{
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public DateOnly BirthDate { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? Bio { get; set; }
}
public sealed class GameProject
{
    public int ProjectId { get; set; }
    public int StudioId { get; set; }
    public Studio Studio { get; set; } = null!;
    public int? ProjectLeadId { get; set; }
    public Employee? ProjectLead { get; set; }
    public string Title { get; set; } = "";
    public string Genre { get; set; } = "";
    public string ProjectStatus { get; set; } = "Concept";
    public DateOnly StartDate { get; set; }
    public DateOnly? PlannedReleaseDate { get; set; }
    public decimal Budget { get; set; }
}
public sealed class Milestone
{
    public int MilestoneId { get; set; }
    public int ProjectId { get; set; }
    public GameProject Project { get; set; } = null!;
    public string Name { get; set; } = "";
    public DateOnly DueDate { get; set; }
    public string MilestoneStatus { get; set; } = "Planned";
}
public sealed class TaskItem
{
    public int TaskId { get; set; }
    public int MilestoneId { get; set; }
    public Milestone Milestone { get; set; } = null!;
    public int? AssignedEmployeeId { get; set; }
    public Employee? AssignedEmployee { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Priority { get; set; } = "Medium";
    public string TaskStatus { get; set; } = "To Do";
    public decimal EstimatedHours { get; set; } = 1;
    public DateOnly? DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int Revision { get; set; } = 1;
}
public sealed class Technology
{
    public int TechnologyId { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string? Version { get; set; }
}
public sealed class GameBuild
{
    public int BuildId { get; set; }
    public int ProjectId { get; set; }
    public GameProject Project { get; set; } = null!;
    public int? CreatedByEmployeeId { get; set; }
    public Employee? CreatedByEmployee { get; set; }
    public string Version { get; set; } = "";
    public string Platform { get; set; } = "";
    public DateTime BuildDate { get; set; } = DateTime.UtcNow;
    public bool IsStable { get; set; }
}
public sealed class ProjectMember
{
    public int ProjectId { get; set; }
    public GameProject Project { get; set; } = null!;
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    public string RoleInProject { get; set; } = "Contributor";
    public DateOnly JoinedAt { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
}
public sealed class ProjectTechnology
{
    public int ProjectId { get; set; }
    public GameProject Project { get; set; } = null!;
    public int TechnologyId { get; set; }
    public Technology Technology { get; set; } = null!;
    public string Purpose { get; set; } = "";
}
public sealed class AppUser
{
    public int AppUserId { get; set; }
    public int? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "Developer";
    public bool IsActive { get; set; } = true;
}
