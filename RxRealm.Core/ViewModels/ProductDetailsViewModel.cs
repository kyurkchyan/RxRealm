using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using RxRealm.Core.Models;
using RxRealm.Core.Reactive;
using RxRealm.Core.Services;

namespace RxRealm.Core.ViewModels;

public class ProductDetailsViewModel : ReactiveObject, IActivatableViewModel, IDisposable
{
    public CompositeDisposable Disposables { get; } = new();

    public ProductDetailsViewModel(Guid productId,
                                   ProductsService productsService)
    {
        var isActivated = this.GetSharedIsActivated();

        Load = ReactiveCommand.Create(() =>
        {
            Product = productsService.GetProduct(productId);
        });
        Load.DisposeWith(Disposables);

        isActivated
            .Where(isActive => isActive && Product == null)
            .Select(_ => Unit.Default)
            .InvokeCommand(Load)
            .DisposeWith(Disposables);

        Load.ThrownExceptions.Subscribe(ex => Console.WriteLine(ex.Message));

        Disposables.Add(Activator);

        isActivated.Connect().DisposeWith(Disposables);

        this.WhenActivated(disposables =>
        {
            this
                .WhenAnyValue(x => x.Product!.Name)
                .ToPropertyEx(this, x => x.Name)
                .DisposeWith(disposables);

            this
                .WhenAnyValue(x => x.Product!.ImageUrl)
                .Select(x => x != null ? new Uri(x) : null)
                .ToPropertyEx(this, x => x.ImageUri)
                .DisposeWith(disposables);

            this
                .WhenAnyValue(x => x.Product!.Price)
                .ToPropertyEx(this, x => x.Price)
                .DisposeWith(disposables);
        });
    }

    [Reactive]
    public Product? Product { get; private set; }

    [ObservableAsProperty]
    public string? Name { get; }

    [ObservableAsProperty]
    public Uri? ImageUri { get; }

    [ObservableAsProperty]
    public decimal Price { get; }

    public ReactiveCommand<Unit, Unit> Load { get; }

    public ViewModelActivator Activator { get; } = new();

    public void Dispose()
    {
        Disposables.Dispose();
    }
}
