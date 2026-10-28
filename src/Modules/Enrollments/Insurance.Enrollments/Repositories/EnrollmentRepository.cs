using System.Data;
using Insurance.BuildingBlocks.Data;
using Insurance.Enrollments.Contracts;
using Insurance.Enrollments.Exceptions;
using Insurance.Enrollments.Models;
using Microsoft.Data.SqlClient;

namespace Insurance.Enrollments.Repositories;

public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly IDbConnectionFactory _db;
    public EnrollmentRepository(IDbConnectionFactory db) => _db = db;

    public async Task<int> CreateAsync(int userId, int policyId, DateOnly startDate, int termMonths, decimal monthlyPremium)
    {
        await using var conn = _db.Create();
        await using var cmd = conn.StoredProc("enrollments.usp_Enrollment_Create");
        cmd.AddParam("@UserId",         SqlDbType.Int, userId);
        cmd.AddParam("@PolicyId",       SqlDbType.Int, policyId);
        cmd.AddParam("@StartDate",      SqlDbType.Date, startDate.ToDateTime(TimeOnly.MinValue));
        cmd.AddParam("@TermMonths",     SqlDbType.Int, termMonths);
        cmd.AddParam("@MonthlyPremium", SqlDbType.Decimal, monthlyPremium, precision: 18, scale: 2);

        await conn.OpenAsync();
        try
        {
            return (int)(await cmd.ExecuteScalarAsync())!;
        }
        catch (SqlException ex) when (ex.Number == 50002)
        {
            throw new DuplicateEnrollmentException();
        }
    }

    public async Task DeleteAsync(int enrollmentId)
    {
        await using var conn = _db.Create();
        await using var cmd = conn.StoredProc("enrollments.usp_Enrollment_Delete");
        cmd.AddParam("@EnrollmentId", SqlDbType.Int, enrollmentId);

        await conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<Enrollment>> GetByUserAsync(int userId)
    {
        await using var conn = _db.Create();
        await using var cmd = conn.StoredProc("enrollments.usp_Enrollment_GetByUser");
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
        await using var cmd = conn.StoredProc("enrollments.usp_Enrollment_GetById");
        cmd.AddParam("@EnrollmentId", SqlDbType.Int, enrollmentId);

        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }

    public async Task<List<Enrollment>> GetManyAsync(IEnumerable<int> enrollmentIds)
    {
        await using var conn = _db.Create();
        await using var cmd = conn.StoredProc("enrollments.usp_Enrollment_GetMany");
        cmd.AddIdListParam("@EnrollmentIds", enrollmentIds);

        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<Enrollment>();
        while (await reader.ReadAsync()) list.Add(Map(reader));
        return list;
    }

    public async Task<Dictionary<int, int>> CountActiveByUserAsync(IEnumerable<int> userIds)
    {
        await using var conn = _db.Create();
        await using var cmd = conn.StoredProc("enrollments.usp_Enrollment_CountActiveByUser");
        cmd.AddIdListParam("@UserIds", userIds);

        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();
        var counts = new Dictionary<int, int>();
        while (await reader.ReadAsync())
            counts[reader.GetInt32(reader.GetOrdinal("UserId"))] = reader.GetInt32(reader.GetOrdinal("ActivePolicies"));
        return counts;
    }

    public async Task<bool> CancelAsync(int enrollmentId, int userId)
    {
        await using var conn = _db.Create();
        await using var cmd = conn.StoredProc("enrollments.usp_Enrollment_Cancel");
        cmd.AddParam("@EnrollmentId", SqlDbType.Int, enrollmentId);
        cmd.AddParam("@UserId",       SqlDbType.Int, userId);

        await conn.OpenAsync();
        // The proc reports the result as a SELECT instead of relying on rows-affected.
        return (int)(await cmd.ExecuteScalarAsync())! == 1;
    }

    private static Enrollment Map(SqlDataReader r) => new()
    {
        EnrollmentId   = r.GetInt32(r.GetOrdinal("EnrollmentId")),
        UserId         = r.GetInt32(r.GetOrdinal("UserId")),
        PolicyId       = r.GetInt32(r.GetOrdinal("PolicyId")),
        StartDate      = r.GetDateOnly("StartDate"),
        EndDate        = r.GetDateOnly("EndDate"),
        MonthlyPremium = r.GetDecimal(r.GetOrdinal("MonthlyPremium")),
        Status         = r.GetEnum<EnrollmentStatus>("Status"),
        CreatedAt      = r.GetDateTime(r.GetOrdinal("CreatedAt"))
    };
}