using Credfeto.Database.Dapper.Interfaces.Builders;
using Credfeto.Database.Shared.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace Credfeto.Database.Shared;

public static class SharedDatabaseSetup
{
    public static IServiceCollection AddDatabaseShared(this IServiceCollection services)
    {
        return services.AddSingleton(typeof(IObjectCollectionBuilder<,>), typeof(ObjectCollectionBuilder<,>));
    }
}