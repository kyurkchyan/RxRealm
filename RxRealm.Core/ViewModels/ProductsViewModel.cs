using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using RxRealm.Core.Models;
using RxRealm.Core.Reactive;
using RxRealm.Core.Services;

namespace RxRealm.Core.ViewModels;

public class ProductsViewModel : ReactiveObject, IActivatableViewModel, IDisposable
{
    private readonly SerialDisposable _productsCollection = new();
    private CompositeDisposable Disposables { get; } = new();

    public ProductsViewModel(ProductsService productsService)
    {
        var isActivated = this.GetSharedIsActivated();

        _productsCollection.DisposeWith(Disposables);

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
        var products = await productsService.GetProductsAsync(products =>
                                                              {
                                                                  if (PriceFilter.HasValue)
                                                                  {
                                                                      products = products.Where(p => p.Price <= PriceFilter);
                                                                  }

                                                                  return products.OrderBy(p => p.Price);
                                                              },
                                                              cancellationToken);

        var productsCollection = new RealmWrapperCollection<Product, ProductViewModel>(products, p => new ProductViewModel(p));
        _productsCollection.Disposable = productsCollection;
        Products = productsCollection;
    }

    [Reactive]
    public IEnumerable<ProductViewModel>? Products { get; private set; }

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
