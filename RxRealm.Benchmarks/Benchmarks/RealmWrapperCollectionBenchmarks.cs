using System.Reactive.Disposables;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Realms;
using RxRealm.Benchmarks.Services;
using RxRealm.Core.Models;
using RxRealm.Core.Services;
using RxRealm.Core.ViewModels;

namespace RxRealm.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RunStrategy.ColdStart, iterationCount:5)]
public class RealmWrapperCollectionBenchmarks
{
    private readonly CompositeDisposable _disposables = new();
    private ProductsService _productsService = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        _productsService = new ProductsService(new FileSystemService());
        await _productsService.InstallDatabaseIfNecessaryAsync();
    }

    [Benchmark]
    public async Task DeleteSingleProduct()
    {
        var products = _productsService.Realm.All<Product>().AsRealmCollection();
        var productsWrapperCollection = new RealmWrapperCollection<Product, ProductViewModel, Guid>(products, p => new ProductViewModel(p));
        var loadedProducts = productsWrapperCollection.Take(50).ToList();
        var initialCount = productsWrapperCollection.Count;
        var productToDelete = products[products.Count / 2];
        await _productsService.RemoveAsync(productToDelete);
        var finalCount = productsWrapperCollection.Count;
        Console.WriteLine($"Initial count: {initialCount}, final count: {finalCount}");
    }

    [Benchmark]
    public async Task AddSingleProduct()
    {
        var products =  await _productsService.GetProductsAsync(products =>
                                                               {
                                                                   return products.OrderBy(p => p.Price);
                                                               });
        var productsWrapperCollection = new RealmWrapperCollection<Product, ProductViewModel, Guid>(products, p => new ProductViewModel(p));
        var loadedProducts = productsWrapperCollection.Take(50).ToList();
        var initialCount = productsWrapperCollection.Count;
        var productToAdd = new Product
        {
            Id = Guid.NewGuid(),
            Name = Guid.NewGuid().ToString(),
            Price = (products[products.Count / 2].Price + products[products.Count / 2 + 1].Price) / 2
        };
        await _productsService.AddAsync(productToAdd);
        var finalCount = productsWrapperCollection.Count;
        Console.WriteLine($"Initial count: {initialCount}, final count: {finalCount}");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _disposables.Dispose();
    }
}
