using System.ComponentModel.DataAnnotations;
using Chapar.API.Models.IHas;

namespace Chapar.API.ViewModels.TenantVMs;

public record CreateEditTenantVM
    : IHasUpdatedAt
{
    [Required(ErrorMessage = "The Name field is required.")]
    [MaxLength(100)]
    public required string Name { get; init; }

    [Range(1, 1440)]
    public int ChallengeLifetimeMinutes { get; init; } = 5;

    [Range(0, 3600)]
    public int ResendCooldownSeconds { get; init; } = 60;

    [Range(1, 10)]
    public int MaxVerificationAttempts { get; init; } = 5;

    [Range(1, 1000)]
    public int MaxRequestsPerWindow { get; init; } = 5;

    [Range(1, 1440)]
    public int IssueWindowMinutes { get; init; } = 15;
    
    public DateTimeOffset UpdatedAt { get; set; }
}
