using Chapar.API.Models.IHas;

namespace Chapar.API.ViewModels.TenantAPIKeyVMs;

public sealed record CreateResultTenantAPIVM
    : IHasCreatedAt
    , IHasExpiresAt
    , IHasUpdatedAt
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string DisplayName { get; set; }
    public required string PlainApiKey { get; set; }
    public required string KeyPreview { get; set; }
    public required string KeyHash { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
