using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
using ReactiveUI;

namespace RxRealm.Core;

public class PaginatedResults<T> : IDisposable
    where T : notnull
{
    private readonly int _pageSize;
    private readonly CompositeDisposable _cleanUp = new();
    private readonly SourceList<T> _sourceList = new();
    private readonly BehaviorSubject<IVirtualRequest?> _pager = new(null);
    private readonly IConnectableObservable<PaginatedResponse<T>> _loadItems;

    public PaginatedResults(int pageSize, Func<IVirtualRequest, IObservable<PaginatedResponse<T>>> virtualize)
    {
        _pageSize = pageSize;
        _sourceList.DisposeWith(_cleanUp);
        _pager.DisposeWith(_cleanUp);

        // Load the items into the _sourceList when the _pager requests the next page
        _loadItems = _pager
            .WhereNotNull()
            .SelectMany(virtualize)
            .Do(newItems => _sourceList.AddRange(newItems.Items))
            .Publish();

        HasMore = _loadItems.Select(virtualResponse => virtualResponse.Size < virtualResponse.TotalSize);
        TotalSize = _loadItems.Select(virtualResponse => virtualResponse.TotalSize);

        var items = _sourceList.Connect().Publish();

        Items = items.AsObservableList();
        Items.DisposeWith(_cleanUp);

        _loadItems.Connect().DisposeWith(_cleanUp);
        items.Connect().DisposeWith(_cleanUp);
    }

    public IObservable<bool> HasMore { get; }

    public IObservable<PaginatedResponse<T>> LoadNextPage()
    {
        return Observable.Create<PaginatedResponse<T>>(observer =>
        {
            var subscription = _loadItems.FirstAsync().Subscribe(
                onNext: response =>
                {
                    observer.OnNext(response);
                    observer.OnCompleted();
                },
                onError: observer.OnError);

            _pager.OnNext(new VirtualRequest(Items.Count, _pageSize));

            return subscription;
        });
    }


    public IObservableList<T> Items { get; }

    public IObservable<int> TotalSize { get; }

    public void Dispose()
    {
        _cleanUp.Dispose();
    }
}