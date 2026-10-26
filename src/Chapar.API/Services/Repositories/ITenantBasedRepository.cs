using Microsoft.EntityFrameworkCore.Query;
using Chapar.API.ViewModels;

namespace Chapar.API.Services.Repositories;

public interface ITenantBasedRepository<EntityT, IdT> where EntityT : class
{
    Task AddAsync(
        EntityT entity,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task ExecuteDeleteAsync(
        IdT id,
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<EntityT> GetByIdAsync(
        IdT id,
        Guid tenantId,
        bool useCache = true,
        CancellationToken cancellationToken = default);

    Task<PagedItemsVM<EntityT>> ListAsync(
        Guid tenantId,
        PaginationParams? pagination = null,
        CancellationToken cancellationToken = default);

    Task ExecuteUpdateAsync(
        IdT id,
        Guid tenantId,
        DateTimeOffset updatedAt,
        Action<UpdateSettersBuilder<EntityT>> setPropertyCalls,
        CancellationToken cancellationToken = default);
}
