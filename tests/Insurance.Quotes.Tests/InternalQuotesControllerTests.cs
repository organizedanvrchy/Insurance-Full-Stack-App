using System.Security.Claims;
using Insurance.BuildingBlocks.Exceptions;
using Insurance.Quotes.Contracts;
using Insurance.Quotes.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Insurance.Quotes.Tests;

// The Enrollments service reserves quotes with the customer's forwarded token.
// Ownership, expiry and one-time use are checked in usp_Quote_Reserve (covered by the end-to-end run).

public class InternalQuotesControllerTests
{
    private readonly FakeQuoteReservations _quotes = new();

    private InternalQuotesController ControllerFor(int callerId) => new(_quotes)
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, callerId.ToString()) }, "test"))
            }
        }
    };

    [Fact]
    public async Task The_caller_can_reserve_a_quote_for_themselves()
    {
        var result = await ControllerFor(5).Reserve(42, new InternalQuotesController.ReserveRequest(5));

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal((42, 5), _quotes.LastReserve);
    }

    [Fact]
    public async Task Nobody_can_reserve_a_quote_on_someone_elses_behalf()
    {
        await Assert.ThrowsAsync<NotFoundException>(
            () => ControllerFor(5).Reserve(42, new InternalQuotesController.ReserveRequest(6)));
        Assert.Null(_quotes.LastReserve);
    }
}

public class FakeQuoteReservations : IQuoteReservations
{
    public (int QuoteId, int UserId)? LastReserve { get; private set; }

    public Task<RedeemableQuote> ReserveAsync(int quoteId, int userId)
    {
        LastReserve = (quoteId, userId);
        return Task.FromResult(new RedeemableQuote(quoteId, 1, 100m));
    }

    public Task ReleaseAsync(int quoteId) => Task.CompletedTask;
}
