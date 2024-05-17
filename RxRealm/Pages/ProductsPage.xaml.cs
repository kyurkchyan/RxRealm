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
        ProductsCollectionView.ItemTemplate = new DataTemplate(() =>
        {
            var productCell = new ProductCell();
            return productCell;
        });
        
        ProductsCollectionView.SelectedItemTemplate = new DataTemplate(() =>
        {
            var productCell = new ProductCell { BackgroundColor = Colors.Gray };
            return productCell;
        });

        this.WhenActivated(disposables =>
        {
            this.BindCommand(ViewModel, vm => vm.Add, v => v.AddButton)
                .DisposeWith(Disposables);

            this.BindCommand(ViewModel, vm => vm.Remove, v => v.RemoveButton)
                .DisposeWith(Disposables);

            this.BindCommand(ViewModel, vm => vm.ChangeName, v => v.ChangeNameButton)
                .DisposeWith(Disposables);

            this.Bind(ViewModel, vm => vm.SelectedProduct, v => v.ProductsCollectionView.SelectedItem)
                .DisposeWith(Disposables);
            
            this.OneWayBind(ViewModel, vm => vm.Products, v => v.ProductsCollectionView.ItemsSource)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.IsBusy, v => v.BusyIndicator.IsRunning)
                .DisposeWith(disposables);
        });
    }
}
