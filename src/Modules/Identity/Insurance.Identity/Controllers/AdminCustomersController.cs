using Insurance.BuildingBlocks.Security;
using Insurance.Identity.DTOs;
using Insurance.Identity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Insurance.Identity.Controllers;

[ApiController]
[Route("api/admin/customers")]
[Authorize(Roles = nameof(UserRole.Admin))]     // Admins only
public class AdminCustomersController : ControllerBase
{
    private readonly ICustomerService _customers;
    public AdminCustomersController(ICustomerService customers) => _customers = customers;

    // e.g. GET /api/admin/customers?search=smith&isActive=true
    [HttpGet]
    public async Task<ActionResult<List<CustomerDto>>> GetAll([FromQuery] string? search, [FromQuery] bool? isActive)
    {
        var list = await _customers.GetAllAsync(search, isActive);
        return Ok(list);
    }

    // Deactivate or reactivate. Returns the updated customer so the UI can refresh the row.
    [HttpPut("{id:int}/active")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDto>> SetActive(int id, SetCustomerActiveRequest request)
    {
        var dto = await _customers.SetActiveAsync(id, request.IsActive!.Value);
        return Ok(dto);
    }
}