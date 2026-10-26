using MassTransit;
using Chapar.API.DTOs;
using Chapar.API.Models;
using Chapar.API.Services.Repositories;

namespace Chapar.API.Services.Queues;

public class MessageRequestedConsumer
(
    IMessageDispatcher dispatcher,
    ITenantBasedRepository<MessageRequest, Guid> repository
    //ILogger<MessageRequestedConsumer> logger // TODO
) : IConsumer<MessageRequest>
{
    public async Task Consume(ConsumeContext<MessageRequest> context)
    {
        MessageRequest message = context.Message;

        DeliveryResultDTO result = await dispatcher.DispatchAsync(
            message,
            context.CancellationToken);

        await repository.ExecuteUpdateAsync(
            id: message.Id,
            tenantId: message.TenantId,
            updatedAt: message.UpdatedAt,
            setPropertyCalls: setter => setter
                .SetProperty(x => x.DeliveryResult, result),
            cancellationToken: context.CancellationToken);
    }
}
