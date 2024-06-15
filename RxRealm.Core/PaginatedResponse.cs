using DynamicData;

namespace RxRealm.Core;

public record PaginatedResponse<T>(IEnumerable<T> Items, int Size, int StartIndex, int TotalSize) : IVirtualResponse;
