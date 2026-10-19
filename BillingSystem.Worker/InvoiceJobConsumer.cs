using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using BillingSystem.Application;
using BillingSystem.Domain;
using BillingSystem.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace BillingSystem.Worker;

#pragma warning disable CA1848
public sealed class InvoiceJobConsumer : BackgroundService
{
    private const int ReceiveBatchSize = 16;
    private const int EmptyPollDelaySeconds = 2;
    private const int StackTraceMaxLength = 2000;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly QueueClient _queueClient;
    private readonly QueueClient _poisonQueueClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InvoiceJobConsumer> _logger;
    private readonly QueueOptions _queueOptions;
    private readonly TimeProvider _timeProvider;

    public InvoiceJobConsumer(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        ILogger<InvoiceJobConsumer> logger,
        IOptions<QueueOptions> queueOptions,
        TimeProvider timeProvider)
    {
        var connectionString = configuration["Storage:ConnectionString"]
            ?? throw new InvalidOperationException("Storage:ConnectionString is not configured.");
        _queueClient = new QueueClient(connectionString, queueOptions.Value.InvoiceJobs);
        _queueClient.CreateIfNotExists();
        _poisonQueueClient = new QueueClient(connectionString, queueOptions.Value.InvoiceJobsPoison);
        _poisonQueueClient.CreateIfNotExists();
        _scopeFactory = scopeFactory;
        _logger = logger;
        _queueOptions = queueOptions.Value;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Response<QueueMessage[]> response;
            try
            {
                response = await _queueClient.ReceiveMessagesAsync(
                    maxMessages: ReceiveBatchSize,
                    visibilityTimeout: TimeSpan.FromSeconds(_queueOptions.VisibilityTimeoutSeconds),
                    cancellationToken: stoppingToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (RequestFailedException ex)
            {
                _logger.LogWarning(ex, "Failed to receive invoice job messages. Retrying.");
                await Task.Delay(TimeSpan.FromSeconds(EmptyPollDelaySeconds), stoppingToken);
                continue;
            }

            if (response.Value.Length == 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(EmptyPollDelaySeconds), stoppingToken);
                continue;
            }

            foreach (var message in response.Value)
            {
                try
                {
                    await ProcessMessageAsync(message, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing invoice job message {MessageId}", message.MessageId);
                }
            }
        }
    }
    private async Task ProcessMessageAsync(QueueMessage message, CancellationToken ct)
    {
        if (message.DequeueCount > _queueOptions.MaxDequeueCount)
        {
            _logger.LogWarning("DequeueCount exceeded for {MessageId}. Dead-lettering.", message.MessageId);
            await MoveToPoisonQueueAsync(message, "MaxDequeueCount exceeded", null, ct);
            return;
        }

        InvoiceJobMessage? job;
        string json;
        try
        {
            json = DecodeMessageText(message.MessageText);
            job = JsonSerializer.Deserialize<InvoiceJobMessage>(json, JsonOptions);
        }
        catch (Exception ex) when (ex is FormatException || ex is JsonException || ex is ArgumentException)
        {
            _logger.LogWarning("Malformed invoice job message {MessageId}", message.MessageId);
            await MoveToPoisonQueueAsync(message, "Malformed message", null, ct);
            return;
        }

        if (job is null)
        {
            await MoveToPoisonQueueAsync(message, "Deserialization returned null", null, ct);
            return;
        }

        if (!IsValidJob(job, out var validationError))
        {
            await MoveToPoisonQueueAsync(message, validationError, null, ct);
            return;
        }

        if (HasSchemaMismatch(json, out var schemaVersion))
        {
            var msg = FormattableString.Invariant($"Unsupported schema version: {schemaVersion}");
            await MoveToPoisonQueueAsync(message, msg, null, ct);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

        var isDuplicate = await dbContext.Invoices
            .AsNoTracking()
            .AnyAsync(i => i.IdempotencyKey == job.IdempotencyKey, ct);
        if (isDuplicate)
        {
            await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, ct);
            _logger.LogInformation("Duplicate invoice job skipped. Key={Key}", job.IdempotencyKey);
            return;
        }

        await ProcessInvoiceJobAsync(dbContext, job, message, ct);
    }


    private async Task ProcessInvoiceJobAsync(BillingDbContext db, InvoiceJobMessage job, QueueMessage message, CancellationToken ct)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            try
            {
                var sub = await db.Subscriptions.Include(s => s.Plan).FirstOrDefaultAsync(s => s.Id == job.SubscriptionId, ct);
                if (sub is null)
                {
                    await tx.RollbackAsync(ct);
                    await MoveToPoisonQueueAsync(message, "Subscription not found", null, ct);
                    return;
                }
                if (sub.Status != SubscriptionStatus.Active || sub.NextBillingDate != job.BillingPeriodStart)
                {
                    await tx.RollbackAsync(ct);
                    await RecordSkippedAsync(db, job, sub, ct);
                    await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, ct);
                    return;
                }
                var now = _timeProvider.GetUtcNow().UtcDateTime;
                var seq = await db.Database.SqlQueryRaw<long>("SELECT NEXT VALUE FOR billing.seq_InvoiceNumber AS [Value]").SingleAsync(ct);
                var issueDate = DateOnly.FromDateTime(now);
                var invoice = new Invoice(Guid.NewGuid(), sub.Id, sub.CustomerId, $"INV-{issueDate:yyyyMM}-{seq:D6}", job.BillingPeriodStart, job.BillingPeriodEnd, sub.Plan.Price, sub.Plan.Currency, InvoiceStatus.Pending, issueDate, issueDate.AddDays(14), job.IdempotencyKey, now);
                db.Invoices.Add(invoice);
                db.AuditLogs.Add(new AuditLog { EntityName = "Invoice", EntityId = invoice.Id.ToString(), Action = "InvoiceIssued", NewValue = invoice.InvoiceNumber, PerformedBy = "System", CorrelationId = job.CorrelationId, OccurredAt = now });
                var monthsDiff = ((job.BillingPeriodStart.Year - sub.StartDate.Year) * 12) + (job.BillingPeriodStart.Month - sub.StartDate.Month);
                var periodIndex = monthsDiff / BillingPeriodCalculator.MonthsOf(sub.Plan.BillingCycle);
                sub.NextBillingDate = BillingPeriodCalculator.GetPeriodStart(sub.StartDate, sub.Plan.BillingCycle, periodIndex + 1);
                sub.UpdatedAt = now;
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, ct);
            }
            catch (DbUpdateException ex) when (SqlErrors.IsUniqueViolation(ex))
            {
                await tx.RollbackAsync(ct);
                await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, ct);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                await UpdateMessageWithBackoffAsync(message, ex, ct);
            }
        });
    }

    private async Task RecordSkippedAsync(BillingDbContext db, InvoiceJobMessage job, Subscription sub, CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        db.AuditLogs.Add(new AuditLog { EntityName = "InvoiceJob", EntityId = job.SubscriptionId.ToString(), Action = "InvoiceJobSkipped", NewValue = sub.Status.ToString(), PerformedBy = "System", CorrelationId = job.CorrelationId, OccurredAt = now });
        await db.SaveChangesAsync(ct);
    }


    private async Task MoveToPoisonQueueAsync(QueueMessage message, string reason, Exception? ex, CancellationToken ct)
    {
        var envelope = new PoisonEnvelope { SchemaVersion = 1, Reason = reason, DequeueCount = (int)message.DequeueCount, EncounteredAt = _timeProvider.GetUtcNow(), OriginalMessage = message.MessageText, MessageId = message.MessageId, QueueName = _queueOptions.InvoiceJobs };
        envelope.StackTrace = Truncate(ex?.ToString(), StackTraceMaxLength);
        try { await _poisonQueueClient.SendMessageAsync(envelope.ToJson(), cancellationToken: ct); }
        catch (Exception sendEx) { _logger.LogError(sendEx, "Failed to send {MessageId} to poison queue", message.MessageId); }
        await TryRecordDeadLetteredAsync(message, reason, ct);
        try { await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, ct); }
        catch (Exception deleteEx) { _logger.LogError(deleteEx, "Failed to delete {MessageId}", message.MessageId); }
    }

    private async Task TryRecordDeadLetteredAsync(QueueMessage message, string reason, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
            db.AuditLogs.Add(new AuditLog { EntityName = "InvoiceJob", EntityId = message.MessageId, Action = "InvoiceJobDeadLettered", NewValue = reason, PerformedBy = "System", CorrelationId = Guid.NewGuid(), OccurredAt = _timeProvider.GetUtcNow().UtcDateTime });
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex) { _logger.LogError(ex, "Failed to record dead-letter audit for {MessageId}", message.MessageId); }
    }

    private async Task UpdateMessageWithBackoffAsync(QueueMessage message, Exception ex, CancellationToken ct)
    {
        if (message.DequeueCount > _queueOptions.MaxDequeueCount)
        {
            await MoveToPoisonQueueAsync(message, "Retry exhausted", ex, ct);
            return;
        }
        var backoff = QueueRetryPolicy.GetBackoff(message.DequeueCount, Random.Shared);
        try
        {
            await _queueClient.UpdateMessageAsync(message.MessageId, message.PopReceipt, visibilityTimeout: backoff, cancellationToken: ct);
            _logger.LogWarning(ex, "Transient failure {MessageId}. Backoff {Backoff}.", message.MessageId, backoff);
        }
        catch (Exception updateEx) { _logger.LogError(updateEx, "Failed to update visibility for {MessageId}", message.MessageId); }
    }

    private static string DecodeMessageText(string messageText)
    {
        try
        {
            var bytes = Convert.FromBase64String(messageText);
            var decoded = Encoding.UTF8.GetString(bytes);
            if (decoded.Length > 0 && decoded.TrimStart()[0] == '{') { return decoded; }
        }
        catch (FormatException) { }
        return messageText;
    }

    private static bool IsValidJob(InvoiceJobMessage job, out string error)
    {
        if (job.SubscriptionId == Guid.Empty) { error = "SubscriptionId is empty"; return false; }
        if (string.IsNullOrWhiteSpace(job.IdempotencyKey)) { error = "IdempotencyKey is empty"; return false; }
        if (job.BillingPeriodStart > job.BillingPeriodEnd) { error = "BillingPeriodStart after BillingPeriodEnd"; return false; }
        error = string.Empty;
        return true;
    }

    private static bool HasSchemaMismatch(string json, out int schemaVersion)
    {
        schemaVersion = 1;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("SchemaVersion", out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var version))
            {
                schemaVersion = version;
                return version != 1;
            }
            return false;
        }
        catch (JsonException) { return false; }
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength) { return value; }
        return value.Substring(0, maxLength);
    }
}
#pragma warning restore CA1848
