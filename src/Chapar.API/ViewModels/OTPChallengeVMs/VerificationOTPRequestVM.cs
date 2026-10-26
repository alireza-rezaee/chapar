using System.ComponentModel.DataAnnotations;

namespace Chapar.API.ViewModels.OTPChallengeVMs;

public sealed record VerificationOTPRequestVM
{
    [Required, RegularExpression(@"^[0-9]{4,10}$")]
    public required string Code { get; init; }
}
