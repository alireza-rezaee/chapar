using Chapar.API.DTOs;
using Chapar.API.Options;

namespace Chapar.API.Services;

public interface ISMSService
    : IMessageChannelService
{
    public Task<DeliveryResultDTO> SendAsync(
        string[] toNumbers,
        string text,
        SMSSenderOptions config,
        CancellationToken cancellationToken = default);

    public Task<DeliveryResultDTO> SendMultipleAsync(
        IEnumerable<DestinationText> recipients,
        SMSSenderOptions config,
        CancellationToken cancellationToken = default);
}
