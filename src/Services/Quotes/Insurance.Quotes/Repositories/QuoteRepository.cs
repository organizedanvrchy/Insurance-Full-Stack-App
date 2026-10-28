using System.Data;
using Insurance.BuildingBlocks.Data;
using Insurance.BuildingBlocks.Exceptions;
using Insurance.Quotes.Contracts;
using Insurance.Quotes.Models;
using Microsoft.Data.SqlClient;

namespace Insurance.Quotes.Repositories;

public class QuoteRepository : IQuoteRepository
{
    private readonly IDbConnectionFactory _db;
    public QuoteRepository(IDbConnectionFactory db) => _db = db;

    public async Task<int> CreateAsync(int userId, int policyId, string answersJson, string breakdownJson,
        decimal basePremium, decimal monthlyPremium, DateTime expiresAtUtc)
    {
        await using var conn = _db.Create();
        await using var cmd = conn.StoredProc("quotes.usp_Quote_Create");
        cmd.AddParam("@UserId",         SqlDbType.Int, userId);
        cmd.AddParam("@PolicyId",       SqlDbType.Int, policyId);
        cmd.AddParam("@Answers",        SqlDbType.NVarChar, answersJson);      // no size: ADO.NET sizes it from the value
        cmd.AddParam("@Breakdown",      SqlDbType.NVarChar, breakdownJson);
        cmd.AddParam("@BasePremium",    SqlDbType.Decimal, basePremium, precision: 18, scale: 2);
        cmd.AddParam("@MonthlyPremium", SqlDbType.Decimal, monthlyPremium, precision: 18, scale: 2);
        cmd.AddParam("@ExpiresAt",      SqlDbType.DateTime2, expiresAtUtc);

        await conn.OpenAsync();
        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    public async Task<Quote?> GetByIdAsync(int quoteId)
    {
        await using var conn = _db.Create();
        await using var cmd = conn.StoredProc("quotes.usp_Quote_GetById");
        cmd.AddParam("@QuoteId", SqlDbType.Int, quoteId);

        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }

    public async Task<RedeemableQuote> ReserveAsync(int quoteId, int userId)
    {
        await using var conn = _db.Create();
        await using var cmd = conn.StoredProc("quotes.usp_Quote_Reserve");
        cmd.AddParam("@QuoteId", SqlDbType.Int, quoteId);
        cmd.AddParam("@UserId",  SqlDbType.Int, userId);

        await conn.OpenAsync();
        try
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            return new RedeemableQuote(
                reader.GetInt32(reader.GetOrdinal("QuoteId")),
                reader.GetInt32(reader.GetOrdinal("PolicyId")),
                reader.GetDecimal(reader.GetOrdinal("MonthlyPremium")));
        }
        catch (SqlException ex) when (ex.Number == 50016)
        {
            throw new NotFoundException("Quote not found.");
        }
        catch (SqlException ex) when (ex.Number == 50017)
        {
            throw new BusinessRuleException(ex.Message);
        }
        catch (SqlException ex) when (ex.Number == 50018)
        {
            throw new ConflictException(ex.Message);
        }
    }

    public async Task ReleaseAsync(int quoteId)
    {
        await using var conn = _db.Create();
        await using var cmd = conn.StoredProc("quotes.usp_Quote_Release");
        cmd.AddParam("@QuoteId", SqlDbType.Int, quoteId);

        await conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task MarkUsedAsync(int quoteId, int enrollmentId)
    {
        await using var conn = _db.Create();
        await using var cmd = conn.StoredProc("quotes.usp_Quote_MarkUsed");
        cmd.AddParam("@QuoteId",      SqlDbType.Int, quoteId);
        cmd.AddParam("@EnrollmentId", SqlDbType.Int, enrollmentId);

        await conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    private static Quote Map(SqlDataReader r) => new()
    {
        QuoteId        = r.GetInt32(r.GetOrdinal("QuoteId")),
        UserId         = r.GetInt32(r.GetOrdinal("UserId")),
        PolicyId       = r.GetInt32(r.GetOrdinal("PolicyId")),
        BasePremium    = r.GetDecimal(r.GetOrdinal("BasePremium")),
        MonthlyPremium = r.GetDecimal(r.GetOrdinal("MonthlyPremium")),
        Breakdown      = r.GetString(r.GetOrdinal("Breakdown")),
        CreatedAt      = r.GetUtcDateTime("CreatedAt"),
        ExpiresAt      = r.GetUtcDateTime("ExpiresAt"),
        ReservedUntil  = r.GetUtcDateTimeOrNull("ReservedUntil"),
        EnrollmentId   = r.IsDBNull(r.GetOrdinal("EnrollmentId")) ? null : r.GetInt32(r.GetOrdinal("EnrollmentId"))
    };
}