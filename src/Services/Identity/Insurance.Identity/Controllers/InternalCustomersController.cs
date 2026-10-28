using Insurance.BuildingBlocks.Security;
using Insurance.Identity.Contracts;
using Insurance.Identity.DTOs;
using Insurance.Identity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Insurance.Identity.Controllers;

// For the gateway's BFF, which serves /api/admin/customers and adds the policy and claim counts.
// The gateway never routes /internal; the admin's token is forwarded, so the role check still applies here.
[ApiController]
[Route("internal/customers")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class InternalCustomersController : ControllerBase
{
    private readonly ICustomerService _customers;
    public InternalCustomersController(ICustomerService customers) => _customers = customers;

    // e.g. GET /internal/customers?search=smith&isActive=true
    [HttpGet]
    public async Task<ActionResult<List<CustomerInfo>>> GetAll([FromQuery] string? search, [FromQuery] bool? isActive) =>
        Ok(await _customers.GetAllAsync(search, isActive));

    // Deactivate or reactivate. 404 if the id isn't a customer.
    [HttpPut("{id:int}/active")]
    [ProducesResponseType(typeof(CustomerInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerInfo>> SetActive(int id, SetCustomerActiveRequest request) =>
        Ok(await _customers.SetActiveAsync(id, request.IsActive!.Value));
}
