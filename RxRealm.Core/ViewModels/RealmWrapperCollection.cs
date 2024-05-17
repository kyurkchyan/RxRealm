using System.Collections;
using System.Collections.Specialized;
using System.Reactive;
using System.Reactive.Disposables;
using DynamicData.Binding;
using Realms;
using Splat;

namespace RxRealm.Core.ViewModels;

public class RealmWrapperCollection<T, TViewModel> : INotifyCollectionChanged, IReadOnlyList<TViewModel>, IList, IDisposable
    where T : IRealmObject
    where TViewModel : class, IDisposable, IModelWrapperViewModel<T>
{
    private readonly CompositeDisposable _disposables = new();
    private readonly IRealmCollection<T> _realmCollection;
    private readonly Func<T, TViewModel> _viewModelFactory;
    private readonly MemoizingMRUCache<int, TViewModel> _viewModelCache;

    public RealmWrapperCollection(IRealmCollection<T> realmCollection, Func<T, TViewModel> viewModelFactory)
    {
        _realmCollection = realmCollection;
        _viewModelFactory = viewModelFactory;
        _viewModelCache = new MemoizingMRUCache<int, TViewModel>((index, _) => _viewModelFactory(_realmCollection[index]), 1000, viewModel => viewModel.Dispose());
        Disposable
            .Create(() => _viewModelCache.InvalidateAll())
            .DisposeWith(_disposables);
        _realmCollection
            .ObserveCollectionChanges()
            .Subscribe(HandleCollectionChanges)
            .DisposeWith(_disposables);
    }

    public int Count => _realmCollection.Count;

    public bool IsFixedSize => false;
    public bool IsReadOnly => true;

    object? IList.this[int index]
    {
        get => GetItemAt(index);
        set => throw new NotImplementedException();
    }

    public TViewModel this[int index] => GetItemAt(index);

    private TViewModel GetItemAt(int index) => _viewModelCache.Get(index);

    public bool Contains(object? value) => value is TViewModel && _viewModelCache.CachedValues().Contains(value);

    public int IndexOf(object? value)
    {
        if (value is not TViewModel viewModel) throw new ArgumentException($"Value must be of type {typeof(TViewModel).FullName}", nameof(value));

        return _realmCollection.IndexOf(viewModel.Model);
    }

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public IEnumerator<TViewModel> GetEnumerator() => new LazyEnumerator(_realmCollection.GetEnumerator(), _viewModelFactory);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void HandleCollectionChanges(EventPattern<NotifyCollectionChangedEventArgs> args)
    {
        var notifyCollectionChangedEventArgs = args.EventArgs;
        NotifyCollectionChangedEventArgs newArgs;

        List<TViewModel>? oldItems = notifyCollectionChangedEventArgs.OldItems?.Count > 0
                                         ? Enumerable.Range(notifyCollectionChangedEventArgs.OldStartingIndex, notifyCollectionChangedEventArgs.OldItems.Count)
                                                     .Select(index => _viewModelCache.TryGet(index, out TViewModel? viewModel) ? viewModel : null)
                                                     .Where(item => item != null)
                                                     .Select(item => item!)
                                                     .ToList()
                                         : null;

        List<TViewModel>? newItems = notifyCollectionChangedEventArgs.NewItems?.Count > 0
                                         ? Enumerable.Range(notifyCollectionChangedEventArgs.NewStartingIndex, notifyCollectionChangedEventArgs.NewItems.Count)
                                                     .Select(index => _viewModelCache.TryGet(index, out TViewModel? viewModel) ? viewModel : null)
                                                     .Where(item => item != null)
                                                     .Select(item => item!)
                                                     .ToList()
                                         : null;

        switch (notifyCollectionChangedEventArgs.Action)
        {
            case NotifyCollectionChangedAction.Add:
            {
                newArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
                                                               newItems,
                                                               notifyCollectionChangedEventArgs.NewStartingIndex);
                break;
            }

            case NotifyCollectionChangedAction.Remove:
            {
                newArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                                                               oldItems,
                                                               notifyCollectionChangedEventArgs.OldStartingIndex);
                break;
            }

            case NotifyCollectionChangedAction.Replace:
            {
                newArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace,
                                                               newItems!,
                                                               oldItems!,
                                                               notifyCollectionChangedEventArgs.OldStartingIndex);
                break;
            }

            case NotifyCollectionChangedAction.Move:
            {
                newArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move,
                                                               newItems,
                                                               notifyCollectionChangedEventArgs.NewStartingIndex,
                                                               notifyCollectionChangedEventArgs.OldStartingIndex);
                break;
            }

            case NotifyCollectionChangedAction.Reset:
            {
                newArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                break;
            }

            default:
            {
                throw new ArgumentOutOfRangeException(nameof(args));
            }
        }

        CollectionChanged?.Invoke(this, newArgs);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    private class LazyEnumerator(IEnumerator<T> collectionEnumerator, Func<T, TViewModel> viewModelFactory) : IEnumerator<TViewModel>
    {
        public TViewModel Current => viewModelFactory(collectionEnumerator.Current);

        object IEnumerator.Current => Current;

        public bool MoveNext() => collectionEnumerator.MoveNext();

        public void Reset() => collectionEnumerator.Reset();

        public void Dispose() => collectionEnumerator.Dispose();
    }

    #region IList implementation that's not needed

    public void CopyTo(Array array, int index)
    {
        throw new NotImplementedException();
    }

    public bool IsSynchronized => false;
    public object SyncRoot { get; } = new();
    public int Add(object? value) => throw new NotImplementedException();

    public void Clear()
    {
        throw new NotImplementedException();
    }

    public void Insert(int index, object? value)
    {
        throw new NotImplementedException();
    }

    public void Remove(object? value)
    {
        throw new NotImplementedException();
    }

    public void RemoveAt(int index)
    {
        throw new NotImplementedException();
    }

    #endregion
}
