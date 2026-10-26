using Chapar.API.DTOs;
using Chapar.API.Enums;
using Chapar.API.Options;

namespace Chapar.API.Services;

public interface IMessageChannelService
{
    Channel SupportedChannels { get; }

    Task<DeliveryResultDTO> SendMessageByAsync(
        Channel channel,
        SendMessageDTO message,
        SenderOptions config,
        CancellationToken cancellationToken = default);

    Task<DeliveryResultDTO> SendMessagesByAsync(
        Channel channel,
        IEnumerable<SendMessageDTO> messages,
        SenderOptions config,
        CancellationToken cancellationToken = default);
}
