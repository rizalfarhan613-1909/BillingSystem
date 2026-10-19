using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using BillingSystem.Application;
using BillingSystem.Domain;
using BillingSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BillingSystem.Worker;

#pragma warning disable CA1848
public sealed class ScanRequestConsumer : BackgroundService
{
    private readonly QueueClient _queueClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScanRequestConsumer> _logger;
    private readonly QueueOptions _queueOptions;

    public ScanRequestConsumer(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        ILogger<ScanRequestConsumer> logger,
        IOptions<QueueOptions> queueOptions)
    {
        var connectionString = configuration["Storage:ConnectionString"]
            ?? throw new InvalidOperationException("Storage:ConnectionString is not configured.");
        _queueClient = new QueueClient(connectionString, queueOptions.Value.ScanRequests);
        _queueClient.CreateIfNotExists();
        _scopeFactory = scopeFactory;
        _logger = logger;
        _queueOptions = queueOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var response = await _queueClient.ReceiveMessagesAsync(
                maxMessages: 16,
                visibilityTimeout: TimeSpan.FromSeconds(_queueOptions.VisibilityTimeoutSeconds),
                cancellationToken: stoppingToken);

            if (response.Value.Length == 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
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
                    _logger.LogError(ex, "Error processing scan request message {MessageId}", message.MessageId);
                }
            }
        }
    }

    private async Task ProcessMessageAsync(QueueMessage message, CancellationToken ct)
    {
        ScanRequestMessage? scanRequest;
        try
        {
            var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(message.MessageText));
            scanRequest = JsonSerializer.Deserialize<ScanRequestMessage>(json);
        }
        catch
        {
            _logger.LogWarning("Malformed scan request message {MessageId}", message.MessageId);
            await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, ct);
            return;
        }

        if (scanRequest is null)
        {
            await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, ct);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

        var asOfDate = scanRequest.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var subscriptions = await dbContext.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.Status == SubscriptionStatus.Active && s.NextBillingDate <= asOfDate)
            .OrderBy(s => s.Id)
            .ToListAsync(ct);

        int enqueued = 0;
        foreach (var sub in subscriptions)
        {
            // BR-04: BillingPeriodStart harus sama dengan NextBillingDate
            if (sub.NextBillingDate != asOfDate)
            {
                continue; // stale, skip
            }

            var periodStart = sub.NextBillingDate;
            var periodEnd = BillingPeriodCalculator.GetPeriodEnd(
                sub.StartDate, sub.Plan.BillingCycle, 0).AddMonths(
                BillingPeriodCalculator.MonthsOf(sub.Plan.BillingCycle));

            // Hitung period index berdasarkan StartDate dan NextBillingDate
            var monthsDiff = (periodStart.Year - sub.StartDate.Year) * 12 + (periodStart.Month - sub.StartDate.Month);
            var periodIndex = monthsDiff / BillingPeriodCalculator.MonthsOf(sub.Plan.BillingCycle);
            periodStart = BillingPeriodCalculator.GetPeriodStart(sub.StartDate, sub.Plan.BillingCycle, periodIndex);
            periodEnd = BillingPeriodCalculator.GetPeriodEnd(sub.StartDate, sub.Plan.BillingCycle, periodIndex);

            var idempotencyKey = IdempotencyKeyFactory.Create(sub.Id, periodStart);

            var jobMessage = new InvoiceJobMessage(
                sub.Id,
                periodStart,
                periodEnd,
                idempotencyKey,
                scanRequest.CorrelationId);

            await _queueClient.SendMessageAsync(
                System.Text.Json.JsonSerializer.Serialize(jobMessage),
                cancellationToken: ct);
            enqueued++;
        }

        // AuditLog BillingRunScanned
        var auditLog = new AuditLog
        {
            EntityName = "BillingRun",
            EntityId = scanRequest.RunId.ToString(),
            Action = "BillingRunScanned",
            OldValue = null,
            NewValue = $"{{\"enqueuedCount\":{enqueued}}}",
            PerformedBy = "System",
            CorrelationId = scanRequest.CorrelationId,
            OccurredAt = DateTime.UtcNow
        };
        dbContext.AuditLogs.Add(auditLog);
        await dbContext.SaveChangesAsync(ct);

        await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, ct);

        _logger.LogInformation("Scan completed. RunId={RunId}, Enqueued={Enqueued}", scanRequest.RunId, enqueued);
    }
}
#pragma warning restore CA1848