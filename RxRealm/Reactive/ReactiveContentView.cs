using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ReactiveUI;

namespace RxRealm.Reactive;

public class ReactiveContentView<T> : ReactiveUI.Maui.ReactiveContentView<T>, ICustomActivatableView, IDisposable
    where T : class
{
    private readonly BehaviorSubject<bool> _activator = new(true);
    private readonly IObservable<Unit> _activated;
    private readonly IObservable<Unit> _deactivated;
    private readonly CompositeDisposable _disposables = new();

    public ReactiveContentView() : this(Observable.Return(true))
    {
    }

    public ReactiveContentView(IObservable<bool> parentActivator)
    {
        var activator = Observable.CombineLatest([this.WhenAnyValue(v => v.IsVisible), _activator, parentActivator])
                                  .Select(items => items.All(x => x))
                                  .DistinctUntilChanged()
                                  .Replay(1);

        activator.Connect().DisposeWith(_disposables);

        activator.Do(isActive => Debug.WriteLine($"{GetType().FullName} {GetHashCode()} is {(isActive ? "active" : "inactive")}"))
                 .Subscribe()
                 .DisposeWith(_disposables);

        _activated = activator.Where(isActive => isActive)
                              .Select(_ => Unit.Default);
        _deactivated = activator.Where(isActive => !isActive)
                                .Select(_ => Unit.Default);
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

    public void Dispose()
    {
        _activator.Dispose();
        _disposables?.Dispose();
    }
}
