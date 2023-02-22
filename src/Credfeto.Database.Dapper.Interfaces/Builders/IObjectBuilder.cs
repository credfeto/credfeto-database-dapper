namespace Credfeto.Database.Dapper.Interfaces.Builders;

public interface IObjectBuilder<in TSourceObject, out TDestinationObject>
    where TSourceObject : class where TDestinationObject : class
{
    TDestinationObject? Build(TSourceObject source);
}