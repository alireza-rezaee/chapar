using Chapar.API.DTOs;
using Chapar.API.Enums;
using Chapar.API.Options;

namespace Chapar.API.Services;

public interface IOTPChannelService
{
    Channel SupportedChannels { get; }

    Task<DeliveryResultDTO> SendOTPByAsync(
        Channel channel,
        SendMessageDTO request,
        SenderOptions config,
        CancellationToken cancellationToken = default);
}
