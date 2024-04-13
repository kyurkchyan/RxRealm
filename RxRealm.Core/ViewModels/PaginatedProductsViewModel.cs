using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using RxRealm.Core.Extensions;
using RxRealm.Core.Models;
using RxRealm.Core.Services;

namespace RxRealm.Core.ViewModels;

public class PaginatedProductsViewModel : ReactiveObject, IActivatableViewModel, IDisposable
{
    private const int PageSize = 50;

    [Reactive] private PaginatedResults<Product>? PaginatedProducts { get; set; }
    internal ObservableCollectionExtended<ProductViewModel> InternalProducts { get; } = [];
    public CompositeDisposable Disposables { get; } = new();

    public PaginatedProductsViewModel(ProductsService productsService)
    {
        var isActivated = this.GetSharedIsActivated();

        Products = new ReadOnlyObservableCollection<ProductViewModel>(InternalProducts);

        Load = ReactiveCommand.CreateFromTask(ct => LoadProducts(productsService, ct));
        Load.DisposeWith(Disposables);

        Load.ThrownExceptions.Subscribe(ex => Console.WriteLine(ex.Message));
        Load.IsExecuting.ToPropertyEx(this, vm => vm.IsBusy);
        isActivated
            .Where(isActive => isActive && PaginatedProducts == null)
            .Select(_ => Unit.Default)
            .InvokeCommand(Load)
            .DisposeWith(Disposables);

        var hasMoreProducts = this.WhenAnyValue(v => v.PaginatedProducts)
            .WhereNotNull()
            .Select(products => products.HasMore)
            .Switch();
        var isNotLoading = Load.IsExecuting.Select(isLoadExecuting => !isLoadExecuting);
        var canLoadMore = Observable.CombineLatest(hasMoreProducts, isNotLoading)
            .Select(booleans => booleans.All(b => b));
        LoadMore = ReactiveCommand.CreateFromObservable(LoadMoreImpl, canLoadMore);
        LoadMore.DisposeWith(Disposables);

        Disposables.Add(Activator);

        isActivated.Connect().DisposeWith(Disposables);
    }

    private async Task LoadProducts(ProductsService productsService, CancellationToken cancellationToken = default)
    {
        PaginatedProducts = await productsService.GetPaginatedProductsAsync(
            products =>
            {
                if (PriceFilter.HasValue)
                {
                    products = products.Where(p => p.Price <= PriceFilter);
                }

                return products.OrderBy(p => p.Price);
            }, PageSize, cancellationToken);

        PaginatedProducts.Items
            .Connect()
            .Transform(product => new ProductViewModel(product))
            .DisposeMany()
            .Bind(InternalProducts)
            .Subscribe()
            .DisposeWith(Disposables);

        await PaginatedProducts.LoadNextPage().ToTask(cancellationToken);
    }

    private IObservable<Unit> LoadMoreImpl()
    {
        return PaginatedProducts!.LoadNextPage().Select(_ => Unit.Default);
    }

    public IEnumerable<ProductViewModel>? Products { get; }

    public decimal? PriceFilter { get; set; }

    [ObservableAsProperty] public bool IsBusy { get; }
    public ReactiveCommand<Unit, Unit> Load { get; }
    public ReactiveCommand<Unit, Unit> LoadMore { get; }
    public ViewModelActivator Activator { get; } = new();

    public void Dispose()
    {
        Disposables.Dispose();
    }
}
