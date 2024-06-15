using DynamicData;

namespace RxRealm.Core;

public interface IPaginatedResults<T> : IDisposable where T : notnull
{
    public IObservable<bool> HasMore { get; }

    public IObservable<PaginatedResponse<T>> LoadNextPage();

    public IObservableList<T> Items { get; }

    public IObservable<int> TotalSize { get; }
}
