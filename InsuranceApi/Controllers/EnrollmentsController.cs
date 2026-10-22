using System.Security.Claims;
using InsuranceApi.DTOs;
using InsuranceApi.Enums;
using InsuranceApi.Exceptions;
using InsuranceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EnrollmentsController: ControllerBase
{
    private readonly IEnrollmentService _enrollments;
    public EnrollmentsController(IEnrollmentService enrollments) => _enrollments = enrollments;

    // Caller Id from validated token
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("mine")]
    public async Task<ActionResult<List<EnrollmentDto>>> GetMine()
    {
        var list = await _enrollments.GetMineAsync(CurrentUserId);
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EnrollmentDto>> GetById(int id)
    {
        var dto = await _enrollments.GetByIdAsync(id, CurrentUserId, User.IsInRole(nameof(UserRole.Admin)));
        if(dto is null) return NotFound();
        return Ok(dto);
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Customer))]
    [ProducesResponseType(typeof(EnrollmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrollmentDto>> Enroll(EnrollmentRequest request)
    {
        try
        {
            var dto = await _enrollments.EnrollAsync(CurrentUserId, request);
            return CreatedAtAction(nameof(GetById), new { id = dto.EnrollmentId }, dto);
        }
        catch (BusinessRuleException ex)
        {
            return BadRequest(new ProblemDetails {Title = ex.Message, Status = 400});
        }
        catch (PolicyNotAvailableException ex)
        {
            return NotFound(new ProblemDetails {Title = ex.Message, Status = 404});
        }
        catch (DuplicateEnrollmentException ex)
        {
            return Conflict(new ProblemDetails {Title = ex.Message, Status = 409});
        }
    }

    [HttpPut("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        var cancelled = await _enrollments.CancelAsync(id, CurrentUserId);
        if (!cancelled) return NotFound();
        return NoContent();
    }
}