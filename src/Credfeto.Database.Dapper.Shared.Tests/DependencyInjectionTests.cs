using Credfeto.Database.Shared.Tests.Mocks;
using FunFair.Test.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Credfeto.Database.Shared.Tests;

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