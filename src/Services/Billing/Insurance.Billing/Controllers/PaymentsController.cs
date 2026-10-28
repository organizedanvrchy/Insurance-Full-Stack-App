using System.Security.Claims;
using Insurance.Billing.DTOs;
using Insurance.Billing.Services;
using Insurance.BuildingBlocks.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Insurance.Billing.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _payments;
    public PaymentsController(IPaymentService payments) => _payments = payments;

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("enrollments/{enrollmentId:int}/payments")]
    [ProducesResponseType(typeof(List<PaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<PaymentDto>>> GetSchedule(int enrollmentId)
    {
        var schedule = await _payments.GetScheduleAsync(
            enrollmentId, CurrentUserId, 
            User.IsInRole(nameof(UserRole.Admin))
        );

        if (schedule is null) return NotFound();
        return Ok(schedule);
    }

    // Errors (404 not found/not yours, 
    // 409 already paid/out of order/inactive) 
    // are translated by ApiExceptionHandler.
    [HttpPost("payments/{id:int}/pay")]
    [Authorize(Roles = nameof(UserRole.Customer))]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PaymentDto>> Pay(int id)
    {
        var paid = await _payments.PayAsync(id, CurrentUserId);
        return Ok(paid);
    }
}