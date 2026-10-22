using System.Data;
using Microsoft.Data.SqlClient;

namespace InsuranceApi.Data;

public static class SqlExtensions
{
    /// <summary>
    /// Creates a command that calls a stored procedure 
    /// (instead of running inline SQL).
    /// </summary>
    public static SqlCommand StoredProc(this SqlConnection conn, string procName) =>
        new(procName, conn)
        {
            CommandType = CommandType.StoredProcedure
        };

    /// <summary>
    /// Adds a typed parameter. null becomes DBNull.Value 
    /// For DECIMAL always pass precision/scale, otherwise 
    /// SQL Server rounds to 0 decimal places.
    /// </summary>
    public static SqlParameter AddParam(
        this SqlCommand cmd,
        string name, SqlDbType type,
        object? value,
        int size = 0,
        byte precision = 0,
        byte scale = 0)
    {
        var param = new SqlParameter(name, type)
        {
            Value = value ?? DBNull.Value
        };
        if (size > 0)
            param.Size = size;
        if (precision > 0)
        {
            param.Precision = precision;
            param.Scale = scale;
        }
        cmd.Parameters.Add(param);
        return param;
    }

    /// <summary>
    /// Reads a string column (e.g. 'Admin') 
    /// and converts it to the matching enum. 
    /// Throws if the DB holds an unknown value.
    /// </summary>
    public static T GetEnum<T>(this SqlDataReader r, string column) where T : struct, Enum =>
        Enum.Parse<T>(r.GetString(r.GetOrdinal(column)));

    /// <summary>
    /// SQL DATE columns come back as DateTime; 
    /// this converts to DateOnly.
    /// </summary>
    public static DateOnly GetDateOnly(this SqlDataReader r, string column) =>
        DateOnly.FromDateTime(r.GetDateTime(r.GetOrdinal(column)));
        
    public static DateTime GetUtcDateTime(this SqlDataReader r, string column) =>
        DateTime.SpecifyKind(r.GetDateTime(r.GetOrdinal(column)), DateTimeKind.Utc);

    public static DateTime? GetUtcDateTimeOrNull(this SqlDataReader r, string column)
    {
        var i = r.GetOrdinal(column);
        return r.IsDBNull(i) ? null : DateTime.SpecifyKind(r.GetDateTime(i), DateTimeKind.Utc);
    }
    public static string? GetStringOrNull(this SqlDataReader r, string column)
    {
        var i = r.GetOrdinal(column);
        return r.IsDBNull(i) ? null : r.GetString(i);
    }
}
