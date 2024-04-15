using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ReactiveUI;

namespace RxRealm.Reactive;

public class ReactiveContentView<T> : ReactiveUI.Maui.ReactiveContentView<T>, ICustomActivatableView
    where T : class
{
    private readonly BehaviorSubject<bool> _activator = new(true);
    private readonly IObservable<Unit> _activated;
    private readonly IObservable<Unit> _deactivated;
    private readonly SerialDisposable _bindings = new();

    public ReactiveContentView()
    {
        var activator = Observable.CombineLatest(this.WhenAnyValue(v => v.IsVisible), _activator)
                                  .Select(items => items.All(x => x));
        _activated = activator.Where(isActive => isActive)
                              .Select(_ => Unit.Default);
        _deactivated = activator.Where(isActive => !isActive)
                                .Select(_ => Unit.Default);
    }

    public ReactiveContentView(IObservable<bool> parentActivator) : this()
    {
        SetParent(parentActivator);
    }

    protected override void OnParentChanged()
    {
        base.OnParentChanged();
        Debug.WriteLine($"Parent changed for {GetType().Name}");
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        Debug.WriteLine($"Handler changed for {GetType().Name}");
    }

    public IObservable<Unit> Activated => _activated.AsObservable();
    public IObservable<Unit> Deactivated => _deactivated.AsObservable();

    public void Activate()
    {
        _activator.OnNext(true);
    }

    public void Deactivate()
    {
        _activator.OnNext(false);
    }

    public void SetParent(IObservable<bool> parentActivator)
    {
        _bindings.Disposable = this.BindActivationTo(parentActivator);
    }
}
