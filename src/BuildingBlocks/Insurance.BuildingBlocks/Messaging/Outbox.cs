using System.Data;
using System.Text.Json;
using Insurance.BuildingBlocks.Data;
using MassTransit;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Insurance.BuildingBlocks.Messaging;

/// <summary>
/// Transactional outbox. A service that must publish an event because of a database change writes the event
/// to dbo.OutboxMessages in the SAME transaction as the change. <see cref="OutboxDispatcher"/> then publishes it.
/// So an event is never published for a change that rolled back, and never lost if the process dies right after
/// the commit. Delivery is at least once: consumers must be idempotent.
/// </summary>
public interface IOutbox
{
    Task EnqueueAsync(SqlConnection connection, SqlTransaction transaction, object message);

    /// <summary>Wakes the dispatcher after a commit, so events go out at once instead of on the next poll.</summary>
    void Notify();
}

public class SqlOutbox : IOutbox
{
    private readonly SemaphoreSlim _signal = new(0);

    public async Task EnqueueAsync(SqlConnection connection, SqlTransaction transaction, object message)
    {
        var type = message.GetType();
        await using var cmd = new SqlCommand(
            "INSERT INTO dbo.OutboxMessages (MessageType, Payload) VALUES (@MessageType, @Payload);", connection, transaction);
        cmd.AddParam("@MessageType", SqlDbType.NVarChar, $"{type.FullName}, {type.Assembly.GetName().Name}", 300);
        cmd.AddParam("@Payload", SqlDbType.NVarChar, JsonSerializer.Serialize(message, type));
        await cmd.ExecuteNonQueryAsync();
    }

    public void Notify()
    {
        if (_signal.CurrentCount == 0) _signal.Release();
    }

    internal Task WaitAsync(TimeSpan timeout, CancellationToken ct) => _signal.WaitAsync(timeout, ct);
}

public class OutboxDispatcher : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    private readonly IDbConnectionFactory _db;
    private readonly IBus _bus;
    private readonly SqlOutbox _outbox;
    private readonly ILogger<OutboxDispatcher> _logger;

    public OutboxDispatcher(IDbConnectionFactory db, IBus bus, SqlOutbox outbox, ILogger<OutboxDispatcher> logger)
    {
        _db = db;
        _bus = bus;
        _outbox = outbox;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                while (await DispatchBatchAsync(stoppingToken) > 0) { }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Publishing from the outbox failed; will retry.");
            }

            try { await _outbox.WaitAsync(PollInterval, stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }

    // READPAST skips rows another instance of this service is publishing right now.
    private async Task<int> DispatchBatchAsync(CancellationToken ct)
    {
        await using var conn = _db.Create();
        await conn.OpenAsync(ct);
        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(ct);

        var batch = new List<(long Id, string Type, string Payload)>();
        await using (var select = new SqlCommand("""
            SELECT TOP (50) Id, MessageType, Payload
            FROM dbo.OutboxMessages WITH (UPDLOCK, READPAST, ROWLOCK)
            WHERE PublishedAt IS NULL
            ORDER BY Id;
            """, conn, tx))
        await using (var reader = await select.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
                batch.Add((reader.GetInt64(0), reader.GetString(1), reader.GetString(2)));
        }

        foreach (var (id, typeName, payload) in batch)
        {
            var type = Type.GetType(typeName, throwOnError: true)!;
            await _bus.Publish(JsonSerializer.Deserialize(payload, type)!, type, ct);

            await using var mark = new SqlCommand("UPDATE dbo.OutboxMessages SET PublishedAt = SYSUTCDATETIME() WHERE Id = @Id;", conn, tx);
            mark.AddParam("@Id", SqlDbType.BigInt, id);
            await mark.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
        return batch.Count;
    }
}

public static class OutboxRegistration
{
    /// <summary>The service's database needs the dbo.OutboxMessages table (see its Database scripts).</summary>
    public static IServiceCollection AddSqlOutbox(this IServiceCollection services)
    {
        services.AddSingleton<SqlOutbox>();
        services.AddSingleton<IOutbox>(sp => sp.GetRequiredService<SqlOutbox>());
        services.AddHostedService<OutboxDispatcher>();
        return services;
    }
}
