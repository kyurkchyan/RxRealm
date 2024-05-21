using System.Collections;

namespace RxRealm.Core.ViewModels;

public static class CollectionExtensions
{
    public static IEnumerable<object> ToEnumerable(this IList list)
    {
        return Enumerable.Range(0, list.Count).Select(i => list[i]!);
    }

    public static IEnumerable<(T, int)> ToEnumerable<T>(this IList list, int startIndex)
    {
        return Enumerable.Range(0, list.Count).Select(i => ((T)list[i]!, startIndex++));
    }

    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
        {
            action(item);
        }
    }
}
