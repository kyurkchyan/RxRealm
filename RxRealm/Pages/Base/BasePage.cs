using System.Reactive.Disposables;
using ReactiveUI.Maui;
using RxRealm.Reactive;

namespace RxRealm.Pages.Base;

public class BasePage<T> : ReactiveContentPage<T> where T : class
{
    protected CompositeDisposable Disposables { get; } = new();
    protected IObservable<bool> Activator { get; }

    protected BasePage()
    {
        var activator = this.GetSharedIsActivated();
        activator.Connect().DisposeWith(Disposables);
        Activator = activator;
    }

    protected override void OnParentChanged()
    {
        base.OnParentChanged();
        if (Parent == null)
        {
            Disposables.Dispose();
        }
    }
}
