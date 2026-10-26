using Chapar.API.Models.IHas;

namespace Chapar.API.Models;

public sealed class SMSConfig
    : IHasId<Guid>
    , IHasCreatedAt
    , IHasUpdatedAt
    , IHasTenant
{
    public Guid Id { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public Guid TenantId { get; set; }
    public required string Name { get; set; }
    public required string FromNumber { get; set; }
    public string? Username { get; set; } // TODO: use EncryptedUsername
    public string? Password { get; set; } // TODO: use EncryptedPassword
    public string? APIKey { get; set; } // TODO: use EncryptedAPIKey
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
