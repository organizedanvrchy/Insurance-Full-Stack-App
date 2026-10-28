using Insurance.BuildingBlocks.Security;
using Insurance.Catalog.Contracts;
using Insurance.Catalog.DTOs;
using Insurance.Catalog.Models;
using Insurance.Catalog.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Insurance.Catalog.Controllers;

// CRUD controller template. Note it knows nothing about SQL or stored procedures.
// (No service layer here because there's no business logic yet. Add one when there is,
//  e.g. EnrollmentService generating the premium schedule.)
[ApiController]
[Route("api/[controller]")]
[Authorize]                                           // any logged-in user can read
public class PoliciesController : ControllerBase
{
    private readonly IPolicyRepository _policies;
    public PoliciesController(IPolicyRepository policies) => _policies = policies;

    [HttpGet]
    public async Task<ActionResult<List<Policy>>> GetAll([FromQuery] bool includeInactive = false)
    {
        // Only admins may see deactivated policies.
        var list = await _policies.GetAllAsync(includeInactive && User.IsInRole(nameof(UserRole.Admin)));
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Policy>> GetById(int id)
    {
        var policy = await _policies.GetByIdAsync(id);
        if (policy is null) return NotFound();
        return Ok(policy);
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<Policy>> Create(PolicyRequest request)
    {
        var id = await _policies.CreateAsync(ToPolicy(request));
        var created = await _policies.GetByIdAsync(id);
        return CreatedAtAction(nameof(GetById), new { id }, created);   // 201 + Location header
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Update(int id, PolicyRequest request)
    {
        var policy = ToPolicy(request);
        policy.PolicyId = id;
        return await _policies.UpdateAsync(policy) ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Deactivate(int id) =>
        await _policies.DeactivateAsync(id) ? NoContent() : NotFound();

    private static Policy ToPolicy(PolicyRequest r) => new()
    {
        PolicyName = r.PolicyName.Trim(),
        PolicyType = r.PolicyType!.Value,
        Description = r.Description,
        CoverageAmount = r.CoverageAmount,
        MonthlyPremium = r.MonthlyPremium,
        TermMonths = r.TermMonths
    };
}
