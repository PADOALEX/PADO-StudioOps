using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PADO.StudioOps.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialStudioOps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "studios",
                columns: table => new
                {
                    studio_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    country = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    founded_year = table.Column<short>(type: "smallint", nullable: false),
                    website = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_studios", x => x.studio_id);
                    table.CheckConstraint("ck_studios_founded_year", "founded_year BETWEEN 1970 AND 2100");
                });

            migrationBuilder.CreateTable(
                name: "technologies",
                columns: table => new
                {
                    technology_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    category = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    version = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_technologies", x => x.technology_id);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    employee_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    studio_id = table.Column<int>(type: "integer", nullable: true),
                    first_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    last_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    position = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    hire_date = table.Column<DateOnly>(type: "date", nullable: false),
                    salary = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.employee_id);
                    table.CheckConstraint("ck_employees_salary", "salary >= 0");
                    table.ForeignKey(
                        name: "FK_employees_studios_studio_id",
                        column: x => x.studio_id,
                        principalTable: "studios",
                        principalColumn: "studio_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "app_users",
                columns: table => new
                {
                    app_user_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_id = table.Column<int>(type: "integer", nullable: true),
                    email = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_users", x => x.app_user_id);
                    table.CheckConstraint("ck_app_users_role", "role IN ('Admin','ProjectManager','Developer')");
                    table.ForeignKey(
                        name: "FK_app_users_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "employee_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employee_profiles",
                columns: table => new
                {
                    employee_id = table.Column<int>(type: "integer", nullable: false),
                    birth_date = table.Column<DateOnly>(type: "date", nullable: false),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    city = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    bio = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_profiles", x => x.employee_id);
                    table.ForeignKey(
                        name: "FK_employee_profiles_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "employee_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "game_projects",
                columns: table => new
                {
                    project_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    studio_id = table.Column<int>(type: "integer", nullable: false),
                    project_lead_id = table.Column<int>(type: "integer", nullable: true),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    genre = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    project_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    planned_release_date = table.Column<DateOnly>(type: "date", nullable: true),
                    budget = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_projects", x => x.project_id);
                    table.CheckConstraint("ck_game_projects_budget", "budget >= 0");
                    table.CheckConstraint("ck_game_projects_status", "project_status IN ('Concept','Pre-production','Production','Testing','Released','Cancelled')");
                    table.ForeignKey(
                        name: "FK_game_projects_employees_project_lead_id",
                        column: x => x.project_lead_id,
                        principalTable: "employees",
                        principalColumn: "employee_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_game_projects_studios_studio_id",
                        column: x => x.studio_id,
                        principalTable: "studios",
                        principalColumn: "studio_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "game_builds",
                columns: table => new
                {
                    build_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    created_by_employee_id = table.Column<int>(type: "integer", nullable: true),
                    version = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    platform = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    build_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_stable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_builds", x => x.build_id);
                    table.ForeignKey(
                        name: "FK_game_builds_employees_created_by_employee_id",
                        column: x => x.created_by_employee_id,
                        principalTable: "employees",
                        principalColumn: "employee_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_game_builds_game_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "game_projects",
                        principalColumn: "project_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "milestones",
                columns: table => new
                {
                    milestone_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    milestone_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestones", x => x.milestone_id);
                    table.CheckConstraint("ck_milestones_status", "milestone_status IN ('Planned','In Progress','Completed','Delayed')");
                    table.ForeignKey(
                        name: "FK_milestones_game_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "game_projects",
                        principalColumn: "project_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_members",
                columns: table => new
                {
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    employee_id = table.Column<int>(type: "integer", nullable: false),
                    role_in_project = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    joined_at = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_members", x => new { x.project_id, x.employee_id });
                    table.ForeignKey(
                        name: "FK_project_members_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "employee_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_project_members_game_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "game_projects",
                        principalColumn: "project_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_technologies",
                columns: table => new
                {
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    technology_id = table.Column<int>(type: "integer", nullable: false),
                    purpose = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_technologies", x => new { x.project_id, x.technology_id });
                    table.ForeignKey(
                        name: "FK_project_technologies_game_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "game_projects",
                        principalColumn: "project_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_technologies_technologies_technology_id",
                        column: x => x.technology_id,
                        principalTable: "technologies",
                        principalColumn: "technology_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tasks",
                columns: table => new
                {
                    task_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    milestone_id = table.Column<int>(type: "integer", nullable: false),
                    assigned_employee_id = table.Column<int>(type: "integer", nullable: true),
                    title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    task_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    estimated_hours = table.Column<decimal>(type: "numeric(7,2)", precision: 7, scale: 2, nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tasks", x => x.task_id);
                    table.CheckConstraint("ck_tasks_estimated_hours", "estimated_hours > 0");
                    table.CheckConstraint("ck_tasks_priority", "priority IN ('Low','Medium','High','Critical')");
                    table.CheckConstraint("ck_tasks_revision", "revision > 0");
                    table.CheckConstraint("ck_tasks_status", "task_status IN ('To Do','In Progress','Code Review','Testing','Done')");
                    table.ForeignKey(
                        name: "FK_tasks_employees_assigned_employee_id",
                        column: x => x.assigned_employee_id,
                        principalTable: "employees",
                        principalColumn: "employee_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tasks_milestones_milestone_id",
                        column: x => x.milestone_id,
                        principalTable: "milestones",
                        principalColumn: "milestone_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_app_users_email",
                table: "app_users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_app_users_employee_id",
                table: "app_users",
                column: "employee_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_employees_email",
                table: "employees",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_employees_studio_id",
                table: "employees",
                column: "studio_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_builds_created_by_employee_id",
                table: "game_builds",
                column: "created_by_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_builds_project_id",
                table: "game_builds",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_projects_project_lead_id",
                table: "game_projects",
                column: "project_lead_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_projects_studio_id",
                table: "game_projects",
                column: "studio_id");

            migrationBuilder.CreateIndex(
                name: "ix_game_projects_title",
                table: "game_projects",
                column: "title",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_milestones_project_id",
                table: "milestones",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_members_employee_id",
                table: "project_members",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_project_technologies_technology_id",
                table: "project_technologies",
                column: "technology_id");

            migrationBuilder.CreateIndex(
                name: "ix_studios_name",
                table: "studios",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tasks_assigned_employee_id",
                table: "tasks",
                column: "assigned_employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_tasks_milestone_id",
                table: "tasks",
                column: "milestone_id");

            migrationBuilder.CreateIndex(
                name: "ix_technologies_name",
                table: "technologies",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "app_users");

            migrationBuilder.DropTable(
                name: "employee_profiles");

            migrationBuilder.DropTable(
                name: "game_builds");

            migrationBuilder.DropTable(
                name: "project_members");

            migrationBuilder.DropTable(
                name: "project_technologies");

            migrationBuilder.DropTable(
                name: "tasks");

            migrationBuilder.DropTable(
                name: "technologies");

            migrationBuilder.DropTable(
                name: "milestones");

            migrationBuilder.DropTable(
                name: "game_projects");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "studios");
        }
    }
}
