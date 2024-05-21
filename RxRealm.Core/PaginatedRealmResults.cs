using System.Collections.Specialized;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using Realms;

namespace RxRealm.Core;

public class PaginatedRealmResults<T> : PaginatedResults<T>
    where T : notnull
{
    private readonly CompositeDisposable _disposables = new();

    public PaginatedRealmResults(IRealmCollection<T> realmCollection, int pageSize = 50) : base(pageSize, Virtualize(realmCollection))
    {
        realmCollection
            .ObserveCollectionChanges()
            .Subscribe(HandleCollectionChanges)
            .DisposeWith(_disposables);
    }

    private static Func<IVirtualRequest, IObservable<PaginatedResponse<T>>> Virtualize(IReadOnlyCollection<T> realmCollection)
    {
        return pagination =>
        {
            var result = realmCollection
                         .Skip(pagination.StartIndex)
                         .Take(pagination.Size)
                         .ToList();

            return Observable.Return(new PaginatedResponse<T>(result,
                                                              result.Count,
                                                              pagination.StartIndex,
                                                              realmCollection.Count));
        };
    }

    private void HandleCollectionChanges(EventPattern<NotifyCollectionChangedEventArgs> args)
    {
        var notifyCollectionChangedEventArgs = args.EventArgs;

        switch (notifyCollectionChangedEventArgs.Action)
        {
            case NotifyCollectionChangedAction.Add:
            {
                if (notifyCollectionChangedEventArgs.NewItems != null)
                {
                    // If the new items are not in set of data that's already loaded, we don't need to do anything
                    if (notifyCollectionChangedEventArgs.NewStartingIndex >= SourceList.Count)
                    {
                        break;
                    }

                    var newItems = notifyCollectionChangedEventArgs.NewItems.Cast<T>();
                    SourceList.InsertRange(newItems, notifyCollectionChangedEventArgs.NewStartingIndex);
                }

                break;
            }
            case NotifyCollectionChangedAction.Remove:
            {
                if (notifyCollectionChangedEventArgs.OldItems != null)
                {
                    // If the old items are not in the set of data that's already loaded, we don't need to do anything
                    if (notifyCollectionChangedEventArgs.OldStartingIndex >= SourceList.Count)
                    {
                        break;
                    }

                    // Remove only the items that are in the set of data that's already loaded
                    var maxCount = Math.Min(notifyCollectionChangedEventArgs.OldItems.Count, SourceList.Count - notifyCollectionChangedEventArgs.OldStartingIndex);
                    if (notifyCollectionChangedEventArgs.OldStartingIndex < SourceList.Count)
                    {
                        SourceList.RemoveRange(notifyCollectionChangedEventArgs.OldStartingIndex, maxCount);
                    }
                }

                break;
            }
            case NotifyCollectionChangedAction.Replace:
            {
                throw new NotImplementedException();
            }
            case NotifyCollectionChangedAction.Move:
            {
                throw new NotImplementedException();
            }
            case NotifyCollectionChangedAction.Reset:
            {
                SourceList.Clear();
                break;
            }
            default:
            {
                throw new ArgumentOutOfRangeException(nameof(args));
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _disposables.Dispose();
        }

        base.Dispose(disposing);
    }
}
