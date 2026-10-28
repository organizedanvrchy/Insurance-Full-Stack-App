using Insurance.Identity.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Insurance.Identity.Controllers;

// For other services and the gateway (which checks on every request that the caller's account is still active).
[ApiController]
[Route("internal/users")]
[Authorize]
public class InternalUsersController : ControllerBase
{
    private readonly IUserDirectory _users;
    public InternalUsersController(IUserDirectory users) => _users = users;

    // GET /internal/users?ids=1&ids=2 -> names, emails, roles and status; never the password hash
    [HttpGet]
    public async Task<ActionResult<List<UserAccount>>> GetMany([FromQuery] int[] ids) =>
        Ok((await _users.GetManyAsync(ids)).Values.ToList());
}
