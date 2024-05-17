using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Realms;
using RxRealm.Core.Models;
using RxRealm.Core.Reactive;
using RxRealm.Core.Services;

namespace RxRealm.Core.ViewModels;

public class ProductsViewModel : ReactiveObject, IActivatableViewModel, IDisposable
{
    private CompositeDisposable Disposables { get; } = new();

    public ProductsViewModel(ProductsService productsService)
    {
        var isActivated = this.GetSharedIsActivated();
        
        Load = ReactiveCommand.CreateFromTask(ct => LoadProducts(productsService, ct));
        Load.DisposeWith(Disposables);

        var productIsSelected = this.WhenAnyValue(vm => vm.SelectedProduct).Select(p => p != null);
        Add = ReactiveCommand.CreateFromTask(ct => AddProduct(productsService), productIsSelected);
        Add.DisposeWith(Disposables);

        Remove = ReactiveCommand.CreateFromTask(ct => RemoveProduct(productsService), productIsSelected);
        Remove.DisposeWith(Disposables);

        ChangeName = ReactiveCommand.CreateFromTask(ct => ChangeNameImpl(productsService), productIsSelected);
        ChangeName.DisposeWith(Disposables);

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

    private async Task<Unit> ChangeNameImpl(ProductsService productsService)
    {
        var product = SelectedProduct!;
        await productsService.UpdateAsync(product, p => p.Name = Guid.NewGuid().ToString());

        return Unit.Default;
    }

    private async Task<Unit> AddProduct(ProductsService productsService)
    {
        var product1 = SelectedProduct!;
        var product2 = Products![Products!.IndexOf(SelectedProduct) + 1];
        var newPrice = (product1.Price + product2.Price) / 2;
        var newProduct = new Product
        {
            Id = Guid.NewGuid(),
            Name = $"New Product {newPrice}",
            Price = newPrice
        };
        await productsService.AddAsync(newProduct);

        return Unit.Default;
    }

    private async Task<Unit> RemoveProduct(ProductsService productsService)
    {
        var productToDelete = SelectedProduct!;
        SelectedProduct = null;
        await productsService.RemoveAsync(productToDelete);
        return Unit.Default;
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
    public IRealmCollection<Product>? Products { get; private set; }

    [Reactive]
    public Product? SelectedProduct { get; set; }

    public decimal? PriceFilter { get; set; }

    [ObservableAsProperty]
    public bool IsBusy { get; }

    public ReactiveCommand<Unit, Unit> Load { get; }

    public ReactiveCommand<Unit, Unit> Add { get; }
    public ReactiveCommand<Unit, Unit> Remove { get; }
    public ReactiveCommand<Unit, Unit> ChangeName { get; }

    public ViewModelActivator Activator { get; } = new();

    public void Dispose()
    {
        Disposables.Dispose();
    }
}
