using System.Security.Claims;
using Insurance.BuildingBlocks.Security;
using Insurance.Quotes.DTOs;
using Insurance.Quotes.Rating;
using Insurance.Quotes.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Insurance.Quotes.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = nameof(UserRole.Customer))]
public class QuotesController : ControllerBase
{
    private readonly IQuoteService _quotes;
    public QuotesController(IQuoteService quotes) => _quotes = quotes;

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // The question list for one plan. Angular builds its form from this.
    [HttpGet("questions/{policyId:int}")]
    public async Task<ActionResult<IReadOnlyList<QuestionDefinition>>> GetQuestions(int policyId)
    {
        var questions = await _quotes.GetQuestionsAsync(policyId);
        return Ok(questions);
    }

    // 400 covers invalid answers AND declined applicants (the message says which).
    [HttpPost]
    [ProducesResponseType(typeof(QuoteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuoteDto>> Create(QuoteRequest request)
    {
        var dto = await _quotes.CreateAsync(CurrentUserId, request);
        return CreatedAtAction(nameof(GetById), new { id = dto.QuoteId }, dto);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<QuoteDto>> GetById(int id)
    {
        var dto = await _quotes.GetByIdAsync(id, CurrentUserId);
        if (dto is null) return NotFound();
        return Ok(dto);
    }
}