using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace PADO.StudioOps.Api.Security;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception exception, CancellationToken ct)
    {
        var pg = exception as PostgresException ?? (exception as DbUpdateException)?.InnerException as PostgresException;
        int status = exception switch
        {
            DbUpdateConcurrencyException => 409,
            _ when pg?.SqlState == PostgresErrorCodes.UniqueViolation => 409,
            _ when pg?.SqlState == PostgresErrorCodes.ForeignKeyViolation => 409,
            _ when pg?.SqlState == PostgresErrorCodes.CheckViolation => 400,
            _ => 500
        };
        logger.LogError(exception, "API request failed with HTTP {Status}", status);
        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status, Title = status == 500 ? "Unexpected server error" : "Database constraint or concurrency conflict",
            Detail = status == 500 ? "Please contact the administrator." : "Refresh and check the submitted values."
        }, ct);
        return true;
    }
}
