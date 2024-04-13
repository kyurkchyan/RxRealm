using DynamicData;

namespace RxRealm.Core;

public class PaginatedResponse<T>(IEnumerable<T> items, int size, int startIndex, int totalSize)
    : IVirtualResponse
{
    public IEnumerable<T> Items { get; } = items;
    public int Size { get; } = size;
    public int StartIndex { get; } = startIndex;
    public int TotalSize { get; } = totalSize;
}