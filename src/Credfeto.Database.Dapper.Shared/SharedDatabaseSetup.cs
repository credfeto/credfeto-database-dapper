using Credfeto.Database.Dapper.Interfaces.Builders;
using Credfeto.Database.Dapper.Shared.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace Credfeto.Database.Dapper.Shared;

public static class SharedDatabaseSetup
{
    public static IServiceCollection AddDatabaseShared(this IServiceCollection services)
    {
        return services.AddSingleton(typeof(IObjectCollectionBuilder<,>), typeof(ObjectCollectionBuilder<,>));
    }
}