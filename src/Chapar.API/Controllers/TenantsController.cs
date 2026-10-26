using Microsoft.AspNetCore.Mvc;
using Chapar.API.Models;
using Chapar.API.Services.Repositories;
using Chapar.API.ViewModels;
using Chapar.API.ViewModels.TenantVMs;

namespace Chapar.API.Controllers;

[ApiController]
[Route("[controller]")]
public class TenantsController
(
    IRepository<Tenant, Guid> repository,
    IUnitOfWork unitOfWork
) : ControllerBase
{
    const string GetRoute = nameof(TenantsController) + nameof(GetAsync);

    [HttpGet]
    [ProducesResponseType(typeof(PagedItemsVM<Tenant>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListAsync(
        [FromRoute] Guid tenantId,
        [FromQuery] PaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await repository.ListAsync(pagination, cancellationToken);
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
    [ProducesResponseType(typeof(Tenant), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await repository.GetByIdAsync(id, cancellationToken: cancellationToken);
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
    public async Task<IActionResult> PostAsync(
        [FromBody] CreateEditTenantVM dto,
        CancellationToken cancellationToken = default)
    {
        Tenant entity = new()
        {
            Name = dto.Name,
            ChallengeLifetimeMinutes = dto.ChallengeLifetimeMinutes,
            ResendCooldownSeconds = dto.ResendCooldownSeconds,
            MaxVerificationAttempts = dto.MaxVerificationAttempts,
            MaxRequestsPerWindow = dto.MaxRequestsPerWindow,
            IssueWindowMinutes = dto.IssueWindowMinutes,
        };

        repository.Add(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CreatedAtRoute(GetRoute, new { id = entity.Id }, entity);
    }

    [HttpPut("{id:Guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PutAsync(
        Guid id,
        [FromBody] CreateEditTenantVM dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await repository.ExecuteUpdateAsync(
                id,
                dto.UpdatedAt,
                setter => setter
                    .SetProperty(x => x.ChallengeLifetimeMinutes, dto.ChallengeLifetimeMinutes)
                    .SetProperty(x => x.ResendCooldownSeconds, dto.ResendCooldownSeconds)
                    .SetProperty(x => x.MaxVerificationAttempts, dto.MaxVerificationAttempts)
                    .SetProperty(x => x.MaxRequestsPerWindow, dto.MaxRequestsPerWindow)
                    .SetProperty(x => x.IssueWindowMinutes, dto.IssueWindowMinutes),
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
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            await repository.ExecuteDeleteAsync(id, cancellationToken);
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
