using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Chapar.API.DTOs;
using Chapar.API.Enums;

namespace Chapar.API.ViewModels.OTPChallengeVMs;

public sealed record CreateOTPRequestVM
{
    [Required]
    public string TemplateName { get; set; } = null!;

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Channel Channels { get; init; }

    public int LifeTimeSeconds { get; set; }

    [Required]
    public required RecipientInfoDTO Recipient { get; set; }

    [Required]
    public required SenderInfoDTO Sender { get; set; } = null!;
}