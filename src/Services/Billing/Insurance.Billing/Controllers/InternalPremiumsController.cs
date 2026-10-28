using Insurance.Billing.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Insurance.Billing.Controllers;

// For the Enrollments service (policy cards and alerts) and the Claims service (the overdue rule).
// The gateway never routes /internal.
[ApiController]
[Route("internal/premiums")]
[Authorize]
public class InternalPremiumsController : ControllerBase
{
    private readonly IPremiumSummaries _premiums;
    public InternalPremiumsController(IPremiumSummaries premiums) => _premiums = premiums;

    // GET /internal/premiums/summaries?enrollmentIds=1&enrollmentIds=2 -> { "1": { paymentsPaid, ... } }
    [HttpGet("summaries")]
    public async Task<ActionResult<IReadOnlyDictionary<int, PremiumSummary>>> GetSummaries([FromQuery] int[] enrollmentIds) =>
        Ok(await _premiums.GetSummariesAsync(enrollmentIds));
}
