using Chapar.API.Models.IHas;

namespace Chapar.API.Models;

public sealed class TenantAPIKey
    : IHasId<Guid>
    , IHasCreatedAt
    , IHasUpdatedAt
    , IHasExpiresAt
    , IHasTenant
{
    public Guid Id { get; set; }
    public required string DisplayName { get; set; }
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public required string KeyPreview { get; set; }
    public required string KeyHash { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
