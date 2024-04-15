using System.Reactive.Disposables;
using RxRealm.Core.Services;
using RxRealm.Core.ViewModels;
using RxRealm.Reactive;
using RxRealm.Services;

namespace RxRealm.Pages;

public partial class ProductDetailsPage
{
    public ProductDetailsPage(Guid productId)
    {
        InitializeComponent();
        ProductDetailsView.BindActivationTo(Activator).DisposeWith(Disposables);
        ProductDetailsView.DisposeWith(Disposables);
        ViewModel = new ProductDetailsViewModel(productId, new ProductsService(new FileSystemService()));
    }
}
