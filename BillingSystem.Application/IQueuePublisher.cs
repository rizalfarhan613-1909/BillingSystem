using System.Text.Json;

namespace BillingSystem.Application;

public interface IQueuePublisher
{
    Task PublishAsync(string queueName, object message, CancellationToken ct = default);
}