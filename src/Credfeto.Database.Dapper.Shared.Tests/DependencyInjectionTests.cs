using Credfeto.Database.Dapper.Shared.Tests.Mocks;
using Credfeto.Database.Shared;
using FunFair.Test.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Credfeto.Database.Dapper.Shared.Tests;

public sealed class DependencyInjectionTests : DependencyInjectionTestsBase
{
    public DependencyInjectionTests(ITestOutputHelper output)
        : base(output: output, dependencyInjectionRegistration: Configure)
    {
    }

    private static IServiceCollection Configure(IServiceCollection services)
    {
        return SharedDatabaseSetup.AddDatabaseShared(services.AddMockedService<IObjectBuilder<BananaEntity, Banana>>());
    }

    [Fact]
    public void ObjectCollectionBuilderMustBeRegistered()
    {
        this.RequireService<IObjectCollectionBuilder<BananaEntity, Banana>>();
    }
}