using MassTransit;
using Chapar.API.DTOs;
using Chapar.API.Models;
using Chapar.API.Services.Repositories;

namespace Chapar.API.Services.Queues;

public class MessageRequestedFaultConsumer
(
    ITenantBasedRepository<MessageRequest, Guid> repository
    // ILogger<MessageRequestedConsumer> logger // TODO
) : IConsumer<Fault<MessageRequest>>
{
    public async Task Consume(ConsumeContext<Fault<MessageRequest>> context)
    {
        MessageRequest message = context.Message.Message;
        ExceptionInfo[] exceptions = context.Message.Exceptions;

        await repository.ExecuteUpdateAsync(
            id: message.Id,
            tenantId: message.TenantId,
            updatedAt: message.UpdatedAt,
            setPropertyCalls: setter => setter
                .SetProperty(x => x.DeliveryResult, DeliveryResultDTO.Failure(exceptions[0].Message)),
            cancellationToken: context.CancellationToken);
    }
}
