using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;

namespace RxRealm.Pages;

public partial class ProductDetailsView
{
    public ProductDetailsView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.Name, v => v.ProductNameLabel.Text)
                .DisposeWith(disposables);
            this.OneWayBind(ViewModel,
                            vm => vm.Price,
                            v => v.ProductPriceLabel.Text,
                            price => price.ToString("C"))
                .DisposeWith(disposables);
            this.WhenAnyValue(v => v.ViewModel!.ImageUri)
                .Do(_ => ProductImage.Source = null)
                .WhereNotNull()
                .Select(imageUrl => new UriImageSource { Uri = imageUrl })
                .BindTo(this, v => v.ProductImage.Source)
                .DisposeWith(disposables);
        });
    }

    private void ShowDetailsClicked(object? sender, EventArgs e)
    {
        if (ViewModel?.Product != null)
        {
            Navigation.PushAsync(new ProductDetailsPage(ViewModel.Product.Id));
        }
    }
}
