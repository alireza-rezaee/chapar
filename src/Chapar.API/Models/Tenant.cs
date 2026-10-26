using Chapar.API.Models.IHas;

namespace Chapar.API.Models;

public sealed class Tenant
    : IHasId<Guid>
    , IHasCreatedAt
    , IHasUpdatedAt
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public int ChallengeLifetimeMinutes { get; set; }
    public int ResendCooldownSeconds { get; set; }
    public int MaxVerificationAttempts { get; set; }
    public int MaxRequestsPerWindow { get; set; }
    public int IssueWindowMinutes { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
