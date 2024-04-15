using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ReactiveUI;

namespace RxRealm.Reactive;

public static class ViewExtensions
{
    public static IConnectableObservable<bool> GetSharedIsActivated(this IActivatableView @this) =>
        Observable.Create<bool>(observer =>
                  {
                      return @this.WhenActivated(disposables =>
                      {
                          observer.OnNext(true);
                          disposables.Add(Disposable.Create(() => observer.OnNext(false)));
                      });
                  })
                  .Replay(1);

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
