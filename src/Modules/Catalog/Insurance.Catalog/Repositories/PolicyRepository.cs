using System.Data;
using Insurance.BuildingBlocks.Data;
using Insurance.Catalog.Contracts;
using Insurance.Catalog.Models;
using Microsoft.Data.SqlClient;

namespace Insurance.Catalog.Repositories;

// THE CRUD TEMPLATE. For Claims, Enrollments etc., copy this file and change names/columns.
public class PolicyRepository : IPolicyRepository
{
    private readonly IDbConnectionFactory _db;
    public PolicyRepository(IDbConnectionFactory db) => _db = db;

    // READ (many) -> ExecuteReader + loop
    public async Task<List<Policy>> GetAllAsync(bool includeInactive)
    {
        await using var conn = _db.Create();
        await using var cmd = conn.StoredProc("catalog.usp_Policy_GetAll");
        cmd.AddParam("@IncludeInactive", SqlDbType.Bit, includeInactive);

        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<Policy>();
        while (await reader.ReadAsync()) list.Add(Map(reader));
        return list;
    }

    // READ (by ids) -> the id list travels as one JSON parameter
    public async Task<List<Policy>> GetManyAsync(IEnumerable<int> policyIds)
    {
        await using var conn = _db.Create();
        await using var cmd = conn.StoredProc("catalog.usp_Policy_GetMany");
        cmd.AddIdListParam("@PolicyIds", policyIds);

        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<Policy>();
        while (await reader.ReadAsync()) list.Add(Map(reader));
        return list;
    }

    // READ (one) -> ExecuteReader + single Read()
    public async Task<Policy?> GetByIdAsync(int policyId)
    {
        await using var conn = _db.Create();
        await using var cmd = conn.StoredProc("catalog.usp_Policy_GetById");
        cmd.AddParam("@PolicyId", SqlDbType.Int, policyId);

        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }

    // CREATE -> ExecuteScalar (proc returns the new id)
    public async Task<int> CreateAsync(Policy p)
    {
        await using var conn = _db.Create();
        await using var cmd = conn.StoredProc("catalog.usp_Policy_Create");
        AddWriteParams(cmd, p);

        await conn.OpenAsync();
        return (int)(await cmd.ExecuteScalarAsync())!;
    }

    // UPDATE -> ExecuteNonQuery (returns rows affected)
    public async Task<bool> UpdateAsync(Policy p)
    {
        await using var conn = _db.Create();
        await using var cmd = conn.StoredProc("catalog.usp_Policy_Update");
        cmd.AddParam("@PolicyId", SqlDbType.Int, p.PolicyId);
        AddWriteParams(cmd, p);

        await conn.OpenAsync();
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    // DELETE (soft) -> ExecuteNonQuery
    public async Task<bool> DeactivateAsync(int policyId)
    {
        await using var conn = _db.Create();
        await using var cmd = conn.StoredProc("catalog.usp_Policy_Deactivate");
        cmd.AddParam("@PolicyId", SqlDbType.Int, policyId);

        await conn.OpenAsync();
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    private static void AddWriteParams(SqlCommand cmd, Policy p)
    {
        cmd.AddParam("@PolicyName",     SqlDbType.NVarChar, p.PolicyName, 100);
        cmd.AddParam("@PolicyType",     SqlDbType.NVarChar, p.PolicyType.ToString(), 30);
        cmd.AddParam("@Description",    SqlDbType.NVarChar, p.Description, 1000);
        cmd.AddParam("@CoverageAmount", SqlDbType.Decimal,  p.CoverageAmount, precision: 18, scale: 2);
        cmd.AddParam("@MonthlyPremium", SqlDbType.Decimal,  p.MonthlyPremium, precision: 18, scale: 2);
        cmd.AddParam("@TermMonths",     SqlDbType.Int,      p.TermMonths);
    }

    private static Policy Map(SqlDataReader r) => new()
    {
        PolicyId       = r.GetInt32(r.GetOrdinal("PolicyId")),
        PolicyName     = r.GetString(r.GetOrdinal("PolicyName")),
        PolicyType     = r.GetEnum<PolicyType>("PolicyType"),
        Description    = r.GetStringOrNull("Description"),
        CoverageAmount = r.GetDecimal(r.GetOrdinal("CoverageAmount")),
        MonthlyPremium = r.GetDecimal(r.GetOrdinal("MonthlyPremium")),
        TermMonths     = r.GetInt32(r.GetOrdinal("TermMonths")),
        IsActive       = r.GetBoolean(r.GetOrdinal("IsActive")),
        CreatedAt      = r.GetDateTime(r.GetOrdinal("CreatedAt"))
    };
}
