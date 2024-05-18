using System.Collections;
using System.Collections.Specialized;
using System.Reactive;
using System.Reactive.Disposables;
using DynamicData.Binding;
using Realms;
using RxRealm.Core.Models;
using Splat;

namespace RxRealm.Core.ViewModels;

public class RealmWrapperCollection<TModel, TViewModel, TId> : INotifyCollectionChanged, IReadOnlyList<TViewModel>, IList, IDisposable
    where TModel : IRealmObject, IHasId<TId>
    where TViewModel : class, IDisposable, IModelWrapperViewModel<TModel, TId>
    where TId : notnull
{
    const int CacheSize = 1000;
    private int _maxAccessedIndex;
    private readonly CompositeDisposable _disposables = new();
    private readonly IRealmCollection<TModel> _realmCollection;
    private readonly Func<TModel, TViewModel> _viewModelFactory;
    private readonly MemoizingMRUCache<TId, TViewModel> _viewModelCache;
    private readonly Dictionary<int, TViewModel> _indexToViewModel = new();
    private readonly Dictionary<TViewModel, int> _viewModelToIndex = new();

    private record ViewModelCreationContext(TModel Model, int Index);

    public RealmWrapperCollection(IRealmCollection<TModel> realmCollection, Func<TModel, TViewModel> viewModelFactory)
    {
        _realmCollection = realmCollection;
        _viewModelFactory = viewModelFactory;

        _viewModelCache = new MemoizingMRUCache<TId, TViewModel>((_, creationContext) => CreateViewModel((ViewModelCreationContext)creationContext!),
                                                                 CacheSize,
                                                                 DisposeViewModel);

        Disposable
            .Create(() => _viewModelCache.InvalidateAll())
            .DisposeWith(_disposables);
        _realmCollection
            .ObserveCollectionChanges()
            .Subscribe(HandleCollectionChanges)
            .DisposeWith(_disposables);
    }

    public int Count => _realmCollection.Count;

    object? IList.this[int index]
    {
        get => GetItemAt(index);
        set => throw new NotImplementedException();
    }

    public TViewModel this[int index] => GetItemAt(index);

    private TViewModel GetItemAt(int index)
    {
        if (index > _maxAccessedIndex)
        {
            _maxAccessedIndex = index;
        }

        TModel model = _realmCollection[index];
        var viewModel = GetViewModelFromCache(model, index);
        return viewModel;
    }

    public bool Contains(object? value) => value is TViewModel viewModel && _realmCollection.Contains(viewModel.Model);

    public int IndexOf(object? value)
    {
        if (value is not TViewModel viewModel) throw new ArgumentException($"Value must be of type {typeof(TViewModel).FullName}", nameof(value));
        return _realmCollection.IndexOf(viewModel.Model);
    }

    public bool IsFixedSize => false;
    public bool IsReadOnly => true;
    public bool IsSynchronized => false;
    public object SyncRoot => _realmCollection;

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public IEnumerator<TViewModel> GetEnumerator() => new LazyEnumerator(_realmCollection.GetEnumerator(), _viewModelFactory);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private TViewModel CreateViewModel(ViewModelCreationContext context)
    {
        (TModel model, int index) = context;
        TViewModel viewModel = _viewModelFactory(model);
        _viewModelToIndex[viewModel] = index;
        _indexToViewModel[index] = viewModel;
        return viewModel;
    }

    private TViewModel GetViewModelFromCache(TModel model, int index) => _viewModelCache.Get(model.Id, new ViewModelCreationContext(model, index));

    private void DisposeViewModel(TViewModel viewModel)
    {
        _viewModelToIndex.Remove(viewModel, out int index);
        _indexToViewModel.Remove(index);
        viewModel.Dispose();
    }

    private void HandleCollectionChanges(EventPattern<NotifyCollectionChangedEventArgs> args)
    {
        var notifyCollectionChangedEventArgs = args.EventArgs;

        List<TViewModel>? oldItems = notifyCollectionChangedEventArgs.OldItems?.Count > 0
                                         ? Enumerable.Range(notifyCollectionChangedEventArgs.OldStartingIndex, notifyCollectionChangedEventArgs.OldItems?.Count ?? 0)
                                                     .Select(index =>
                                                     {
                                                         if (_indexToViewModel.TryGetValue(index, out TViewModel? viewModel))
                                                         {
                                                             _viewModelCache.Invalidate(viewModel.Id);
                                                             return viewModel;
                                                         }

                                                         return null;
                                                     })
                                                     .Where(item => item != null)
                                                     .Select(item => item!)
                                                     .ToList()
                                         : null;

        List<TViewModel>? newItems = notifyCollectionChangedEventArgs.NewStartingIndex > 0
                                         ? notifyCollectionChangedEventArgs.NewItems?.ToEnumerable<TModel>(notifyCollectionChangedEventArgs.NewStartingIndex)?
                                                                           .Select(tuple =>
                                                                           {
                                                                               (TModel model, int index) = tuple;
                                                                               return notifyCollectionChangedEventArgs.NewStartingIndex >= _maxAccessedIndex
                                                                                          ? GetViewModelFromCache(model, index)
                                                                                          : null;
                                                                           })
                                                                           .Where(item => item != null)
                                                                           .Select(item => item!)
                                                                           .ToList()
                                         : null;

        NotifyCollectionChangedEventArgs newArgs = notifyCollectionChangedEventArgs.Action switch
        {
            NotifyCollectionChangedAction.Add => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems, notifyCollectionChangedEventArgs.NewStartingIndex),
            NotifyCollectionChangedAction.Remove => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItems, notifyCollectionChangedEventArgs.OldStartingIndex),
            NotifyCollectionChangedAction.Replace => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace,
                                                                                          newItems!,
                                                                                          oldItems!,
                                                                                          notifyCollectionChangedEventArgs.OldStartingIndex),
            NotifyCollectionChangedAction.Move => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move,
                                                                                       newItems,
                                                                                       notifyCollectionChangedEventArgs.NewStartingIndex,
                                                                                       notifyCollectionChangedEventArgs.OldStartingIndex),
            NotifyCollectionChangedAction.Reset => new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset),
            _ => throw new ArgumentOutOfRangeException(nameof(args))
        };

        RecalculateIndexes();
        CollectionChanged?.Invoke(this, newArgs);
    }

    private void RecalculateIndexes()
    {
        var viewModels = _indexToViewModel.Values.ToList();
        _indexToViewModel.Clear();
        _viewModelToIndex.Clear();
        foreach (TViewModel viewModel in viewModels)
        {
            int index = IndexOf(viewModel);
            if (index >= 0)
            {
                _indexToViewModel[index] = viewModel;
                _viewModelToIndex[viewModel] = index;
            }
        }
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    private class LazyEnumerator(IEnumerator<TModel> collectionEnumerator, Func<TModel, TViewModel> viewModelFactory) : IEnumerator<TViewModel>
    {
        public TViewModel Current => viewModelFactory(collectionEnumerator.Current);

        object IEnumerator.Current => Current;

        public bool MoveNext() => collectionEnumerator.MoveNext();

        public void Reset() => collectionEnumerator.Reset();

        public void Dispose() => collectionEnumerator.Dispose();
    }

    #region IList Edit functions that are unsupported because this collection is read only

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

    public void CopyTo(Array array, int index)
    {
        throw new NotImplementedException();
    }

    #endregion IList Edit functions that are unsupported because this collection is read only
}
