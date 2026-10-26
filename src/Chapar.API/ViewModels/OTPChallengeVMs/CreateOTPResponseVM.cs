using System.ComponentModel.DataAnnotations;
using Chapar.API.Enums;

namespace Chapar.API.ViewModels.OTPChallengeVMs;

public sealed record CreateOTPResponseVM
{
    public bool IsVerified { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
