using Chapar.API.Enums;

namespace Chapar.API.DTOs;

public record SenderInfoDTO
{
    public Guid? EmailId { get; init; }
    public Guid? SMSId { get; init; }
}
