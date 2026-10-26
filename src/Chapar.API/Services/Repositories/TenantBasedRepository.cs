using EFCoreSecondLevelCacheInterceptor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Chapar.API.Data;
using Chapar.API.Models;
using Chapar.API.Models.IHas;
using Chapar.API.ViewModels;

namespace Chapar.API.Services.Repositories;

public class TenantBasedRepository<EntityT, IdT>
(
    ApplicationDbContext dbContext
// ILogger<BasicRepository<EntityT, IdT>> logger
) : ITenantBasedRepository<EntityT, IdT>
    where EntityT : class, IHasId<IdT>, IHasUpdatedAt, IHasCreatedAt, IHasTenant
    where IdT : IEquatable<IdT>
{
    protected readonly ApplicationDbContext _dbContext = dbContext;

    protected DbSet<Tenant> TenantSet => _dbContext.Set<Tenant>();
    protected DbSet<EntityT> EntitySet => _dbContext.Set<EntityT>();

    public async Task AddAsync(
        EntityT entity,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        entity.TenantId = tenantId;
        
        bool tenantExists = await TenantSet
            .AsNoTracking()
            .AnyAsync(c => c.Id.Equals(tenantId), cancellationToken);

        if (!tenantExists)
            throw TenantNotFoundException(tenantId);

        EntitySet.Add(entity);
    }

    public async Task ExecuteDeleteAsync(
        IdT id,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        int affectedRows = await EntitySet
            .Where(x => x.Id.Equals(id) && x.TenantId.Equals(tenantId))
            .ExecuteDeleteAsync(cancellationToken);

        if (affectedRows == 0)
            throw IdNotFoundException(id);
    }

    public async Task ExecuteUpdateAsync(
        IdT id,
        Guid tenantId,
        DateTimeOffset updatedAt,
        Action<UpdateSettersBuilder<EntityT>> setPropertyCalls,
        CancellationToken cancellationToken = default)
    {
        int affectedRows = await EntitySet
            .Where(x =>
                x.Id.Equals(id)
                && x.TenantId.Equals(tenantId)
                && x.UpdatedAt == updatedAt)
            .ExecuteUpdateAsync(setPropertyCalls => setPropertyCalls
                .SetProperty(e => e.UpdatedAt, DateTimeOffset.UtcNow),
                cancellationToken
            );

        if (affectedRows == 0)
            throw IdNotFoundOrConcurrentException(id);
    }

    public async Task<EntityT> GetByIdAsync(
        IdT id,
        Guid tenantId,
        bool useCache = true,
        CancellationToken cancellationToken = default)
    {
        IQueryable<EntityT> query = EntitySet.AsNoTracking();

        if (useCache)
            query = query.Cacheable(CacheExpirationMode.Sliding, TimeSpan.FromMinutes(15));

        EntityT? entity = await query
            .FirstOrDefaultAsync(
                x => x.Id.Equals(id) && x.TenantId.Equals(tenantId)
                , cancellationToken)
            ?? throw IdNotFoundException(id);

        return entity;
    }

    public async Task<PagedItemsVM<EntityT>> ListAsync(
        Guid tenantId,
        PaginationParams? pagination = null,
        CancellationToken cancellationToken = default)
    {
        pagination ??= new();
        
        IQueryable<EntityT> query = EntitySet.AsNoTracking()
            .Where(x => x.TenantId.Equals(tenantId));

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

    protected static KeyNotFoundException TenantNotFoundException(Guid id)
        => new($"{typeof(Tenant).Name} with ID '{id}' was not found.");

    protected static KeyNotFoundException IdNotFoundException(IdT id)
        => new($"{typeof(EntityT).Name} with ID '{id}' was not found.");

    protected static KeyNotFoundException IdNotFoundOrConcurrentException(IdT id)
        => new($"{typeof(EntityT).Name} with ID '{id}' was not found or may have been deleted/modified concurrently.");
}
