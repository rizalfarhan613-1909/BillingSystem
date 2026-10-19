using BillingSystem.Domain;

namespace BillingSystem.Api.DTOs;

public record DashboardSummaryDto(
    int TotalActiveSubscriptions,
    decimal TotalRevenueThisMonth,
    int TotalOverdueInvoices);

public record RecentAuditLogDto(
    long Id,
    string EntityName,
    string EntityId,
    string Action,
    string? OldValue,
    string? NewValue,
    string PerformedBy,
    Guid CorrelationId,
    DateTime OccurredAt);

public record RecentInvoiceDto(
    Guid Id,
    string InvoiceNumber,
    Guid CustomerId,
    Guid SubscriptionId,
    InvoiceStatus Status,
    decimal Amount,
    string Currency,
    DateOnly IssueDate,
    DateOnly DueDate,
    DateTime CreatedAt);

public record DashboardRecentTransactionsDto(
    IReadOnlyList<RecentAuditLogDto> RecentAuditLogs,
    IReadOnlyList<RecentInvoiceDto> RecentInvoices);