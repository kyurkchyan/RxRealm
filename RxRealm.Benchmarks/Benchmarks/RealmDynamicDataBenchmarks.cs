using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using DynamicData;
using Realms;
using RxRealm.Benchmarks.Services;
using RxRealm.Core.Models;
using RxRealm.Core.Services;
using RxRealm.Core.ViewModels;

namespace RxRealm.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RunStrategy.ColdStart, iterationCount: 5)]
public class RealmDynamicDataBenchmarks
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
    public void LoadProductsFromRealm()
    {
        var products = _productsService.Realm.All<Product>().AsRealmCollection();
        Debug.WriteLine(products.Count);
    }

    [Benchmark]
    public void LoadProductsFromRealm_ToList()
    {
        var products = _productsService.Realm.All<Product>().ToList();
        Debug.WriteLine(products.Count);
    }

    [Benchmark]
    public void LoadProductsWith_Virtualization_After_Filtration_Transformation()
    {
        var products = _productsService.Realm.All<Product>().AsRealmCollection()
            .AsObservableChangeSet()
            .Virtualise(Observable.Return(new VirtualRequest(0, 50)))
            .Filter(item => item.IsValid)
            .Transform(p => new ProductViewModel(p));
        products.Subscribe(c => Console.WriteLine($"________________ Products count changed: {c.Count}"))
            .DisposeWith(_disposables);
    }

    [Benchmark]
    public void LoadProductsWith_Virtualization_Transformation()
    {
        var products = _productsService.Realm.All<Product>().AsRealmCollection()
            .AsObservableChangeSet()
            .Virtualise(Observable.Return(new VirtualRequest(0, 50)))
            .Transform(p => new ProductViewModel(p));
        products.Subscribe(c => Console.WriteLine($"________________ Products count changed: {c.Count}"))
            .DisposeWith(_disposables);
    }

    [Benchmark]
    public async Task LoadFirstPageOfProducts()
    {
        var products = await _productsService.GetProductsAsync(query => query, 50);
        await products.LoadNextPage().ToTask();
        products.Items.Connect()
            .Subscribe(c => Console.WriteLine($"________________ Products count changed: {c.Count}"))
            .DisposeWith(_disposables);
    }


    [Benchmark]
    public async Task Load10PagesOfProducts()
    {
        var products = await _productsService.GetProductsAsync(query => query, 50);
        products.Items.Connect()
            .Subscribe(c => Console.WriteLine($"________________ Products count changed: {c.Count}"))
            .DisposeWith(_disposables);
        for (var i = 0; i < 10; i++)
        {
            await products.LoadNextPage().ToTask();
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _disposables.Dispose();
    }
}