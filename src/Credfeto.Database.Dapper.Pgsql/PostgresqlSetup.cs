using Microsoft.Extensions.DependencyInjection;

namespace Credfeto.Database.Dapper.Pgsql;

public static class PostgresqlSetup
{
    public static IServiceCollection AddPostgresql(this IServiceCollection services)
    {
        return services.AddSingleton<IDatabase, PgsqlDatabase>();
    }
}