using Chapar.API.Enums;
using Chapar.API.Models.IHas;

namespace Chapar.API.Models;

public sealed class Template
    : IHasId<Guid>
    , IHasCreatedAt
    , IHasUpdatedAt
    , IHasTenant
{
    public Guid Id { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public Guid TenantId { get; set; }
    public required string Name { get; set; }
    public Channel Channel { get; set; }
    public string? Subject { get; set; }
    public required string Body { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
