using System.Data;
using Insurance.BuildingBlocks.Data;
using Insurance.BuildingBlocks.Messaging;
using Insurance.Enrollments.Contracts;
using Insurance.Enrollments.Exceptions;
using Insurance.Enrollments.Models;
using Microsoft.Data.SqlClient;

namespace Insurance.Enrollments.Repositories;

public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly IDbConnectionFactory _db;
    private readonly IOutbox _outbox;

    public EnrollmentRepository(IDbConnectionFactory db, IOutbox outbox)
    {
        _db = db;
        _outbox = outbox;
    }

    // The enrollment row and its EnrollmentRequested event commit together (transactional outbox).
    public async Task<int> CreatePendingAsync(int userId, int policyId, int quoteId, DateOnly startDate, int termMonths, decimal monthlyPremium)
    {
        await using var conn = _db.Create();
        await conn.OpenAsync();
        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync();

        await using var cmd = conn.StoredProc("enrollments.usp_Enrollment_CreatePending");
        cmd.Transaction = tx;
        cmd.AddParam("@UserId",         SqlDbType.Int, userId);
        cmd.AddParam("@PolicyId",       SqlDbType.Int, policyId);
        cmd.AddParam("@QuoteId",        SqlDbType.Int, quoteId);
        cmd.AddParam("@StartDate",      SqlDbType.Date, startDate.ToDateTime(TimeOnly.MinValue));
        cmd.AddParam("@TermMonths",     SqlDbType.Int, termMonths);
        cmd.AddParam("@MonthlyPremium", SqlDbType.Decimal, monthlyPremium, precision: 18, scale: 2);

        int id;
        try
        {
            id = (int)(await cmd.ExecuteScalarAsync())!;
        }
        catch (SqlException ex) when (ex.Number == 50002)
        {
            throw new DuplicateEnrollmentException();
        }

        await _outbox.EnqueueAsync(conn, tx, new EnrollmentRequested(id, userId, startDate, termMonths, monthlyPremium));
        await tx.CommitAsync();
        _outbox.Notify();
        return id;
    }

    // Pending -> Active or Failed. Returns the row as it is now, even if it was already completed.
    public async Task<Enrollment?> CompleteAsync(int enrollmentId, bool succeeded)
    {
        await using var conn = _db.Create();
        await using var cmd = conn.StoredProc("enrollments.usp_Enrollment_Complete");
        cmd.AddParam("@EnrollmentId", SqlDbType.Int, enrollmentId);
        cmd.AddParam("@Succeeded",    SqlDbType.Bit, succeeded);

        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
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

    // The status change and its EnrollmentCancelled event commit together (transactional outbox).
    public async Task<bool> CancelAsync(int enrollmentId, int userId)
    {
        await using var conn = _db.Create();
        await conn.OpenAsync();
        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync();

        await using var cmd = conn.StoredProc("enrollments.usp_Enrollment_Cancel");
        cmd.Transaction = tx;
        cmd.AddParam("@EnrollmentId", SqlDbType.Int, enrollmentId);
        cmd.AddParam("@UserId",       SqlDbType.Int, userId);

        // The proc reports the result as a SELECT instead of relying on rows-affected.
        var cancelled = (int)(await cmd.ExecuteScalarAsync())! == 1;
        if (cancelled)
            await _outbox.EnqueueAsync(conn, tx, new EnrollmentCancelled(enrollmentId, userId));

        await tx.CommitAsync();
        _outbox.Notify();
        return cancelled;
    }

    private static Enrollment Map(SqlDataReader r) => new()
    {
        EnrollmentId   = r.GetInt32(r.GetOrdinal("EnrollmentId")),
        UserId         = r.GetInt32(r.GetOrdinal("UserId")),
        PolicyId       = r.GetInt32(r.GetOrdinal("PolicyId")),
        QuoteId        = r.GetInt32(r.GetOrdinal("QuoteId")),
        StartDate      = r.GetDateOnly("StartDate"),
        EndDate        = r.GetDateOnly("EndDate"),
        MonthlyPremium = r.GetDecimal(r.GetOrdinal("MonthlyPremium")),
        Status         = r.GetEnum<EnrollmentStatus>("Status"),
        CreatedAt      = r.GetDateTime(r.GetOrdinal("CreatedAt"))
    };
}