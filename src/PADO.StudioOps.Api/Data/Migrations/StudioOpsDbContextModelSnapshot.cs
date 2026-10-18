// Snapshot frozen at InitialStudioOps. Do not delegate to the live DbContext:
// doing so would hide differences when scaffolding future migrations.
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using PADO.StudioOps.Api.Domain;

namespace PADO.StudioOps.Api.Data.Migrations;

[DbContext(typeof(StudioOpsDbContext))]
public sealed class StudioOpsDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder b)
    {
        b.HasAnnotation("ProductVersion", "10.0.13");
        b.HasAnnotation("Relational:MaxIdentifierLength", 63);
        b.UseIdentityByDefaultColumns();
        b.Entity<Studio>(e => {
            e.ToTable("studios", t => t.HasCheckConstraint("ck_studios_founded_year", "founded_year BETWEEN 1970 AND 2100"));
            e.Property(x => x.Name).HasMaxLength(100).IsRequired(); e.HasIndex(x => x.Name).IsUnique().HasDatabaseName("ix_studios_name");
            e.Property(x => x.Country).HasMaxLength(60).IsRequired(); e.Property(x => x.Website).HasMaxLength(200);
        });
        b.Entity<Employee>(e => {
            e.ToTable("employees", t => t.HasCheckConstraint("ck_employees_salary", "salary >= 0"));
            e.HasOne(x => x.Studio).WithMany().HasForeignKey(x => x.StudioId).OnDelete(DeleteBehavior.SetNull);
            e.Property(x => x.FirstName).HasMaxLength(50).IsRequired(); e.Property(x => x.LastName).HasMaxLength(50).IsRequired();
            e.Property(x => x.Email).HasMaxLength(120).IsRequired(); e.HasIndex(x => x.Email).IsUnique().HasDatabaseName("ix_employees_email");
            e.HasIndex(x => x.StudioId).HasDatabaseName("ix_employees_studio_id");
            e.Property(x => x.Position).HasMaxLength(80).IsRequired(); e.Property(x => x.Salary).HasPrecision(12,2);
        });
        b.Entity<EmployeeProfile>(e => {
            e.ToTable("employee_profiles"); e.HasKey(x => x.EmployeeId);
            e.HasOne(x => x.Employee).WithOne(x => x.Profile).HasForeignKey<EmployeeProfile>(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Phone).HasMaxLength(30); e.Property(x => x.City).HasMaxLength(80); e.Property(x => x.Bio).HasMaxLength(500);
        });
        b.Entity<GameProject>(e => {
            e.HasKey(x => x.ProjectId); e.ToTable("game_projects", t => { t.HasCheckConstraint("ck_game_projects_budget", "budget >= 0"); t.HasCheckConstraint("ck_game_projects_status", "project_status IN ('Concept','Pre-production','Production','Testing','Released','Cancelled')"); });
            e.HasOne(x => x.Studio).WithMany().HasForeignKey(x => x.StudioId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ProjectLead).WithMany().HasForeignKey(x => x.ProjectLeadId).OnDelete(DeleteBehavior.SetNull);
            e.Property(x => x.Title).HasMaxLength(120).IsRequired(); e.HasIndex(x => x.Title).IsUnique().HasDatabaseName("ix_game_projects_title");
            e.HasIndex(x => x.StudioId).HasDatabaseName("ix_game_projects_studio_id");
            e.HasIndex(x => x.ProjectLeadId).HasDatabaseName("ix_game_projects_project_lead_id");
            e.Property(x => x.Genre).HasMaxLength(60).IsRequired(); e.Property(x => x.ProjectStatus).HasMaxLength(30).IsRequired();
            e.Property(x => x.Budget).HasPrecision(14, 2);
        });
        b.Entity<Milestone>(e => {
            e.ToTable("milestones", t => t.HasCheckConstraint("ck_milestones_status", "milestone_status IN ('Planned','In Progress','Completed','Delayed')"));
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.ProjectId).HasDatabaseName("ix_milestones_project_id"); e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.MilestoneStatus).HasMaxLength(30).IsRequired();
        });
        b.Entity<TaskItem>(e => {
            e.HasKey(x => x.TaskId); e.ToTable("tasks", t => {
                t.HasCheckConstraint("ck_tasks_priority", "priority IN ('Low','Medium','High','Critical')");
                t.HasCheckConstraint("ck_tasks_status", "task_status IN ('To Do','In Progress','Code Review','Testing','Done')");
                t.HasCheckConstraint("ck_tasks_estimated_hours", "estimated_hours > 0");
                t.HasCheckConstraint("ck_tasks_revision", "revision > 0");
            });
            e.HasOne(x => x.Milestone).WithMany().HasForeignKey(x => x.MilestoneId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.AssignedEmployee).WithMany().HasForeignKey(x => x.AssignedEmployeeId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => x.MilestoneId).HasDatabaseName("ix_tasks_milestone_id"); e.HasIndex(x => x.AssignedEmployeeId).HasDatabaseName("ix_tasks_assigned_employee_id");
            e.Property(x => x.Title).HasMaxLength(160).IsRequired(); e.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            e.Property(x => x.Priority).HasMaxLength(20).IsRequired(); e.Property(x => x.TaskStatus).HasMaxLength(30).IsRequired();
            e.Property(x => x.EstimatedHours).HasPrecision(7,2); e.Property(x => x.Revision).IsConcurrencyToken();
        });
        b.Entity<Technology>(e => { e.ToTable("technologies"); e.Property(x => x.Name).HasMaxLength(100).IsRequired(); e.HasIndex(x => x.Name).IsUnique().HasDatabaseName("ix_technologies_name"); e.Property(x => x.Category).HasMaxLength(60).IsRequired(); e.Property(x => x.Version).HasMaxLength(30); });
        b.Entity<GameBuild>(e => {
            e.HasKey(x => x.BuildId); e.ToTable("game_builds"); e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.CreatedByEmployee).WithMany().HasForeignKey(x => x.CreatedByEmployeeId).OnDelete(DeleteBehavior.SetNull);
            e.Property(x => x.Version).HasMaxLength(30).IsRequired(); e.Property(x => x.Platform).HasMaxLength(40).IsRequired();
            e.HasIndex(x => x.ProjectId).HasDatabaseName("ix_game_builds_project_id");
            e.HasIndex(x => x.CreatedByEmployeeId).HasDatabaseName("ix_game_builds_created_by_employee_id");
        });
        b.Entity<ProjectMember>(e => {
            e.ToTable("project_members"); e.HasKey(x => new { x.ProjectId, x.EmployeeId });
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.EmployeeId).HasDatabaseName("ix_project_members_employee_id");
            e.Property(x => x.RoleInProject).HasMaxLength(80).IsRequired();
        });
        b.Entity<ProjectTechnology>(e => {
            e.ToTable("project_technologies"); e.HasKey(x => new { x.ProjectId, x.TechnologyId });
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Technology).WithMany().HasForeignKey(x => x.TechnologyId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.TechnologyId).HasDatabaseName("ix_project_technologies_technology_id");
            e.Property(x => x.Purpose).HasMaxLength(120).IsRequired();
        });
        b.Entity<AppUser>(e => {
            e.ToTable("app_users", t => t.HasCheckConstraint("ck_app_users_role", "role IN ('Admin','ProjectManager','Developer')"));
            e.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.Email).IsUnique().HasDatabaseName("ix_app_users_email"); e.HasIndex(x => x.EmployeeId).IsUnique().HasDatabaseName("ix_app_users_employee_id");
            e.Property(x => x.Email).HasMaxLength(120).IsRequired(); e.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired(); e.Property(x => x.Role).HasMaxLength(30).IsRequired();
        });
        // Explicit naming strategy; no extra EF naming-conventions package required.
        foreach (var entity in b.Model.GetEntityTypes())
            foreach (var property in entity.GetProperties())
                property.SetColumnName(Regex.Replace(property.Name, "(?<!^)([A-Z])", "_$1").ToLowerInvariant());
    }
}
