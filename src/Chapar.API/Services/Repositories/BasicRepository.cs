using EFCoreSecondLevelCacheInterceptor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Chapar.API.Data;
using Chapar.API.Models.IHas;
using Chapar.API.ViewModels;

namespace Chapar.API.Services.Repositories;

public class BasicRepository<EntityT, IdT>
(
    ApplicationDbContext dbContext
    // ILogger<BasicRepository<EntityT, IdT>> logger
) : IRepository<EntityT, IdT>
    where EntityT : class, IHasId<IdT>, IHasUpdatedAt, IHasCreatedAt
    where IdT : IEquatable<IdT>
{
    protected readonly ApplicationDbContext _dbContext = dbContext;

    protected DbSet<EntityT> EntitySet => _dbContext.Set<EntityT>();

    public void Add(EntityT entity) => EntitySet.Add(entity);

    public async Task ExecuteDeleteAsync(
        IdT id,
        CancellationToken cancellationToken = default)
    {
        int affectedRows = await EntitySet
            .Where(x=> x.Id.Equals(id))
            .ExecuteDeleteAsync(cancellationToken);

        if (affectedRows == 0)
            throw IdNotFoundException(id);
    }

    public async Task ExecuteUpdateAsync(
        IdT id,
        DateTimeOffset updatedAt,
        Action<UpdateSettersBuilder<EntityT>> setPropertyCalls, CancellationToken cancellationToken = default)
    {
        int affectedRows = await EntitySet
            .Where(x => x.Id.Equals(id) && x.UpdatedAt == updatedAt)
            .ExecuteUpdateAsync(setPropertyCalls => setPropertyCalls
                .SetProperty(e => e.UpdatedAt, DateTimeOffset.UtcNow),
                cancellationToken
            );
        
        if (affectedRows == 0)
            throw IdNotFoundOrConcurrentException(id);
    }

    public async Task<EntityT> GetByIdAsync(
        IdT id,
        bool useCache = true,
        CancellationToken cancellationToken = default)
    {
        IQueryable<EntityT> query = EntitySet.AsNoTracking();

        if (useCache)
            query = query.Cacheable(CacheExpirationMode.Sliding, TimeSpan.FromMinutes(15));
        
        EntityT? entity = await query
            .FirstOrDefaultAsync(x => x.Id.Equals(id), cancellationToken)
            ?? throw IdNotFoundException(id);

        return entity;
    }

    public async Task<PagedItemsVM<EntityT>> ListAsync(
        PaginationParams? pagination = null,
        CancellationToken cancellationToken = default)
    {
        pagination ??= new();
        IQueryable<EntityT> query = EntitySet.AsNoTracking();

        int totalCount = await query
            .Cacheable(CacheExpirationMode.Absolute, TimeSpan.FromMinutes(10))
            .CountAsync(cancellationToken);

        int totalPages = totalCount == 0
            ? 1 
            : (int)Math.Ceiling((double)totalCount / pagination.PageSize);

        if (pagination.PageIndex > totalPages)
            throw new KeyNotFoundException(
                $"Requested page index '{pagination.PageIndex}' is out of range. Total available pages: {totalPages}.");

        List<EntityT> items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pagination.PageIndex - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Cacheable(CacheExpirationMode.Absolute, TimeSpan.FromMinutes(10))
            .ToListAsync(cancellationToken);
        
        return new PagedItemsVM<EntityT>(totalCount, items);
    }

    protected static KeyNotFoundException IdNotFoundException(IdT id)
        => new($"{typeof(EntityT).Name} with ID '{id}' was not found.");

    protected static KeyNotFoundException IdNotFoundOrConcurrentException(IdT id)
        => new($"{typeof(EntityT).Name} with ID '{id}' was not found or may have been deleted/modified concurrently.");
}
