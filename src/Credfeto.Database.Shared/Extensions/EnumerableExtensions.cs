using System.Collections.Generic;

namespace Credfeto.Database.Shared.Extensions;

internal static class EnumerableExtensions
{
    public static IEnumerable<TItemType> RemoveNulls<TItemType>(this IEnumerable<TItemType?> source)
        where TItemType : class
    {
        foreach (TItemType? item in source)
        {
            if (!ReferenceEquals(objA: item, objB: null))
            {
                yield return item;
            }
        }
    }
}