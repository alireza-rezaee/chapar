using Chapar.API.Enums;
using Chapar.API.Models.IHas;

namespace Chapar.API.Models;

public sealed class EmailConfig
    : IHasId<Guid>
    , IHasCreatedAt
    , IHasUpdatedAt
    , IHasTenant
{
    public Guid Id { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public Guid TenantId { get; set; }
    public required string Name { get; set; }
    public required string Host { get; set; }
    public int Port { get; set; }
    public required string Username { get; set; } // TODO: EncryptedUsername
    public required string Password { get; set; } // TODO: EncryptedPassword
    public required string FromAddress { get; set; }
    public required string FromName { get; set; }
    public SmtpSecureSocketOption SocketOption { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
