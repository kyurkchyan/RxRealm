using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ReactiveUI;

namespace RxRealm.Core.Reactive;

public static class ReactiveExtensions
{
    public static IConnectableObservable<bool> GetSharedIsActivated(this IActivatableViewModel @this)
    {
        IConnectableObservable<bool> connectableIsActivated = @this
                                                              .Activator.Activated.Select(_ => true)
                                                              .Merge(@this.Activator.Deactivated.Select(_ => false))
                                                              .Replay(1);
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

    public static IDisposable BindDisposable<T>(this IViewFor<T> @this, Func<T, IDisposable> disposableFactory) where T : class, IReactiveObject =>
        @this.WhenAnyValue(x => x.ViewModel)
             .WhereNotNull()
             .Select(viewModel => Observable.Create<IDisposable>(observer =>
             {
                 IDisposable registration = disposableFactory(viewModel);
                 observer.OnNext(registration);
                 return registration;
             }))
             .Switch()
             .Subscribe();
}
