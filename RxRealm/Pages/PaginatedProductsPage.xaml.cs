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
            var productCell = new ProductViewModelCell(Activator);
            productCell.DisposeWith(Disposables);
            return productCell;
        });
        ProductsCollectionView.SelectedItemTemplate = new DataTemplate(() =>
        {
            var productCell = new ProductViewModelCell(Activator);
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
}
