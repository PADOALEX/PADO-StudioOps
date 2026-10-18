using PADO.StudioOps.Api.Domain;
using PADO.StudioOps.Api.Security;
using Xunit;

namespace PADO.StudioOps.UnitTests;

public sealed class DomainTests
{
    [Theory]
    [InlineData("To Do", "todo")]
    [InlineData("In Progress", "in-progress")]
    [InlineData("Code Review", "in-progress")]
    [InlineData("Testing", "in-progress")]
    [InlineData("Done", "done")]
    public void KanbanProjectionKeepsOriginalWorkflow(string preciseStatus, string expected) =>
        Assert.Equal(expected, StudioOpsValues.KanbanColumn(preciseStatus));

    [Fact]
    public void InvalidRolesAreRejected() =>
        Assert.False(StudioOpsValues.Valid("SuperAdmin", StudioOpsValues.Roles));

    [Fact]
    public void ValidationRequiresValidReleaseOrder() =>
        Assert.False(ApiRules.ReleaseAfterStart(new DateOnly(2025, 1, 1), new DateOnly(2026, 1, 1)));

    [Theory]
    [InlineData("a@example.com", true)]
    [InlineData("not an email", false)]
    [InlineData("", false)]
    public void EmailValidation(string value, bool expected) => Assert.Equal(expected, ApiRules.Email(value));
}
