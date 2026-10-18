using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PADO.StudioOps.Api.Data;
using PADO.StudioOps.Api.Domain;

namespace PADO.StudioOps.Api.Services;

public static class DemoSeed
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StudioOpsDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();
        var email = config["STUDIOOPS_ADMIN_EMAIL"]?.Trim().ToLowerInvariant();
        var password = config["STUDIOOPS_ADMIN_PASSWORD"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || password.Length < 12 || password == "replace-with-a-strong-password")
            throw new InvalidOperationException("Set STUDIOOPS_ADMIN_EMAIL and a strong STUDIOOPS_ADMIN_PASSWORD (12+ chars) before seeding.");
        if (!await db.AppUsers.AnyAsync(x => x.Role == "Admin"))
        {
            var admin = new AppUser { Email = email, Role = "Admin" };
            admin.PasswordHash = hasher.HashPassword(admin, password);
            db.AppUsers.Add(admin);
            await db.SaveChangesAsync();
        }
        if (config.GetValue<bool>("STUDIOOPS_DEMO_SEED") && !await db.Studios.AnyAsync())
        {
            var studio = new Studio { Name = "PADO Game Development", Country = "Ukraine", FoundedYear = 2024, Website = "https://github.com/PADOALEX" };
            var lead = new Employee { Studio = studio, FirstName = "Marta", LastName = "Koval", Email = "marta@studioops.demo", Position = "Project Manager", HireDate = new DateOnly(2025, 6, 2), Salary = 2100m };
            var dev = new Employee { Studio = studio, FirstName = "Andrii", LastName = "Melnyk", Email = "andrii@studioops.demo", Position = "Gameplay Developer", HireDate = new DateOnly(2026, 1, 12), Salary = 1700m };
            var project = new GameProject { Studio = studio, ProjectLead = lead, Title = "Blind Protocol", Genre = "Co-op Horror", ProjectStatus = "Production", StartDate = new DateOnly(2026, 8, 1), PlannedReleaseDate = new DateOnly(2027, 6, 1), Budget = 65000m };
            var milestone = new Milestone { Project = project, Name = "Playable Prototype", DueDate = new DateOnly(2026, 12, 15), MilestoneStatus = "In Progress" };
            db.AddRange(studio, lead, dev, project, milestone);
            await db.SaveChangesAsync();
            db.ProjectMembers.AddRange(
                new ProjectMember { ProjectId = project.ProjectId, EmployeeId = lead.EmployeeId, RoleInProject = "Lead" },
                new ProjectMember { ProjectId = project.ProjectId, EmployeeId = dev.EmployeeId, RoleInProject = "Gameplay Developer" });
            db.Tasks.AddRange(
                new TaskItem { MilestoneId = milestone.MilestoneId, AssignedEmployeeId = dev.EmployeeId, Title = "Implement terminal interactions", Description = "Connect in-world terminal actions to network state.", Priority = "High", EstimatedHours = 12, TaskStatus = "In Progress", DueDate = new DateOnly(2026, 11, 5) },
                new TaskItem { MilestoneId = milestone.MilestoneId, AssignedEmployeeId = dev.EmployeeId, Title = "Optimize bunker lighting", Description = "Profile shadow casters and frame time.", Priority = "Medium", EstimatedHours = 6, TaskStatus = "To Do" },
                new TaskItem { MilestoneId = milestone.MilestoneId, AssignedEmployeeId = lead.EmployeeId, Title = "Define first-play flow", Description = "Document tutorial and player objective sequence.", Priority = "Critical", EstimatedHours = 4, TaskStatus = "Done" });
            db.GameBuilds.Add(new GameBuild { ProjectId = project.ProjectId, CreatedByEmployeeId = dev.EmployeeId, Version = "0.2.0-alpha", Platform = "Windows", IsStable = false });
            await db.SaveChangesAsync();
        }
    }
}
