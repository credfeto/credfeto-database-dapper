using System;
using System.Collections.Generic;
using System.Linq;
using Credfeto.Database.Dapper.Interfaces.Builders;
using Credfeto.Extensions.Linq;

namespace Credfeto.Database.Dapper.Shared.Builders;

public sealed class ObjectCollectionBuilder<TSourceObject, TDestinationObject> : IObjectCollectionBuilder<TSourceObject, TDestinationObject>
    where TSourceObject : class where TDestinationObject : class
{
    private readonly IObjectBuilder<TSourceObject, TDestinationObject> _entityBuilder;

    public ObjectCollectionBuilder(IObjectBuilder<TSourceObject, TDestinationObject> entityBuilder)
    {
        this._entityBuilder = entityBuilder ?? throw new ArgumentNullException(nameof(entityBuilder));
    }

    public TDestinationObject? Build(TSourceObject source)
    {
        return this._entityBuilder.Build(source);
    }

    public IReadOnlyList<TDestinationObject> Build(IEnumerable<TSourceObject> entities)
    {
        return entities.Select(this.Build)
                       .RemoveNulls()
                       .ToArray();
    }
}