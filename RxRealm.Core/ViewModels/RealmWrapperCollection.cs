using System.Collections;
using System.Collections.Specialized;
using Realms;
using Splat;

namespace RxRealm.Core.ViewModels;

public class RealmWrapperCollection<T, TViewModel> : INotifyCollectionChanged, IReadOnlyList<TViewModel>, IList, IDisposable
    where T : IRealmObject
    where TViewModel : class, IDisposable
{
    private readonly IRealmCollection<T> _realmCollection;
    private readonly Func<T, TViewModel> _viewModelFactory;
    private readonly MemoizingMRUCache<int, TViewModel> _viewModelCache;

    public RealmWrapperCollection(IRealmCollection<T> realmCollection, Func<T, TViewModel> viewModelFactory)
    {
        _realmCollection = realmCollection;
        _viewModelFactory = viewModelFactory;
        _viewModelCache = new MemoizingMRUCache<int, TViewModel>((index, _) => _viewModelFactory(_realmCollection[index]), 1000, viewModel => viewModel.Dispose());
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

    public event NotifyCollectionChangedEventHandler? CollectionChanged
    {
        add => _realmCollection.CollectionChanged += value;
        remove => _realmCollection.CollectionChanged -= value;
    }

    public IEnumerator<TViewModel> GetEnumerator() => new LazyEnumerator(_realmCollection.GetEnumerator(), _viewModelFactory);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Dispose()
    {
        _viewModelCache.InvalidateAll();
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

    public bool Contains(object? value) => throw new NotImplementedException();

    public int IndexOf(object? value) => throw new NotImplementedException();

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
