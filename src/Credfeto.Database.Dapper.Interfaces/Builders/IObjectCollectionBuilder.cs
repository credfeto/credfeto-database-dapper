using System.Collections.Generic;

namespace Credfeto.Database.Dapper.Interfaces.Builders;

public interface IObjectCollectionBuilder<in TSourceObject, out TDestinationObject> : IObjectBuilder<TSourceObject, TDestinationObject>
    where TSourceObject : class where TDestinationObject : class
{
    IReadOnlyList<TDestinationObject> Build(IEnumerable<TSourceObject> entities);
}