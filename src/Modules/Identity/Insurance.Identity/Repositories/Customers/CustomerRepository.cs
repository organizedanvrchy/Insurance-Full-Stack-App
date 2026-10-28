using System.Data;
using Insurance.BuildingBlocks.Data;
using Insurance.Identity.Models;
using Microsoft.Data.SqlClient;

namespace Insurance.Identity.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly IDbConnectionFactory _db;
    public CustomerRepository(IDbConnectionFactory db) => _db = db;

    public async Task<List<CustomerSummary>> GetAllAsync(string? search, bool? isActive)
    {
        await using var conn = _db.Create();
        await using var cmd = conn.StoredProc("dbo.usp_Customer_GetAll");
        cmd.AddParam("@Search",   SqlDbType.NVarChar, search, 100);   // null => DBNull => "no filter"
        cmd.AddParam("@IsActive", SqlDbType.Bit, isActive);

        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<CustomerSummary>();
        while (await reader.ReadAsync()) list.Add(Map(reader));
        return list;
    }

    public async Task<CustomerSummary?> SetActiveAsync(int userId, bool isActive)
    {
        await using var conn = _db.Create();
        await using var cmd = conn.StoredProc("dbo.usp_Customer_SetActive");
        cmd.AddParam("@UserId",   SqlDbType.Int, userId);
        cmd.AddParam("@IsActive", SqlDbType.Bit, isActive);

        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }

    private static CustomerSummary Map(SqlDataReader r) => new()
    {
        UserId         = r.GetInt32(r.GetOrdinal("UserId")),
        FullName       = r.GetString(r.GetOrdinal("FullName")),
        Email          = r.GetString(r.GetOrdinal("Email")),
        Phone          = r.GetStringOrNull("Phone"),
        IsActive       = r.GetBoolean(r.GetOrdinal("IsActive")),
        CreatedAt      = r.GetUtcDateTime("CreatedAt"),
        ActivePolicies = r.GetInt32(r.GetOrdinal("ActivePolicies")),
        TotalClaims    = r.GetInt32(r.GetOrdinal("TotalClaims")),
        OpenClaims     = r.GetInt32(r.GetOrdinal("OpenClaims"))
    };
}