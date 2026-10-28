using System.Data;
using Insurance.BuildingBlocks.Data;
using Insurance.BuildingBlocks.Exceptions;
using Insurance.Catalog.Contracts;
using Insurance.Enrollments.Contracts;
using Insurance.Enrollments.Enums;
using Insurance.Enrollments.Exceptions;
using Insurance.Enrollments.Models;
using Microsoft.Data.SqlClient;

namespace Insurance.Enrollments.Repositories;

public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly IDbConnectionFactory _db;
    public EnrollmentRepository(IDbConnectionFactory db) => _db = db;

    public async Task<int> CreateAsync(int userId, int quoteId, DateOnly startDate)
    {
        await using var conn = _db.Create();
        await using var cmd = conn.StoredProc("dbo.usp_Enrollment_Create");
        cmd.AddParam("@UserId",    SqlDbType.Int, userId);
        cmd.AddParam("@QuoteId",   SqlDbType.Int, quoteId);
        cmd.AddParam("@StartDate", SqlDbType.Date, startDate.ToDateTime(TimeOnly.MinValue));

        await conn.OpenAsync();
        try
        {
            return (int)(await cmd.ExecuteScalarAsync())!;
        }
        catch (SqlException ex) when (ex.Number == 50002)
        {
            throw new DuplicateEnrollmentException();
        }
        catch (SqlException ex) when (ex.Number == 50016)
        {
            throw new NotFoundException("Quote not found.");
        }
        // Policy retired since the quote, or the quote expired: the customer must start over.
        catch (SqlException ex) when (ex.Number is 50003 or 50017)
        {
            throw new BusinessRuleException(ex.Message);
        }
        catch (SqlException ex) when (ex.Number == 50018)
        {
            throw new ConflictException(ex.Message);
        }
    }

    public async Task<List<Enrollment>> GetByUserAsync(int userId)
    {
        await using var conn = _db.Create();
        await using var cmd = conn.StoredProc("dbo.usp_Enrollment_GetByUser");
        cmd.AddParam("@UserId", SqlDbType.Int, userId);

        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<Enrollment>();
        while (await reader.ReadAsync()) list.Add(Map(reader));
        return list;
    }

    public async Task<Enrollment?> GetByIdAsync(int enrollmentId)
    {
        await using var conn = _db.Create();
        await using var cmd = conn.StoredProc("dbo.usp_Enrollment_GetById");
        cmd.AddParam("@EnrollmentId", SqlDbType.Int, enrollmentId);

        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }

    public async Task<bool> CancelAsync(int enrollmentId, int userId)
    {
        await using var conn = _db.Create();
        await using var cmd = conn.StoredProc("dbo.usp_Enrollment_Cancel");
        cmd.AddParam("@EnrollmentId", SqlDbType.Int, enrollmentId);
        cmd.AddParam("@UserId",       SqlDbType.Int, userId);

        await conn.OpenAsync();
        // This proc has several statements, so it reports the result as a SELECT instead of relying on rows-affected.
        return (int)(await cmd.ExecuteScalarAsync())! == 1;
    }

    private static Enrollment Map(SqlDataReader r) => new()
    {
        EnrollmentId   = r.GetInt32(r.GetOrdinal("EnrollmentId")),
        UserId         = r.GetInt32(r.GetOrdinal("UserId")),
        PolicyId       = r.GetInt32(r.GetOrdinal("PolicyId")),
        PolicyName     = r.GetString(r.GetOrdinal("PolicyName")),
        PolicyType     = r.GetEnum<PolicyType>("PolicyType"),
        CoverageAmount = r.GetDecimal(r.GetOrdinal("CoverageAmount")),
        StartDate      = r.GetDateOnly("StartDate"),
        EndDate        = r.GetDateOnly("EndDate"),
        MonthlyPremium = r.GetDecimal(r.GetOrdinal("MonthlyPremium")),
        Status         = r.GetEnum<EnrollmentStatus>("Status"),
        CreatedAt      = r.GetDateTime(r.GetOrdinal("CreatedAt")),
        PaymentsPaid   = r.GetInt32(r.GetOrdinal("PaymentsPaid")),
        PaymentsTotal  = r.GetInt32(r.GetOrdinal("PaymentsTotal")),
        OverdueCount   = r.GetInt32(r.GetOrdinal("OverdueCount")),
        NextDueDate    = r.IsDBNull(r.GetOrdinal("NextDueDate")) ? null : r.GetDateOnly("NextDueDate"),
        NextDueAmount  = r.IsDBNull(r.GetOrdinal("NextDueAmount")) ? null : r.GetDecimal(r.GetOrdinal("NextDueAmount"))
    };
}