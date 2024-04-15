using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using RxRealm.Core.Extensions;
using RxRealm.Core.Models;
using RxRealm.Core.Services;

namespace RxRealm.Core.ViewModels;

public class ProductsViewModel : ReactiveObject, IActivatableViewModel, IDisposable
{
    public CompositeDisposable Disposables { get; } = new();

    public ProductsViewModel(ProductsService productsService)
    {
        var isActivated = this.GetSharedIsActivated();

        Load = ReactiveCommand.CreateFromTask(ct => LoadProducts(productsService, ct));
        Load.DisposeWith(Disposables);

        Load.ThrownExceptions.Subscribe(ex => Console.WriteLine(ex.Message));
        Load.IsExecuting.ToPropertyEx(this, vm => vm.IsBusy);
        isActivated
            .Where(isActive => isActive && Products == null)
            .Select(_ => Unit.Default)
            .InvokeCommand(Load)
            .DisposeWith(Disposables);

        Disposables.Add(Activator);

        isActivated.Connect().DisposeWith(Disposables);
    }

    private async Task LoadProducts(ProductsService productsService, CancellationToken cancellationToken = default)
    {
        Products = await productsService.GetProductsAsync(products =>
                                                          {
                                                              if (PriceFilter.HasValue)
                                                              {
                                                                  products = products.Where(p => p.Price <= PriceFilter);
                                                              }

                                                              return products.OrderBy(p => p.Price);
                                                          },
                                                          cancellationToken);
    }

    [Reactive]
    public IEnumerable<Product>? Products { get; private set; }

    public decimal? PriceFilter { get; set; }

    [ObservableAsProperty]
    public bool IsBusy { get; }

    public ReactiveCommand<Unit, Unit> Load { get; }
    public ViewModelActivator Activator { get; } = new();

    public void Dispose()
    {
        Disposables.Dispose();
    }
}