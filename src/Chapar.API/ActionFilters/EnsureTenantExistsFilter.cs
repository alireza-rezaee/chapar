using EFCoreSecondLevelCacheInterceptor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Chapar.API.Data;

namespace Chapar.API.ActionFilters;

public class EnsureTenantAccessibilityFilter(ApplicationDbContext dbContext) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.RouteData.Values.TryGetValue("tenantId", out object? rawValue) &&
            Guid.TryParse(rawValue?.ToString(), out Guid tenantId))
        {
            bool exists = await dbContext.Tenants
                .AsNoTracking()
                .Where(t => t.Id == tenantId)
                .Cacheable(CacheExpirationMode.Absolute, TimeSpan.FromMinutes(10))
                .AnyAsync(context.HttpContext.RequestAborted);

            // TODO(authorization): check if tenant is availble for surrent user or not

            if (!exists)
            {
                context.Result = new NotFoundObjectResult(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "NotFound",
                    Detail = $"Tenant with ID '{tenantId}' was not found."
                });
                return;
            }
        }

        await next();
    }
}
