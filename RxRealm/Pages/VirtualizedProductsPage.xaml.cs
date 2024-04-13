using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using RxRealm.Core;
using RxRealm.Core.ViewModels;
using DevExpress.Maui.CollectionView;
using ReactiveUI;

namespace RxRealm.Pages;

public partial class VirtualizedProductsPage
{
    public VirtualizedProductsPage()
    {
        InitializeComponent();
        ViewModel = ServiceLocator.Services.GetRequiredService<VirtualizedProductsViewModel>();
        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.Products, v => v.ProductsCollectionView.ItemsSource)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.IsBusy, v => v.BusyIndicator.IsRunning)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.LoadMore, v => v.ProductsCollectionView,
                    toEvent: nameof(DXCollectionView.LoadMore))
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
