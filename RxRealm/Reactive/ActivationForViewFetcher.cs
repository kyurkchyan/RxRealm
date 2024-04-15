using System.Reactive.Linq;
using ReactiveUI;

namespace RxRealm.Reactive;

public class ActivationForViewFetcher : IActivationForViewFetcher
{
    public int GetAffinityForView(Type view)
    {
        bool isCustomActivatableView = typeof(ICustomActivatableView).IsAssignableFrom(view);
        return isCustomActivatableView ? 20 : 0;
    }

    public IObservable<bool> GetActivationForView(IActivatableView view)
    {
        if (view is ICustomActivatableView customActivatableView)
        {
            return customActivatableView.Activated.Select(_ => true)
                                        .Merge(customActivatableView.Deactivated.Select(_ => false))
                                        .DistinctUntilChanged()
                                        .Replay(1)
                                        .RefCount();
        }

        return Observable.Never<bool>();
    }
}
