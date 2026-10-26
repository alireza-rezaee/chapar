using Microsoft.EntityFrameworkCore.Query;
using Chapar.API.ViewModels;

namespace Chapar.API.Services.Repositories;

public interface IRepository<EntityT, IdT> where EntityT : class
{
    void Add(EntityT entity);

    Task ExecuteDeleteAsync(
        IdT id,
        CancellationToken cancellationToken = default);

    Task<EntityT> GetByIdAsync(
        IdT id,
        bool useCache = true,
        CancellationToken cancellationToken = default);

    Task<PagedItemsVM<EntityT>> ListAsync(
        PaginationParams? pagination = null,
        CancellationToken cancellationToken = default);

    Task ExecuteUpdateAsync(
        IdT id,
        DateTimeOffset updatedAt,
        Action<UpdateSettersBuilder<EntityT>> setPropertyCalls,
        CancellationToken cancellationToken = default);
}
