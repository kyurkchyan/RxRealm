using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ReactiveUI;

namespace RxRealm.Core.Extensions;

public static class ReactiveExtensions
{

    public static IObservable<bool> GetIsActivated(this IActivatableViewModel @this) =>
        @this.Activator.Activated.Select(_ => true)
             .Merge(@this.Activator.Deactivated.Select(_ => false))
             .Replay(1)
             .RefCount();

    public static IConnectableObservable<bool> GetSharedIsActivated(this IActivatableViewModel @this)
    {
        IConnectableObservable<bool> connectableIsActivated = @this
                                                              .GetIsActivated()
                                                              .Publish();
        return connectableIsActivated;
    }

    public static IDisposable BindActivationTo<TTarget, TSource>(this TTarget @this, TSource source)
        where TTarget : IActivatableViewModel
        where TSource : IActivatableViewModel
        => @this.BindActivationTo(source.Activator);

    public static IDisposable BindActivationTo<TTarget>(this TTarget @this, ViewModelActivator activator) where TTarget : IActivatableViewModel
    {
        return new CompositeDisposable
        {
            activator.Activated
                     .Subscribe(_ => @this.Activator.Activate()),
            activator.Deactivated
                     .Subscribe(_ => @this.Activator.Deactivate(true))
        };
    }
}
