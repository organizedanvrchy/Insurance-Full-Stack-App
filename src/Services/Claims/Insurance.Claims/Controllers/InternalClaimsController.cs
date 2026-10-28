using Insurance.Claims.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Insurance.Claims.Controllers;

// For the gateway's BFF (admin customer list). The gateway never routes /internal.
[ApiController]
[Route("internal/claims")]
[Authorize]
public class InternalClaimsController : ControllerBase
{
    private readonly IClaimStatistics _claims;
    public InternalClaimsController(IClaimStatistics claims) => _claims = claims;

    // GET /internal/claims/counts?userIds=1&userIds=2 -> { "1": { totalClaims, openClaims } }; users with none are left out
    [HttpGet("counts")]
    public async Task<ActionResult<IReadOnlyDictionary<int, ClaimCounts>>> GetCounts([FromQuery] int[] userIds) =>
        Ok(await _claims.GetCountsByUserAsync(userIds));
}
