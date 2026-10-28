using Insurance.Catalog.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Insurance.Catalog.Controllers;

// For other services only (the gateway never routes /internal). Called with the user's forwarded token.
[ApiController]
[Route("internal/plans")]
[Authorize]
public class InternalPlansController : ControllerBase
{
    private readonly IPlanCatalog _plans;
    public InternalPlansController(IPlanCatalog plans) => _plans = plans;

    // GET /internal/plans?ids=1&ids=2 -> every plan in the list, active or not
    [HttpGet]
    public async Task<ActionResult<List<PlanInfo>>> GetMany([FromQuery] int[] ids) =>
        Ok((await _plans.GetManyAsync(ids)).Values.ToList());
}
