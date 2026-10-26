using Chapar.API.DTOs;
using Chapar.API.Enums;
using Chapar.API.Models;
using Chapar.API.Options;
using Chapar.API.Services.Repositories;

namespace Chapar.API.Services;

public class MessageDispatcher
(
    IEnumerable<IMessageChannelService> services,
    ITemplateService templateService,
    MessageRepository repository,
    ILogger<MessageDispatcher> logger
) : IMessageDispatcher
{
    public async Task<DeliveryResultDTO> DispatchAsync(
        MessageRequest message,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Dispatching message-request {Id} for tenant {TenantId} to {Channels}",
            message.Id, message.TenantId, message.Channels);

        Channel[] requestedChannels = [.. Enum.GetValues<Channel>().Where(
            channel => channel != 0 && message.Channels.HasFlag(channel))];

        if (requestedChannels.Length == 0)
        {
            logger.LogWarning("No channel is selected.");
            return DeliveryResultDTO.Failure("No channel is selected.");
        }

        var targetServices = requestedChannels
            .Select(
                x => (
                    Channel: x,
                    Service: services.FirstOrDefault(
                        y => y.SupportedChannels.HasFlag(x))))
            .Where(x => x.Service != null).ToArray();

        if (targetServices.Length == 0)
        {
            logger.LogWarning("No matching channels found for {Channels}", message.Channels);
            return DeliveryResultDTO.Failure($"No matching channels found for {message.Channels}");
        }

        var tasks = targetServices.Select(
            x => SendToChannelServiceAsync(
                message,
                x.Channel,
                x.Service!,
                cancellationToken));

        DeliveryResultDTO[] results = await Task.WhenAll(tasks);

        return DeliveryResultDTO.Merge(results);
    }

    private async Task<DeliveryResultDTO> SendToChannelServiceAsync(
        MessageRequest message,
        Channel channel,
        IMessageChannelService service,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<RenderedTemplateDTO> renderedMessages = await templateService.RenderAsync(
            message.TenantId,
            message.TemplateName,
            message.Channels,
            message.Data,
            cancellationToken);

        IEnumerable<string> destinations = message.Recipients
            .Select(x => x.GetDestinationByChannel(channel))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>();

        if (message.Data.Count == 1 && message.Recipients.Count > 1)
        {
            renderedMessages = Enumerable.Repeat(
                renderedMessages.First(),
                message.Recipients.Count);
        }

        IEnumerable<SendMessageDTO> messages = destinations
            .Zip(renderedMessages)
            .Select(x => new SendMessageDTO()
            {
                Destination = x.First,
                Message = x.Second.Message,
                Subject = x.Second.Subject,
            });

        SenderOptions options = await repository.GetSenderOptionsForMessageAsync(
            message.Id, message.TenantId);

        DeliveryResultDTO result = await service.SendMessagesByAsync(
            channel,
            messages,
            options,
            cancellationToken);

        return result;
    }
}
