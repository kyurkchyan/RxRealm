using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;

namespace RxRealm.Reactive;

public static class ViewExtensions
{
    public static IObservable<bool> GetIsActivated(this IActivatableView @this) =>
        Observable.Create<bool>(observer =>
                  {
                      return @this.WhenActivated(disposables =>
                      {
                          observer.OnNext(true);
                          disposables.Add(Disposable.Create(() => observer.OnNext(false)));
                      });
                  })
                  .Replay(1)
                  .RefCount();

    public static IDisposable BindActivationTo(this ICustomActivatableView @this, IObservable<bool> activator) =>
        activator
            .Subscribe(isActivated =>
            {
                if (isActivated)
                {
                    @this.Activate();
                }
                else
                {
                    @this.Deactivate();
                }
            });
}
