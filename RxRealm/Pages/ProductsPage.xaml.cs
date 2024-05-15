using System.Reactive.Disposables;
using DevExpress.Maui.CollectionView;
using ReactiveUI;
using RxRealm.Core;
using RxRealm.Core.ViewModels;

namespace RxRealm.Pages;

public partial class ProductsPage
{
    public ProductsPage()
    {
        InitializeComponent();
        ViewModel = ServiceLocator.Services.GetRequiredService<ProductsViewModel>();
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
