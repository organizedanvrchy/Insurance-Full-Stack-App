using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceApi.Exceptions;

/// <summary>
/// The one place that turns our typed exceptions into HTTP status codes.
/// Controllers just call the service; anything thrown below lands here.
/// Unknown exceptions return false, so ASP.NET's default 500 handling still applies.
/// </summary>
public class ApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            // 400: the caller's input broke a rule
            BusinessRuleException e => (StatusCodes.Status400BadRequest, e.Message),

            // 404: it doesn't exist (or isn't yours, which we deliberately don't distinguish)
            NotFoundException e           => (StatusCodes.Status404NotFound, e.Message),
            PolicyNotAvailableException e => (StatusCodes.Status404NotFound, e.Message),
            PaymentNotFoundException e    => (StatusCodes.Status404NotFound, e.Message),

            // 409: valid request, but it conflicts with the current state
            DuplicateEmailException         => (StatusCodes.Status409Conflict, "That email is already registered."),
            DuplicateEnrollmentException e  => (StatusCodes.Status409Conflict, e.Message),
            PaymentConflictException e      => (StatusCodes.Status409Conflict, e.Message),
            ClaimConflictException e        => (StatusCodes.Status409Conflict, e.Message),
            ConflictException e             => (StatusCodes.Status409Conflict, e.Message),

            _ => (0, string.Empty)
        };

        if (status == 0) return false;      // not ours: let the default 500 handling run

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails { Status = status, Title = title }, 
            cancellationToken
        );
        
        return true;
    }
}