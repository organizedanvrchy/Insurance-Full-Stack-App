using Insurance.Enrollments.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Insurance.Enrollments.Controllers;

// For other services (Billing, Claims) and the gateway's BFF. The gateway never routes /internal.
[ApiController]
[Route("internal/enrollments")]
[Authorize]
public class InternalEnrollmentsController : ControllerBase
{
    private readonly IEnrollmentLookup _enrollments;
    public InternalEnrollmentsController(IEnrollmentLookup enrollments) => _enrollments = enrollments;

    // GET /internal/enrollments?ids=1&ids=2 (Failed enrollments are never returned)
    [HttpGet]
    public async Task<ActionResult<List<EnrollmentInfo>>> GetMany([FromQuery] int[] ids) =>
        Ok((await _enrollments.GetManyAsync(ids)).Values.ToList());

    // GET /internal/enrollments/active-counts?userIds=1&userIds=2 -> { "1": 2 }; users with none are left out
    [HttpGet("active-counts")]
    public async Task<ActionResult<IReadOnlyDictionary<int, int>>> CountActive([FromQuery] int[] userIds) =>
        Ok(await _enrollments.CountActiveByUserAsync(userIds));
}
