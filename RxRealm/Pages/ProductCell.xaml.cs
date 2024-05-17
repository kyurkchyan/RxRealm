using RxRealm.Core.Models;

namespace RxRealm.Pages;

public partial class ProductCell
{
    public ProductCell()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (BindingContext is Product product)
        {
            ProductImage.Source = product.ImageUrl != null ? new UriImageSource { Uri = new Uri(product.ImageUrl) } : null;
            ProductNameLabel.Text = product.Name ?? "";
            ProductPriceLabel.Text = product.Price.ToString("C");
        }
    }
}
