using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DevExpress.Maui.CollectionView;
using ReactiveUI;
using RxRealm.Core;
using RxRealm.Core.ViewModels;

namespace RxRealm.Pages;

public partial class PaginatedProductsPage
{
    public PaginatedProductsPage()
    {
        InitializeComponent();
        ViewModel = ServiceLocator.Services.GetRequiredService<PaginatedProductsViewModel>();
        ProductsCollectionView.ItemTemplate = new DataTemplate(() =>
        {
            var productCell = new ProductCell(Activator);
            productCell.DisposeWith(Disposables);
            return productCell;
        });

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.Products, v => v.ProductsCollectionView.ItemsSource)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.IsBusy, v => v.BusyIndicator.IsRunning)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel,
                             vm => vm.LoadMore,
                             v => v.ProductsCollectionView,
                             toEvent:nameof(DXCollectionView.LoadMore))
                .DisposeWith(disposables);

            this.WhenAnyObservable(v => v.ViewModel!.LoadMore.IsExecuting)
                .Do(isRefreshing => Debug.WriteLine($"IsRefreshing: {isRefreshing}"))
                .BindTo(this, v => v.ProductsCollectionView.IsRefreshing)
                .DisposeWith(disposables);

            this.WhenAnyObservable(v => v.ViewModel!.LoadMore.CanExecute)
                .BindTo(this, v => v.ProductsCollectionView.IsLoadMoreEnabled)
                .DisposeWith(disposables);
        });
    }

    private void ProductsCollectionView_SelectionChanged(object? sender, CollectionViewSelectionChangedEventArgs e)
    {
        if (e.AddedItems.FirstOrDefault() is ProductViewModel product)
        {
            Navigation.PushAsync(new ProductDetailsPage(product.Id));
        }
    }
}
