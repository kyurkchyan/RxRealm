using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using RxRealm.Core.Models;

namespace RxRealm.Core.ViewModels;

public class ProductViewModel : ReactiveObject, IDisposable, IActivatableViewModel, IModelWrapperViewModel<Product>
{
    public ProductViewModel(Product product)
    {
        Model = product;
        Id = product.Id;
        this.WhenActivated(disposables =>
        {
            product
                .WhenAnyValue(x => x.Name)
                .ToPropertyEx(this, x => x.Name)
                .DisposeWith(disposables);

            product
                .WhenAnyValue(x => x.ImageUrl)
                .Select(x => x != null ? new Uri(x) : null)
                .ToPropertyEx(this, x => x.ImageUri)
                .DisposeWith(disposables);

            product
                .WhenAnyValue(x => x.Price)
                .ToPropertyEx(this, x => x.Price)
                .DisposeWith(disposables);
        });
    }
    
    public Product Model { get; }

    public Guid Id { get; }

    [ObservableAsProperty] public string? Name { get; }
    [ObservableAsProperty] public Uri? ImageUri { get; }
    [ObservableAsProperty] public decimal Price { get; }

    public void Dispose()
    {
    }

    public ViewModelActivator Activator { get; } = new();
}
