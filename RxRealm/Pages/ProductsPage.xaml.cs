using System.Reactive.Disposables;
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
        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.Products, v => v.ProductsCollectionView.ItemsSource)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.IsBusy, v => v.BusyIndicator.IsRunning)
                .DisposeWith(disposables);
        });
    }
}
