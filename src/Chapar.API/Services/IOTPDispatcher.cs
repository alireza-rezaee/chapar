using Chapar.API.DTOs;
using Chapar.API.Models;

namespace Chapar.API.Services;

public interface IOTPDispatcher
{
    Task<DeliveryResultDTO> DispatchAsync(
        OTPChallenge request,
        CancellationToken cancellationToken = default);    
}
