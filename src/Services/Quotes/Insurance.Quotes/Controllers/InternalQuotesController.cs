using System.Security.Claims;
using Insurance.BuildingBlocks.Exceptions;
using Insurance.Quotes.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Insurance.Quotes.Controllers;

// For the Enrollments service's saga (the gateway never routes /internal). The customer's token is forwarded,
// so a quote can only be reserved on behalf of the customer who is enrolling.
[ApiController]
[Route("internal/quotes")]
[Authorize]
public class InternalQuotesController : ControllerBase
{
    private readonly IQuoteReservations _quotes;
    public InternalQuotesController(IQuoteReservations quotes) => _quotes = quotes;

    public record ReserveRequest(int UserId);

    [HttpPost("{id:int}/reserve")]
    public async Task<ActionResult<RedeemableQuote>> Reserve(int id, ReserveRequest request)
    {
        // Same answer as "not yours", so ids can't be probed.
        if (request.UserId.ToString() != User.FindFirstValue(ClaimTypes.NameIdentifier))
            throw new NotFoundException("Quote not found.");

        return Ok(await _quotes.ReserveAsync(id, request.UserId));
    }

    [HttpPost("{id:int}/release")]
    public async Task<IActionResult> Release(int id)
    {
        await _quotes.ReleaseAsync(id);
        return NoContent();
    }
}
