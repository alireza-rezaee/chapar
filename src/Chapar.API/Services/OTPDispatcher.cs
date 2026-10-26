using Chapar.API.DTOs;
using Chapar.API.Enums;
using Chapar.API.Models;
using Chapar.API.Options;
using Chapar.API.Services.Repositories;

namespace Chapar.API.Services;

public class OTPDispatcher
(
    IEnumerable<IOTPChannelService> services,
    ITemplateService templateService,
    OTPRepository repository,
    ILogger<OTPDispatcher> logger
) : IOTPDispatcher
{
    public async Task<DeliveryResultDTO> DispatchAsync(
        OTPChallenge request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Dispatching message-request {Id} for tenant {TenantId} to {Channel}",
            request.Id, request.TenantId, request.Channels);

        Channel[] requestedChannels = [.. Enum.GetValues<Channel>().Where(
            channel => channel != 0 && request.Channels.HasFlag(channel))];

        if (requestedChannels.Length == 0)
        {
            logger.LogWarning("No channel is selected.");
            return DeliveryResultDTO.Failure("No channel is selected.");
        }

        if (requestedChannels.Length != 1)
        {
            logger.LogWarning("Only one channel must be selected.");
            return DeliveryResultDTO.Failure("Only one channel must be selected.");
        }

        var targetService = requestedChannels
            .Select(x => services.FirstOrDefault(y => y.SupportedChannels.HasFlag(x)))
            .FirstOrDefault();

        if (targetService is null)
        {
            logger.LogWarning("No matching channels found for {Channels}", request.Channels);
            return DeliveryResultDTO.Failure($"No matching channels found for {request.Channels}");
        }

        DeliveryResultDTO result = await SendToChannelServiceAsync(
                request,
                channel: requestedChannels.First(),
                service: targetService,
                cancellationToken);

        return result;
    }

    private async Task<DeliveryResultDTO> SendToChannelServiceAsync(
        OTPChallenge message,
        Channel channel,
        IOTPChannelService service,
        CancellationToken cancellationToken = default)
    {
        RenderedTemplateDTO renderedMessage = await templateService.RenderAsync(
            message.TenantId,
            message.TemplateName,
            message.Channels,
            message.Data,
            cancellationToken);

        string destination = message.Recipient.GetDestinationByChannel(channel)
            ?? throw new InvalidOperationException(); // TODO:
        
        SendMessageDTO otpDTO = new()
        {
            Message = renderedMessage.Message,
            Destination = destination,
            Subject = renderedMessage.Subject,
            FullName = message.Recipient.FullName,
        };

        SenderOptions options = await repository.GetSenderOptionsForMessageAsync(
            message.Id, message.TenantId);

        DeliveryResultDTO result = await service.SendOTPByAsync(
            channel,
            otpDTO,
            options,
            cancellationToken);
        
        return result;
    }
}
