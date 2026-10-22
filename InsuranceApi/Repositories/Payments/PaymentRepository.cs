using System.Data;
using InsuranceApi.Data;
using InsuranceApi.Enums;
using InsuranceApi.Exceptions;
using InsuranceApi.Models;
using Microsoft.Data.SqlClient;

namespace InsuranceApi.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly IDbConnectionFactory _db;
    public PaymentRepository(IDbConnectionFactory db) => _db = db;

    public async Task<List<Payment>> GetByEnrollmentAsync(int enrollmentId)
    {
        await using var conn = _db.Create();
        await using var cmd = conn.StoredProc("dbo.usp_Payment_GetByEnrollment");
        cmd.AddParam("@EnrollmentId", SqlDbType.Int, enrollmentId);

        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<Payment>();
        while (await reader.ReadAsync()) list.Add(Map(reader));
        return list;
    }

    public async Task<Payment> PayAsync(int paymentId, int userId)
    {
        await using var conn = _db.Create();
        await using var cmd = conn.StoredProc("dbo.usp_Payment_Pay");
        cmd.AddParam("@PaymentId", SqlDbType.Int, paymentId);
        cmd.AddParam("@UserId", SqlDbType.Int, userId);

        await conn.OpenAsync();
        try
        {
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                throw new InvalidOperationException("usp_Payment_Pay returned no row.");
            return Map(reader);
        }
        // 50004 = not found OR not yours (the proc deliberately doesn't say which)
        catch (SqlException ex) when (ex.Number == 50004)
        {
            throw new PaymentNotFoundException(paymentId);
        }
        // 50005 already paid, 50006 enrollment not active, 50007 earlier installment unpaid.
        // The proc's THROW message is already user-friendly, so we pass it along.
        catch (SqlException ex) when (ex.Number is 50005 or 50006 or 50007)
        {
            throw new PaymentConflictException(ex.Message);
        }
    }

    private static Payment Map(SqlDataReader r) => new()
    {
        PaymentId = r.GetInt32(r.GetOrdinal("PaymentId")),
        EnrollmentId = r.GetInt32(r.GetOrdinal("EnrollmentId")),
        UserId = r.GetInt32(r.GetOrdinal("UserId")),
        DueDate = r.GetDateOnly("DueDate"),
        Amount = r.GetDecimal(r.GetOrdinal("Amount")),
        PaidDate = r.IsDBNull(r.GetOrdinal("PaidDate")) ? null : r.GetDateOnly("PaidDate"),
        Status = r.GetEnum<PaymentStatus>("Status")
    };
}