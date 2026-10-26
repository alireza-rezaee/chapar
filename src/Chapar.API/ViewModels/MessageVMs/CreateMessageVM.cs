using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Chapar.API.DTOs;
using Chapar.API.Enums;

namespace Chapar.API.ViewModels.MessageVMs;

public sealed record CreateMessageVM
{
    [Required]
    public string TemplateName { get; set; } = null!;

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Channel Channels { get; set; }
    
    [Required]
    public List<Dictionary<string, object>> Data { get; init; } = null!;
    
    [Required]
    public List<RecipientInfoDTO> Recipients { get; init; } = null!;

    [Required]
    public SenderInfoDTO Sender { get; set; } = null!;

    public DateTimeOffset? ToSendAtAfter { get; set; }
}