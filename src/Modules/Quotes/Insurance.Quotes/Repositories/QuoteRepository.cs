using System.Data;
using Insurance.BuildingBlocks.Data;
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

    public async Task<bool> MarkUsedAsync(int quoteId, int enrollmentId)
    {
        await using var conn = _db.Create();
        await using var cmd = conn.StoredProc("quotes.usp_Quote_MarkUsed");
        cmd.AddParam("@QuoteId",      SqlDbType.Int, quoteId);
        cmd.AddParam("@EnrollmentId", SqlDbType.Int, enrollmentId);

        await conn.OpenAsync();
        return (int)(await cmd.ExecuteScalarAsync())! == 1;
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
        EnrollmentId   = r.IsDBNull(r.GetOrdinal("EnrollmentId")) ? null : r.GetInt32(r.GetOrdinal("EnrollmentId"))
    };
}