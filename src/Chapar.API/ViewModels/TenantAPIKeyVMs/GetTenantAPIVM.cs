namespace Chapar.API.ViewModels.TenantAPIKeyVMs;

public sealed record GetTenantAPIVM
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string DisplayName { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
