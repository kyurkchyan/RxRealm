using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Threading.Tasks;
using Bogus;
using DynamicData;
using FluentAssertions;
using FluentAssertions.Execution;
using Nito.AsyncEx;
using Realms;
using RxRealm.Core.Models;

namespace RxRealm.Core.IntegrationTests;

public class PaginatedRealmResultsShould : IAsyncLifetime
{
    private readonly CompositeDisposable _disposables = new();
    private readonly RealmConfiguration _configuration;

    public PaginatedRealmResultsShould()
    {
        _configuration = new RealmConfiguration(Path.Combine(Directory.GetCurrentDirectory(), $"{Guid.NewGuid()}.realm")) { Schema = new[] { typeof(Product) } };
        Disposable.Create(() =>
        {
            Realm.DeleteRealm(_configuration);
        });
    }

    public async Task InitializeAsync()
    {
        using var realm = GetRealm();
        var price = 1;
        var faker = new Faker<Product>()
                    .RuleFor(p => p.Id, Guid.NewGuid)
                    .RuleFor(p => p.Name, f => f.Commerce.ProductName())
                    .RuleFor(p => p.Price, f => price++)
                    .RuleFor(p => p.ImageUrl, f => f.Image.PicsumUrl());
        var products = faker.Generate(100);
        await realm.WriteAsync(() => realm.Add(products));
    }

    [Fact]
    public void LoadTwoPagesOfProducts()
    {
        AsyncContext.Run(async () =>
        {
            using var realm = GetRealm();
            const int pageSize = 50;
            var paginatedProducts = new PaginatedRealmResults<Product>(realm.All<Product>().OrderBy(p => p.Price).AsRealmCollection());
            paginatedProducts.DisposeWith(_disposables);
            paginatedProducts.Items
                             .Connect()
                             .Bind(out ReadOnlyObservableCollection<Product> products)
                             .Subscribe()
                             .DisposeWith(_disposables);

            await paginatedProducts.LoadNextPage().ToTask();
            await paginatedProducts.LoadNextPage().ToTask();

            using (new AssertionScope())
            {
                products.Should().NotBeNull();
                products.Should().HaveCount(2 * pageSize);
                var firstTwoPages = realm.All<Product>().OrderBy(p => p.Price).AsRealmCollection().Take(2 * pageSize);
                products.Should().BeEquivalentTo(firstTwoPages, options => options.Including(p => p.Id));
            }
        });
    }

    [Fact]
    public void RemoveProductFromObservableCollection_WhenProductIsRemovedFromDb_InsideTheLoadedPage()
    {
        AsyncContext.Run(async () =>
        {
            using var realm = GetRealm();
            const int pageSize = 50;
            var paginatedProducts = new PaginatedRealmResults<Product>(realm.All<Product>().OrderBy(p => p.Price).AsRealmCollection());
            paginatedProducts.DisposeWith(_disposables);
            paginatedProducts.Items
                             .Connect()
                             .Bind(out ReadOnlyObservableCollection<Product> products)
                             .Subscribe()
                             .DisposeWith(_disposables);
            await paginatedProducts.LoadNextPage().ToTask();

            var productToDelete = products[products.Count / 2];
            var deletedId = productToDelete.Id;
            await realm.WriteAsync(() => realm.Remove(productToDelete));

            await Extensions.RetryWithExponentialBackoff(() =>
            {
                using (new AssertionScope())
                {
                    products.Should().NotBeNull();
                    products.Should().HaveCount(pageSize - 1);
                    products.FirstOrDefault(p => p.Id == deletedId).Should().BeNull();
                }
            });
        });
    }

    [Fact]
    public void RemoveRangeOfProductsFromObservableCollection_WhenProductsAreRemovedFromDb_InsideTheLoadedPage()
    {
        AsyncContext.Run(async () =>
        {
            using var realm = GetRealm();
            const int pageSize = 50;
            var paginatedProducts = new PaginatedRealmResults<Product>(realm.All<Product>().OrderBy(p => p.Price).AsRealmCollection());
            paginatedProducts.DisposeWith(_disposables);
            paginatedProducts.Items
                             .Connect()
                             .Bind(out ReadOnlyObservableCollection<Product> products)
                             .Subscribe()
                             .DisposeWith(_disposables);
            await paginatedProducts.LoadNextPage().ToTask();

            var productsToDelete = products.Skip(5).Take(10).ToArray();
            var deletedIds = productsToDelete.Select(p => p.Id).ToArray();
            var filter = $"_id IN {{{string.Join(",", deletedIds.Select(id => $"uuid({id})"))}}}";
            await realm.WriteAsync(() => realm.RemoveRange(realm.All<Product>().Filter(filter)));

            await Extensions.RetryWithExponentialBackoff(() =>
            {
                using (new AssertionScope())
                {
                    products.Should().NotBeNull();
                    products.Should().HaveCount(pageSize - deletedIds.Length);
                    products.Any(p => deletedIds.Any(id => id == p.Id)).Should().BeFalse();
                }
            });
        });
    }

    [Fact]
    public void RemoveOnlyRangeOfProductsThatAreInsideLoadedObservableCollection_WhenProductsAreRemovedFromDb_ThatHaveItemsOutsideTheLoadedPage()
    {
        AsyncContext.Run(async () =>
        {
            using var realm = GetRealm();
            var paginatedProducts = new PaginatedRealmResults<Product>(realm.All<Product>().OrderBy(p => p.Price).AsRealmCollection());
            paginatedProducts.DisposeWith(_disposables);
            paginatedProducts.Items
                             .Connect()
                             .Bind(out ReadOnlyObservableCollection<Product> products)
                             .Subscribe()
                             .DisposeWith(_disposables);
            await paginatedProducts.LoadNextPage().ToTask();

            var productsToDelete = realm.All<Product>().AsRealmCollection().Skip(40).Take(20).ToArray();
            var deletedIds = productsToDelete.Select(p => p.Id).ToArray();
            var filter = $"_id IN {{{string.Join(",", deletedIds.Select(id => $"uuid({id})"))}}}";
            await realm.WriteAsync(() => realm.RemoveRange(realm.All<Product>().Filter(filter)));

            await Extensions.RetryWithExponentialBackoff(() =>
            {
                using (new AssertionScope())
                {
                    products.Should().NotBeNull();
                    products.Should().HaveCount(40);
                    products.Any(p => deletedIds.Any(id => id == p.Id)).Should().BeFalse();
                }
            });
        });
    }

    [Fact]
    public void DoNothing_WhenProductsAreRemovedFromDb_OutsideOfTheLoadedPage()
    {
        AsyncContext.Run(async () =>
        {
            using var realm = GetRealm();
            var paginatedProducts = new PaginatedRealmResults<Product>(realm.All<Product>().OrderBy(p => p.Price).AsRealmCollection());
            paginatedProducts.DisposeWith(_disposables);
            paginatedProducts.Items
                             .Connect()
                             .Bind(out ReadOnlyObservableCollection<Product> products)
                             .Subscribe()
                             .DisposeWith(_disposables);
            await paginatedProducts.LoadNextPage().ToTask();

            var productsToDelete = realm.All<Product>().AsRealmCollection().Skip(60).Take(20).ToArray();
            var deletedIds = productsToDelete.Select(p => p.Id).ToArray();
            var filter = $"_id IN {{{string.Join(",", deletedIds.Select(id => $"uuid({id})"))}}}";
            await realm.WriteAsync(() => realm.RemoveRange(realm.All<Product>().Filter(filter)));

            await Extensions.RetryWithExponentialBackoff(() =>
            {
                using (new AssertionScope())
                {
                    products.Should().NotBeNull();
                    products.Should().HaveCount(50);
                }
            });
        });
    }

    [Fact]
    public void AddProductToObservableCollection_WhenProductIsAddedInDb_InsideTheLoadedPage()
    {
        AsyncContext.Run(async () =>
        {
            using var realm = GetRealm();
            const int pageSize = 50;
            var paginatedProducts = new PaginatedRealmResults<Product>(realm.All<Product>().OrderBy(p => p.Price).AsRealmCollection());
            paginatedProducts.DisposeWith(_disposables);
            paginatedProducts.Items
                             .Connect()
                             .Bind(out ReadOnlyObservableCollection<Product> products)
                             .Subscribe()
                             .DisposeWith(_disposables);
            await paginatedProducts.LoadNextPage().ToTask();

            var productToAdd = new Product
            {
                Id = Guid.NewGuid(),
                Name = "New Product",
                Price = (products[products.Count / 2].Price + products[products.Count / 2 + 1].Price) / 2
            };
            await realm.WriteAsync(() => realm.Add(productToAdd));

            await Extensions.RetryWithExponentialBackoff(() =>
            {
                using (new AssertionScope())
                {
                    products.Should().NotBeNull();
                    products.Should().HaveCount(pageSize + 1);
                    Product? addedProduct = products.FirstOrDefault(p => p.Id == productToAdd.Id);
                    addedProduct.Should().NotBeNull();
                    addedProduct!.Name.Should().Be(productToAdd.Name);
                    addedProduct.Price.Should().Be(productToAdd.Price);
                }
            });
        });
    }

    [Fact]
    public void AddRangeOfProductsToObservableCollection_WhenProductsAreAddedInDb_InsideTheLoadedPage()
    {
        AsyncContext.Run(async () =>
        {
            using var realm = GetRealm();
            const int pageSize = 50;
            var paginatedProducts = new PaginatedRealmResults<Product>(realm.All<Product>().OrderBy(p => p.Price).AsRealmCollection());
            paginatedProducts.DisposeWith(_disposables);
            paginatedProducts.Items
                             .Connect()
                             .Bind(out ReadOnlyObservableCollection<Product> products)
                             .Subscribe()
                             .DisposeWith(_disposables);
            await paginatedProducts.LoadNextPage().ToTask();

            const int newItemsCount = 10;
            decimal newPriceStart = products[products.Count / 2].Price;
            decimal newPriceIncrease = (products[products.Count / 2 + 1].Price - newPriceStart) / newItemsCount;
            var productsToAdd = Enumerable.Range(0, newItemsCount)
                                          .Select(i => new Product
                                          {
                                              Id = Guid.NewGuid(),
                                              Name = $"New Product {i}",
                                              Price = newPriceStart + i * newPriceIncrease
                                          })
                                          .ToArray();

            await realm.WriteAsync(() => realm.Add(productsToAdd));

            await Extensions.RetryWithExponentialBackoff(() =>
            {
                using (new AssertionScope())
                {
                    products.Should().NotBeNull();
                    products.Should().HaveCount(pageSize + newItemsCount);
                    foreach (Product product in productsToAdd)
                    {
                        Product? addedProduct = products.FirstOrDefault(p => p.Id == product.Id);
                        addedProduct.Should().NotBeNull();
                        addedProduct!.Name.Should().Be(product.Name);
                        addedProduct.Price.Should().Be(product.Price);
                    }
                }
            });
        });
    }

    [Fact]
    public void DoNothing_WhenProductsAreAddedInDb_OutsideOfTheLoadedPage()
    {
        AsyncContext.Run(async () =>
        {
            using var realm = GetRealm();
            const int pageSize = 50;
            var paginatedProducts = new PaginatedRealmResults<Product>(realm.All<Product>().OrderBy(p => p.Price).AsRealmCollection());
            paginatedProducts.DisposeWith(_disposables);
            paginatedProducts.Items
                             .Connect()
                             .Bind(out ReadOnlyObservableCollection<Product> products)
                             .Subscribe()
                             .DisposeWith(_disposables);
            await paginatedProducts.LoadNextPage().ToTask();

            const int newItemsCount = 10;
            decimal maxPrice = products.Last().Price;
            decimal newPriceStart = maxPrice + 10;
            var productsToAdd = Enumerable.Range(0, newItemsCount)
                                          .Select(i => new Product
                                          {
                                              Id = Guid.NewGuid(),
                                              Name = $"New Product {i}",
                                              Price = newPriceStart + i
                                          })
                                          .ToArray();

            await realm.WriteAsync(() => realm.Add(productsToAdd));

            await Extensions.RetryWithExponentialBackoff(() =>
            {
                using (new AssertionScope())
                {
                    products.Should().NotBeNull();
                    products.Should().HaveCount(pageSize);

                    foreach (Product product in productsToAdd)
                    {
                        Product? addedProduct = products.FirstOrDefault(p => p.Id == product.Id);
                        addedProduct.Should().BeNull();
                    }
                }
            });
        });
    }

    // [Fact]
    // public void DoNothing_WhenProductsAreRemovedFromDb_OutsideOfTheLoadedPage()
    // {
    //     AsyncContext.Run(async () =>
    //     {
    //         using var realm = GetRealm();
    //         var paginatedProducts = new PaginatedRealmResults<Product>(realm.All<Product>().OrderBy(p => p.Price).AsRealmCollection());
    //         paginatedProducts.DisposeWith(_disposables);
    //         paginatedProducts.Items
    //                          .Connect()
    //                          .Bind(out ReadOnlyObservableCollection<Product> products)
    //                          .Subscribe()
    //                          .DisposeWith(_disposables);
    //         await paginatedProducts.LoadNextPage().ToTask();
    //
    //         var productsToDelete = realm.All<Product>().AsRealmCollection().Skip(60).Take(20).ToArray();
    //         var deletedIds = productsToDelete.Select(p => p.Id).ToArray();
    //         var filter = $"_id IN {{{string.Join(",", deletedIds.Select(id => $"uuid({id})"))}}}";
    //         await realm.WriteAsync(() => realm.RemoveRange(realm.All<Product>().Filter(filter)));
    //
    //         await Extensions.RetryWithExponentialBackoff(() =>
    //         {
    //             using (new AssertionScope())
    //             {
    //                 products.Should().NotBeNull();
    //                 products.Should().HaveCount(50);
    //             }
    //         });
    //     });
    // }

    private Realm GetRealm() => Realm.GetInstance(_configuration);

    public Task DisposeAsync()
    {
        _disposables.Dispose();
        return Task.CompletedTask;
    }
}
