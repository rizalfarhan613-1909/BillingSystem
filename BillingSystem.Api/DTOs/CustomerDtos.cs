namespace BillingSystem.Api.DTOs;

public record CreateCustomerRequest(
    string FullName,
    string Email,
    string? CompanyName,
    string? BillingAddress
);

public record UpdateCustomerRequest(
    string FullName,
    string Email,
    string? CompanyName,
    string? BillingAddress,
    bool IsActive
);

public record CustomerResponse(
    Guid Id,
    string FullName,
    string Email,
    string? CompanyName,
    string? BillingAddress,
    bool IsActive,
    DateTime CreatedAt
);