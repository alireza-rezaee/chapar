using Chapar.API.Enums;

namespace Chapar.API.DTOs;

public record RecipientInfoDTO
{
    public string ExternalUserId { get; init; } = null!;
    public string? FullName { get; set; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }

    public string? GetDestinationByChannel(Channel channel)
    {
        return channel switch
        {
            Channel.SMS => PhoneNumber,
            Channel.VoiceCall => PhoneNumber,
            Channel.Email => Email,
            _ => null
        };
    }
}
