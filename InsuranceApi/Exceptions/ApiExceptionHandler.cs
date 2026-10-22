using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceApi.Exceptions;

public class ApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            BusinessRuleException e     => (StatusCodes.Status400BadRequest, e.Message),
            NotFoundException e         => (StatusCodes.Status404NotFound, e.Message),
            ClaimConflictException e    => (StatusCodes.Status409Conflict, e.Message),
            ConflictException e         => (StatusCodes.Status409Conflict, e.Message),
            _                           => (0, string.Empty)
        };

        if (status == 0) return false;      // not ours: let the default 500 handling run

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails { Status = status, Title = title }, cancellationToken);
        return true;
    }
}