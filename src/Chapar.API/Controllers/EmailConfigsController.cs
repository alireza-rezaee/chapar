using Microsoft.AspNetCore.Mvc;
using Chapar.API.Attributes;
using Chapar.API.Models;
using Chapar.API.Services.Repositories;
using Chapar.API.ViewModels;
using Chapar.API.ViewModels.EmailConfigVMs;

namespace Chapar.API.Controllers;

[ApiController]
[EnsureTenantAccessibility]
[Route("tenants/{tenantId:Guid}/email-configs")]
public class EmailConfigsController
(
    ITenantBasedRepository<EmailConfig, Guid> repository,
    IUnitOfWork unitOfWork
    // ILogger<OTPChallengesController> logger
) : ControllerBase
{
    const string GetRoute = nameof(EmailConfigsController) + nameof(GetAsync);

    [HttpGet]
    [ProducesResponseType(typeof(PagedItemsVM<EmailConfig>), StatusCodes.Status200OK)]
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
    [ProducesResponseType(typeof(EmailConfig), StatusCodes.Status200OK)]
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
    [ProducesResponseType(typeof(EmailConfig), StatusCodes.Status201Created)]
    public async Task<IActionResult> PostAsync(
        [FromRoute] Guid tenantId,
        [FromBody] CreateEditEmailConfigVM dto,
        CancellationToken cancellationToken = default)
    {
        EmailConfig entity = new()
        {
            Name = dto.Name,
            Host = dto.Host,
            Port = dto.Port,
            Username = dto.Username,
            Password = dto.Password,
            FromAddress = dto.FromAddress,
            FromName = dto.FromName,
            SocketOption = dto.SocketOption,
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
        [FromBody] CreateEditEmailConfigVM dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await repository.ExecuteUpdateAsync(
                id,
                tenantId,
                dto.UpdatedAt,
                setter => setter
                    .SetProperty(x => x.Name, dto.Name)
                    .SetProperty(x => x.Host, dto.Host)
                    .SetProperty(x => x.Port, dto.Port)
                    .SetProperty(x => x.Username, dto.Username)
                    .SetProperty(x => x.Password, dto.Password)
                    .SetProperty(x => x.FromAddress, dto.FromAddress)
                    .SetProperty(x => x.FromName, dto.FromName)
                    .SetProperty(x => x.SocketOption, dto.SocketOption),
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
