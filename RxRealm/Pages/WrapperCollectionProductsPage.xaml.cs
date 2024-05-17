using System.Reactive.Disposables;
using ReactiveUI;
using RxRealm.Core;
using RxRealm.Core.ViewModels;

namespace RxRealm.Pages;

public partial class WrapperCollectionProductsPage
{
    public WrapperCollectionProductsPage()
    {
        InitializeComponent();
        ViewModel = ServiceLocator.Services.GetRequiredService<WrapperCollectionProductsViewModel>();
        ProductsCollectionView.ItemTemplate = new DataTemplate(() =>
        {
            var productCell = new ProductCell(Activator);
            productCell.DisposeWith(Disposables);
            return productCell;
        });
        
        ProductsCollectionView.SelectedItemTemplate = new DataTemplate(() =>
        {
            var productCell = new ProductCell(Activator);
            productCell.BackgroundColor = Colors.Gray;
            productCell.DisposeWith(Disposables);
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
