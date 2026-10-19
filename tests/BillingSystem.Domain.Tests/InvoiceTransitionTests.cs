using BillingSystem.Domain;

namespace BillingSystem.Domain.Tests;

public sealed class InvoiceTransitionTests
{
    private static Invoice CreateInvoice(InvoiceStatus status)
    {
        return new Invoice(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "INV-202601-000001",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            100000m,
            "IDR",
            status,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 15),
            "INV:key:20260101",
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Theory]
    [InlineData(InvoiceStatus.Pending, InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Pending, InvoiceStatus.Overdue)]
    [InlineData(InvoiceStatus.Pending, InvoiceStatus.Failed)]
    [InlineData(InvoiceStatus.Overdue, InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Overdue, InvoiceStatus.Failed)]
    [InlineData(InvoiceStatus.Failed, InvoiceStatus.Pending)]
    public void TransitionTo_ValidMatrix_Succeeds(InvoiceStatus from, InvoiceStatus to)
    {
        var invoice = CreateInvoice(from);
        var now = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var reason = to == InvoiceStatus.Failed ? "Payment gateway error" : null;

        invoice.TransitionTo(to, now, reason);

        Assert.Equal(to, invoice.Status);
    }

    [Fact]
    public void TransitionTo_PendingToFailed_RequiresReason()
    {
        var invoice = CreateInvoice(InvoiceStatus.Pending);
        var now = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);

        Assert.Throws<ArgumentException>(() => invoice.TransitionTo(InvoiceStatus.Failed, now));
    }

    [Theory]
    [InlineData(InvoiceStatus.Pending, InvoiceStatus.Pending)]
    [InlineData(InvoiceStatus.Paid, InvoiceStatus.Pending)]
    [InlineData(InvoiceStatus.Paid, InvoiceStatus.Overdue)]
    [InlineData(InvoiceStatus.Paid, InvoiceStatus.Failed)]
    [InlineData(InvoiceStatus.Paid, InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Overdue, InvoiceStatus.Pending)]
    [InlineData(InvoiceStatus.Overdue, InvoiceStatus.Overdue)]
    [InlineData(InvoiceStatus.Failed, InvoiceStatus.Paid)]
    [InlineData(InvoiceStatus.Failed, InvoiceStatus.Overdue)]
    [InlineData(InvoiceStatus.Failed, InvoiceStatus.Failed)]
    public void TransitionTo_InvalidMatrix_Throws(InvoiceStatus from, InvoiceStatus to)
    {
        var invoice = CreateInvoice(from);
        var now = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);

        Assert.Throws<InvalidStatusTransitionException>(() => invoice.TransitionTo(to, now, "reason"));
    }

    [Fact]
    public void TransitionTo_FromPaid_AlwaysThrows()
    {
        var invoice = CreateInvoice(InvoiceStatus.Paid);
        var now = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);

        Assert.Throws<InvalidStatusTransitionException>(() => invoice.TransitionTo(InvoiceStatus.Pending, now));
    }
}
