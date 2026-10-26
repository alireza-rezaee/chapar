using EFCoreSecondLevelCacheInterceptor;
using Microsoft.EntityFrameworkCore;
using Chapar.API.Data;

namespace Chapar.API.Extensions;

public static class MsSqlServiceCollectionExtensions
{
    public static IServiceCollection AddConfiguredMsSqlDbContext(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
            options
                .UseSqlServer(
                    connectionString,
                    mysqlOptions =>
                    {
                        mysqlOptions
                            .CommandTimeout((int)TimeSpan.FromMinutes(3).TotalSeconds)
                            .EnableRetryOnFailure()
                            .MigrationsAssembly(
                                typeof(MsSqlServiceCollectionExtensions).Assembly.FullName);
                    })
                .AddInterceptors(serviceProvider.GetRequiredService<SecondLevelCacheInterceptor>()));

        return services;
    }
}
