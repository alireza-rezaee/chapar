using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Chapar.API.Attributes;
using Chapar.API.DTOs;
using Chapar.API.Models;
using Chapar.API.Services.Repositories;
using Chapar.API.ViewModels;
using Chapar.API.ViewModels.TenantAPIKeyVMs;

namespace Chapar.API.Controllers;

[ApiController]
[EnsureTenantAccessibility]
[Route("tenants/{tenantId:Guid}/api-keys")]
public class TenantAPIKeysController
(
    ITenantBasedRepository<TenantAPIKey, Guid> repository,
    IUnitOfWork unitOfWork
) : ControllerBase
{
    const string GetRoute = nameof(TenantAPIKeysController) + nameof(GetAsync);

    [HttpGet]
    [ProducesResponseType(typeof(PagedItemsVM<GetTenantAPIVM>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListAsync(
        [FromRoute] Guid tenantId,
        [FromQuery] PaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await repository.ListAsync(tenantId, pagination, cancellationToken);
            return Ok(ConvertToGetVM(result));
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
    [ProducesResponseType(typeof(GetTenantAPIVM), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await repository.GetByIdAsync(id, tenantId, cancellationToken: cancellationToken);
            return Ok(ConvertToGetVM(result));
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
    [ProducesResponseType(typeof(CreateResultTenantAPIVM), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PostAsync(
        [FromRoute] Guid tenantId,
        [FromBody] CreateTenantAPIVM dto,
        CancellationToken cancellationToken = default)
    {
        ApiKeyResultDTO newApiKey = GenerateApiKey();

        TenantAPIKey entity = new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DisplayName = dto.DisplayName,
            KeyPreview = newApiKey.KeyPreview,
            KeyHash = newApiKey.KeyHash,
            ExpiresAt = dto.ExpiresAt,
        };

        await repository.AddAsync(entity, tenantId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        CreateResultTenantAPIVM response = new()
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            DisplayName = entity.DisplayName,
            KeyHash = newApiKey.KeyHash,
            KeyPreview = newApiKey.KeyPreview,
            PlainApiKey = newApiKey.PlainApiKey,
            ExpiresAt = entity.ExpiresAt,
            UpdatedAt = entity.UpdatedAt,
            CreatedAt = entity.CreatedAt
        };

        return CreatedAtRoute(
            GetRoute,
            new { id = entity.Id, tenantId },
            response
        );
    }

    [HttpPut("{id:Guid}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegenerateAsync(
        [FromRoute] Guid tenantId,
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await repository.GetByIdAsync(
                id: id,
                tenantId: tenantId,
                useCache: false,
                cancellationToken: cancellationToken);

            if (entity.ExpiresAt <= DateTimeOffset.UtcNow)
                return Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = nameof(NotFound),
                    Detail = $"API key with ID '{id}' is already expired or revoked."
                });

            ApiKeyResultDTO newApiKey = GenerateApiKey();

            await repository.ExecuteUpdateAsync(
                id: id,
                tenantId: tenantId,
                updatedAt: 
                entity.UpdatedAt,
                setPropertyCalls: setter => setter
                    .SetProperty(x => x.KeyHash, newApiKey.KeyHash)
                    .SetProperty(x => x.KeyPreview, newApiKey.KeyPreview),
                cancellationToken: cancellationToken);

            CreateResultTenantAPIVM response = new()
            {
                Id = entity.Id,
                TenantId = entity.TenantId,
                DisplayName = entity.DisplayName,
                KeyHash = newApiKey.KeyHash,
                KeyPreview = newApiKey.KeyPreview,
                PlainApiKey = newApiKey.PlainApiKey,
                ExpiresAt = entity.ExpiresAt,
                UpdatedAt = entity.UpdatedAt,
                CreatedAt = entity.CreatedAt
            };

            return CreatedAtRoute(
                GetRoute,
                new { tenantId, id = entity.Id },
                response
            );
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

    public ApiKeyResultDTO GenerateApiKey()
    {
        byte[] randomBytes = RandomNumberGenerator.GetBytes(32);

        string secret = Convert.ToHexString(randomBytes).ToLowerInvariant();

        string prefix = $"chapar_";
        string plainApiKey = $"{prefix}{secret}";

        byte[] keyBytes = Encoding.UTF8.GetBytes(plainApiKey);
        byte[] hashBytes = SHA256.HashData(keyBytes);
        string keyHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        string keyPreview = $"{prefix}{secret[..4]}...{secret[^4..]}";

        return new ApiKeyResultDTO
        (
            PlainApiKey: plainApiKey,
            KeyHash: keyHash,
            KeyPreview: keyPreview
        );
    }

    private static GetTenantAPIVM ConvertToGetVM(TenantAPIKey paged)
    {
        return new GetTenantAPIVM()
        {
            Id = paged.Id,
            TenantId = paged.TenantId,
            DisplayName = paged.DisplayName,
            ExpiresAt = paged.ExpiresAt,
            UpdatedAt = paged.UpdatedAt,
            CreatedAt = paged.CreatedAt,
        };
    }

    private static PagedItemsVM<GetTenantAPIVM> ConvertToGetVM(PagedItemsVM<TenantAPIKey> paged)
        => new(paged.TotalCount, paged.Items.Select(ConvertToGetVM));
}
