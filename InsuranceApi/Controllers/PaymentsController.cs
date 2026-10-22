using System.Security.Claims;
using InsuranceApi.DTOs;
using InsuranceApi.Enums;
using InsuranceApi.Exceptions;
using InsuranceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceApi.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _payments;
    public PaymentsController(IPaymentService payments) => _payments = payments;

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("enrollments/{enrollmentId:int}/payments")]
    public async Task<ActionResult<List<PaymentDto>>> GetSchedule(int enrollmentId)
    {
        var schedule = await _payments.GetScheduleAsync(
            enrollmentId, CurrentUserId, User.IsInRole(nameof(UserRole.Admin)));

        if (schedule is null) return NotFound();
        return Ok(schedule);
    }

    [HttpPost("payments/{id:int}/pay")]
    [Authorize(Roles = nameof(UserRole.Customer))]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PaymentDto>> Pay(int id)
    {
        try
        {
            var paid = await _payments.PayAsync(id, CurrentUserId);
            return Ok(paid);
        }
        catch (PaymentNotFoundException ex)
        {
            return NotFound(new ProblemDetails { Title = ex.Message, Status = 404 });
        }
        catch (PaymentConflictException ex)
        {
            return Conflict(new ProblemDetails { Title = ex.Message, Status = 409 });
        }
    }
}