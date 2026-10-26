using MassTransit;
using Chapar.API.DTOs;
using Chapar.API.Models;
using Chapar.API.Services.Repositories;

namespace Chapar.API.Services.Queues;

public class OTPRequestedConsumer
(
    IOTPDispatcher dispatcher,
    ITenantBasedRepository<OTPChallenge, Guid> repository
    // ILogger<OTPRequestedConsumer> logger // TODO
) : IConsumer<OTPChallenge>
{
    public async Task Consume(ConsumeContext<OTPChallenge> context)
    {
        OTPChallenge message = context.Message;

        await dispatcher.DispatchAsync(message, context.CancellationToken);

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
