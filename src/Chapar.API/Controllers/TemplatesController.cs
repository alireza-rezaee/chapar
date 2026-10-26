using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Chapar.API.Attributes;
using Chapar.API.Models;
using Chapar.API.Services.Repositories;
using Chapar.API.ViewModels;
using Chapar.API.ViewModels.TemplateVMs;

namespace Chapar.API.Controllers;

[ApiController]
[EnsureTenantAccessibility]
[Route("tenants/{tenantId:Guid}/[controller]")]
public class TemplatesController
(
    ITenantBasedRepository<Template, Guid> repository,
    IUnitOfWork unitOfWork
) : ControllerBase
{
    const string GetRoute = nameof(TemplatesController) + nameof(GetAsync);

    [HttpGet]
    [ProducesResponseType(typeof(PagedItemsVM<Template>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PagedItemsVM<Template>), StatusCodes.Status404NotFound)]
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
    [ProducesResponseType(typeof(Template), StatusCodes.Status200OK)]
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
    [ProducesResponseType(typeof(Tenant), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Tenant), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PostAsync(
        [FromRoute] Guid tenantId,
        [FromBody] CreateEditTemplateVM dto,
        CancellationToken cancellationToken = default)
    {
        Template entity = new()
        {
            TenantId = tenantId,
            Name = dto.Name,
            Channel = dto.Channel,
            Subject = dto.Subject,
            Body = dto.Body,
        };

        await repository.AddAsync(entity, tenantId, cancellationToken);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when
        (
            ex.InnerException is SqlException sqlEx
            && (sqlEx.Number == 2601 || sqlEx.Number == 2627)
        )
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = nameof(BadRequest),
                Detail = "Cannot create duplicate key."
            });
        }

        return CreatedAtRoute(GetRoute, new { id = entity.Id, tenantId }, entity);
    }

    [HttpPut("{id:Guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Tenant), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PutAsync(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        [FromBody] CreateEditTemplateVM dto,
        CancellationToken cancellationToken = default)
    {
        // TODO: handle db doublicate index exception (same template `Name`)
        try
        {
            await repository.ExecuteUpdateAsync(
                id,
                tenantId,
                dto.UpdatedAt,
                setter => setter
                    .SetProperty(x => x.Name, dto.Name)
                    .SetProperty(x => x.Channel, dto.Channel)
                    .SetProperty(x => x.Subject, dto.Subject)
                    .SetProperty(x => x.Body, dto.Body),
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
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
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
