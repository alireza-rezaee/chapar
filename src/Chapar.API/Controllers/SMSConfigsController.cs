using Microsoft.AspNetCore.Mvc;
using Chapar.API.Attributes;
using Chapar.API.Models;
using Chapar.API.Services.Repositories;
using Chapar.API.ViewModels;
using Chapar.API.ViewModels.SMSConfigVMs;

namespace Chapar.API.Controllers;

[ApiController]
[EnsureTenantAccessibility]
[Route("tenants/{tenantId:Guid}/sms-configs")]
public class SMSConfigsController
(
    ITenantBasedRepository<SMSConfig, Guid> repository,
    IUnitOfWork unitOfWork
    // ILogger<OTPChallengesController> logger
) : ControllerBase
{
    const string GetRoute = nameof(SMSConfigsController) + nameof(GetAsync);

    [HttpGet]
    [ProducesResponseType(typeof(PagedItemsVM<SMSConfig>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListAsync(
        [FromRoute] Guid tenantId,
        [FromQuery] PaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await repository.ListAsync(tenantId, pagination, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = nameof(NotFound),
                Detail = ex.Message
            });
        }
    }

    [HttpGet("{id:Guid}", Name = GetRoute)]
    [ProducesResponseType(typeof(SMSConfig), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(
        [FromRoute] Guid id,
        [FromRoute] Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await repository.GetByIdAsync(id, tenantId, cancellationToken: cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = nameof(NotFound),
                Detail = ex.Message
            });
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(SMSConfig), StatusCodes.Status201Created)]
    public async Task<IActionResult> PostAsync(
        [FromBody] CreateEditSMSConfigVM dto,
        [FromRoute] Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        SMSConfig entity = new()
        {
            Name = dto.Name,
            FromNumber = dto.FromNumber,
            Username = dto.Username,
            Password = dto.Password,
            APIKey = dto.APIKey,
        };

        await repository.AddAsync(entity, tenantId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CreatedAtRoute(GetRoute, new { id = entity.Id, tenantId }, entity);
    }

    [HttpPut("{id:Guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PutAsync(
        [FromRoute] Guid id,
        [FromRoute] Guid tenantId,
        [FromBody] CreateEditSMSConfigVM dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await repository.ExecuteUpdateAsync(
                id,
                tenantId,
                dto.UpdatedAt,
                setter => setter
                    .SetProperty(x => x.FromNumber, dto.FromNumber)
                    .SetProperty(x => x.Username, dto.Username)
                    .SetProperty(x => x.Password, dto.Password)
                    .SetProperty(x => x.APIKey, dto.APIKey),
                cancellationToken);

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = nameof(NotFound),
                Detail = ex.Message
            });
        }
    }

    [HttpDelete("{id:Guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] Guid id,
        [FromRoute] Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await repository.ExecuteDeleteAsync(id, tenantId, cancellationToken);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = nameof(NotFound),
                Detail = ex.Message
            });
        }

        return NoContent();
    }
}
