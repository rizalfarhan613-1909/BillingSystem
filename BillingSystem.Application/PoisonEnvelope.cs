using System.Text.Json;

namespace BillingSystem.Application;

public sealed class PoisonEnvelope
{
    public int SchemaVersion { get; set; } = 1;
    public string Reason { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public int DequeueCount { get; set; }
    public DateTimeOffset EncounteredAt { get; set; }
    public string OriginalMessage { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;
    public string QueueName { get; set; } = string.Empty;

    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = false
    };

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, _jsonOptions);
    }
}