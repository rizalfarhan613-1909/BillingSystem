using Azure.Storage.Queues;
using BillingSystem.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BillingSystem.Infrastructure.Queue;

public sealed class AzureQueuePublisher : IQueuePublisher
{
    private readonly QueueServiceClient _serviceClient;
    private readonly QueueOptions _queueOptions;

    public AzureQueuePublisher(
        IConfiguration configuration,
        IOptions<QueueOptions> queueOptions)
    {
        var connectionString = configuration["Storage:ConnectionString"]
            ?? throw new InvalidOperationException("Storage:ConnectionString is not configured.");

        _serviceClient = new QueueServiceClient(connectionString);
        _queueOptions = queueOptions.Value;
    }

#pragma warning disable CA2016
    public async Task PublishAsync(string queueName, object message, CancellationToken ct = default)
    {
        var queueClient = _serviceClient.GetQueueClient(queueName);
        await queueClient.CreateIfNotExistsAsync();

        var json = JsonSerializer.Serialize(message);
        var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
        await queueClient.SendMessageAsync(base64, cancellationToken: ct);
    }
#pragma warning restore CA2016
}