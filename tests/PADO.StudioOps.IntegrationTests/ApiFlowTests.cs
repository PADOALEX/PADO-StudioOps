using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace PADO.StudioOps.IntegrationTests;

public sealed class StudioOpsFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connection = Environment.GetEnvironmentVariable("STUDIOOPS_TEST_DB");
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("STUDIOOPS_TEST_DB must point to an isolated PostgreSQL test database (CI provisions one).");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:StudioOps"] = connection,
            ["STUDIOOPS_AUTO_MIGRATE"] = "true",
            ["STUDIOOPS_BOOTSTRAP_ADMIN"] = "true",
            ["STUDIOOPS_DEMO_SEED"] = "false",
            ["STUDIOOPS_ADMIN_EMAIL"] = "ci-admin@studioops.test",
            ["STUDIOOPS_ADMIN_PASSWORD"] = "CI-only-complex-password-123!"
        }));
    }
}

public sealed class ApiFlowTests : IClassFixture<StudioOpsFactory>
{
    private readonly StudioOpsFactory _factory;
    public ApiFlowTests(StudioOpsFactory factory) => _factory = factory;

    private static async Task<T> RequireJson<T>(HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private sealed record CsrfResponse(string Token);
    private sealed record StudioCreated(int StudioId);
    private sealed record EmployeeCreated(int EmployeeId);
    private sealed record ProjectCreated(int ProjectId);
    private sealed record MilestoneCreated(int MilestoneId);
    private sealed record TaskCreated(int TaskId, int Revision);
    private sealed record OverviewResponse(int TotalTasks, int CompletedTasks, decimal CompletionPercent);

    [Fact]
    public async Task AdminCanCreateStudioProjectMilestoneAndMoveTicket()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true, AllowAutoRedirect = false });
        var unauthorized = await client.GetAsync("/api/projects");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        var csrf = await RequireJson<CsrfResponse>(await client.GetAsync("/api/auth/csrf"));
        var noCsrf = await client.PostAsJsonAsync("/api/auth/login", new { email = "ci-admin@studioops.test", password = "CI-only-complex-password-123!" });
        Assert.Equal(HttpStatusCode.BadRequest, noCsrf.StatusCode);

        async Task<HttpResponseMessage> Send(HttpMethod method, string path, object? body = null)
        {
            var request = new HttpRequestMessage(method, path);
            request.Headers.Add("X-CSRF-TOKEN", csrf.Token);
            if (body is not null) request.Content = JsonContent.Create(body);
            return await client.SendAsync(request);
        }
        Assert.True((await Send(HttpMethod.Post, "/api/auth/login", new { email = "ci-admin@studioops.test", password = "CI-only-complex-password-123!" })).IsSuccessStatusCode);
        // The anonymous antiforgery token is not valid for an authenticated principal.
        csrf = await RequireJson<CsrfResponse>(await client.GetAsync("/api/auth/csrf"));
        var unique = Guid.NewGuid().ToString("N")[..9];
        var studio = await RequireJson<StudioCreated>(await Send(HttpMethod.Post, "/api/studios", new { name = $"CI Studio {unique}", country = "Ukraine", foundedYear = 2026, website = (string?)null }));
        var employee = await RequireJson<EmployeeCreated>(await Send(HttpMethod.Post, "/api/employees", new { studioId = studio.StudioId, firstName = "CI", lastName = "Developer", email = $"dev-{unique}@ci.test", position = "Developer", hireDate = "2026-10-18", salary = 1000 }));
        var project = await RequireJson<ProjectCreated>(await Send(HttpMethod.Post, "/api/projects", new { studioId = studio.StudioId, projectLeadId = employee.EmployeeId, title = $"CI Project {unique}", genre = "Horror", projectStatus = "Production", startDate = "2026-10-18", plannedReleaseDate = "2027-03-01", budget = 10000 }));
        var milestone = await RequireJson<MilestoneCreated>(await Send(HttpMethod.Post, $"/api/projects/{project.ProjectId}/milestones", new { name = "Alpha", dueDate = "2027-01-15", milestoneStatus = "Planned" }));
        var task = await RequireJson<TaskCreated>(await Send(HttpMethod.Post, $"/api/projects/{project.ProjectId}/tasks", new { milestoneId = milestone.MilestoneId, assignedEmployeeId = employee.EmployeeId, title = "CI task", description = "Test end to end", priority = "High", taskStatus = "To Do", estimatedHours = 2, dueDate = "2027-01-10" }));
        var moved = await RequireJson<TaskCreated>(await Send(HttpMethod.Patch, $"/api/projects/{project.ProjectId}/tasks/{task.TaskId}/status", new { status = "Done", revision = task.Revision }));
        Assert.Equal(2, moved.Revision);
        var stale = await Send(HttpMethod.Patch, $"/api/projects/{project.ProjectId}/tasks/{task.TaskId}/status", new { status = "To Do", revision = 1 });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        var overview = await RequireJson<OverviewResponse>(await client.GetAsync($"/api/projects/{project.ProjectId}/overview"));
        Assert.Equal(1, overview.TotalTasks); Assert.Equal(1, overview.CompletedTasks); Assert.Equal(100m, overview.CompletionPercent);
        // Provision a developer whose employee record is in the studio but is not a member
        // of this project: authorization must reject reads and writes.
        var outsider = await RequireJson<EmployeeCreated>(await Send(HttpMethod.Post, "/api/employees", new {
            studioId = studio.StudioId, firstName = "Other", lastName = "Engineer",
            email = $"outside-{unique}@ci.test", position = "Developer", hireDate = "2026-10-18", salary = 900
        }));
        var outsideEmail = $"outside-login-{unique}@ci.test";
        var userResponse = await Send(HttpMethod.Post, "/api/admin/users", new {
            email = outsideEmail, password = "Dev-test-password-123!", role = "Developer", employeeId = outsider.EmployeeId
        });
        Assert.Equal(HttpStatusCode.Created, userResponse.StatusCode);
        using var devClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true, AllowAutoRedirect = false });
        var devCsrf = await RequireJson<CsrfResponse>(await devClient.GetAsync("/api/auth/csrf"));
        var devLogin = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login");
        devLogin.Headers.Add("X-CSRF-TOKEN", devCsrf.Token);
        devLogin.Content = JsonContent.Create(new { email = outsideEmail, password = "Dev-test-password-123!" });
        Assert.True((await devClient.SendAsync(devLogin)).IsSuccessStatusCode);
        var authenticatedDevCsrf = await RequireJson<CsrfResponse>(await devClient.GetAsync("/api/auth/csrf"));
        Assert.Equal(HttpStatusCode.Forbidden, (await devClient.GetAsync($"/api/projects/{project.ProjectId}/tasks")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await devClient.GetAsync("/api/employees")).StatusCode);
        var devMove = new HttpRequestMessage(HttpMethod.Patch, $"/api/projects/{project.ProjectId}/tasks/{task.TaskId}/status");
        devMove.Headers.Add("X-CSRF-TOKEN", authenticatedDevCsrf.Token);
        devMove.Content = JsonContent.Create(new { status = "To Do", revision = moved.Revision });
        Assert.Equal(HttpStatusCode.Forbidden, (await devClient.SendAsync(devMove)).StatusCode);
    }
}
