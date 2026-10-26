using Chapar.API.DTOs;
using Chapar.API.Models;

namespace Chapar.API.Services;

public interface IMessageDispatcher
{
    Task<DeliveryResultDTO> DispatchAsync(
        MessageRequest request,
        CancellationToken cancellationToken = default);    
}
