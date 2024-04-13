using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using RxRealm.Core.Extensions;
using RxRealm.Core.Models;
using RxRealm.Core.Services;

namespace RxRealm.Core.ViewModels;

public class VirtualizedProductsViewModel : ReactiveObject, IActivatableViewModel, IDisposable
{
    private const int PageSize = 50;

    [Reactive]
    private IConnectableObservable<IVirtualChangeSet<Product>>? ProductsChangeSet { get; set; }

    private readonly BehaviorSubject<VirtualRequest> _paginationSubject = new(new VirtualRequest(0, PageSize));
    internal ObservableCollectionExtended<ProductViewModel> InternalProducts { get; } = [];
    public CompositeDisposable Disposables { get; } = new();

    public VirtualizedProductsViewModel(ProductsService productsService)
    {
        var isActivated = this.GetSharedIsActivated();

        Products = new ReadOnlyObservableCollection<ProductViewModel>(InternalProducts);

        Load = ReactiveCommand.CreateFromTask(ct => LoadProducts(productsService, _paginationSubject, ct));
        Load.DisposeWith(Disposables);

        Load.ThrownExceptions.Subscribe(ex => Console.WriteLine(ex.Message));
        Load.IsExecuting.ToPropertyEx(this, vm => vm.IsBusy);
        isActivated
            .Where(isActive => isActive && ProductsChangeSet == null)
            .Select(_ => Unit.Default)
            .InvokeCommand(Load)
            .DisposeWith(Disposables);

        var products = this.WhenAnyValue(vm => vm.ProductsChangeSet)
                           .Do(_ => InternalProducts.Clear())
                           .WhereNotNull()
                           .Select(changeSet => changeSet)
                           .Switch();

        products
            .Transform(product => new ProductViewModel(product))
            .DisposeMany()
            .Bind(InternalProducts)
            .Subscribe()
            .DisposeWith(Disposables);

        var hasMoreProducts = products.Select(virtualChangeset =>
                                                  virtualChangeset.Response.Size < virtualChangeset.Response.TotalSize);
        var isNotLoading = Load.IsExecuting.Select(isLoadExecuting => !isLoadExecuting);
        var canLoadMore = Observable.CombineLatest(hasMoreProducts, isNotLoading)
                                    .Select(booleans => booleans.All(b => b));
        LoadMore = ReactiveCommand.CreateFromObservable(LoadMoreImpl, canLoadMore);
        LoadMore.DisposeWith(Disposables);

        Disposables.Add(Activator);
        Disposables.Add(_paginationSubject);

        isActivated.Connect().DisposeWith(Disposables);
    }

    private async Task LoadProducts(ProductsService productsService,
                                    IObservable<VirtualRequest> pagination, CancellationToken cancellationToken = default)
    {
        ProductsChangeSet = await productsService.GetVirtualizedProductsAsync(products =>
                                                                              {
                                                                                  if (PriceFilter.HasValue)
                                                                                  {
                                                                                      products = products.Where(p => p.Price <= PriceFilter);
                                                                                  }

                                                                                  return products.OrderBy(p => p.Price);
                                                                              },
                                                                              pagination,
                                                                              cancellationToken);
        ProductsChangeSet.Connect().DisposeWith(Disposables);

        await ProductsChangeSet.FirstAsync().ToTask(cancellationToken);
    }

    private IObservable<Unit> LoadMoreImpl()
    {
        var currentCount = InternalProducts.Count;
        var changedCount = this.WhenAnyValue(vm => vm.InternalProducts.Count)
                               .Where(count => count != currentCount)
                               .Select(_ => Unit.Default)
                               .FirstAsync();
        _paginationSubject.OnNext(new VirtualRequest(0, currentCount + PageSize));
        return changedCount;
    }

    public IEnumerable<ProductViewModel>? Products { get; }

    public decimal? PriceFilter { get; set; }

    [ObservableAsProperty]
    public bool IsBusy { get; }

    public ReactiveCommand<Unit, Unit> Load { get; }
    public ReactiveCommand<Unit, Unit> LoadMore { get; }
    public ViewModelActivator Activator { get; } = new();

    public void Dispose()
    {
        Disposables.Dispose();
    }
}
