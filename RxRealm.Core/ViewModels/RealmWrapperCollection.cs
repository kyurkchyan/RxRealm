using System.Collections;
using System.Collections.Specialized;
using Realms;

namespace RxRealm.Core.ViewModels;

public class RealmWrapperCollection<T, TViewModel>(IRealmCollection<T> collection, Func<T, TViewModel> viewModelFactory) : INotifyCollectionChanged, IReadOnlyList<TViewModel>
    where T : IRealmObject
    where TViewModel : class
{
    public int Count => collection.Count;

    public TViewModel this[int index] => viewModelFactory(collection[index]);

    public event NotifyCollectionChangedEventHandler? CollectionChanged
    {
        add => collection.CollectionChanged += value;
        remove => collection.CollectionChanged -= value;
    }

    public IEnumerator<TViewModel> GetEnumerator()
    {
        foreach (var item in collection)
        {
            yield return viewModelFactory(item);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
