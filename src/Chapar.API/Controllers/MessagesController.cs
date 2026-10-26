using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Chapar.API.Attributes;
using Chapar.API.Models;
using Chapar.API.Services.Repositories;
using Chapar.API.ViewModels;
using Chapar.API.ViewModels.MessageVMs;

namespace Chapar.API.Controllers;

[ApiController]
[EnsureTenantAccessibility]
[Route("tenants/{tenantId:Guid}/[controller]")]
public class MessagesController
(
    IPublishEndpoint publishEndpoint,
    ITenantBasedRepository<MessageRequest, Guid> repository,
    IUnitOfWork unitOfWork
) : ControllerBase
{
    const string GetRoute = nameof(MessagesController) + nameof(GetAsync);

    [HttpGet]
    [ProducesResponseType(typeof(PagedItemsVM<MessageRequest>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PagedItemsVM<MessageRequest>), StatusCodes.Status404NotFound)]
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
    [ProducesResponseType(typeof(MessageRequest), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
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
    [ProducesResponseType(typeof(MessageRequest), StatusCodes.Status201Created)]
    public async Task<IActionResult> PostAsync(
        [FromRoute] Guid tenantId,
        [FromBody] CreateMessageVM dto,
        CancellationToken cancellationToken = default)
    {
        MessageRequest entity = new()
        {
            TenantId = tenantId,
            TemplateName = dto.TemplateName,
            Channels = dto.Channels,
            Data = dto.Data,
            Recipients = dto.Recipients,
            ToSendAtAfter = dto.ToSendAtAfter,
            SenderEmailId = dto.Sender.EmailId,
            SenderSMSId = dto.Sender.SMSId,
        };

        await repository.AddAsync(entity, tenantId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await publishEndpoint.Publish(entity, cancellationToken);
        
        return CreatedAtRoute(
            GetRoute,
            new { tenantId, id = entity.Id },
            entity
        );
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
