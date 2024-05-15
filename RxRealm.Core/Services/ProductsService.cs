using System.Reactive.Linq;
using System.Reactive.Subjects;
using Bogus;
using DynamicData;
using DynamicData.Binding;
using Realms;
using RxRealm.Core.Models;

namespace RxRealm.Core.Services;

public class ProductsService(IFileSystemService fileSystemService)
{
    public string DatabasePath { get; } = Path.Combine(fileSystemService.AppDataFolderPath, "products.realm");

    private Realm? _realm;
    public Realm Realm => _realm ??= GetRealm(DatabasePath);

    public static Realm GetRealm(string databasePath)
    {
        return Realm.GetInstance(databasePath);
    }

    public async Task<PaginatedResults<Product>> GetPaginatedProductsAsync(
        Func<IQueryable<Product>, IQueryable<Product>> expression,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        await InstallDatabaseIfNecessaryAsync(cancellationToken);
        var products = expression(Realm.All<Product>()).AsRealmCollection();

        return new PaginatedResults<Product>(pageSize,
                                             pagination
                                                 => Observable.Return(GetPaginatedResponse(products, pagination)));
    }

    public async Task<IConnectableObservable<IVirtualChangeSet<Product>>> GetVirtualizedProductsAsync(
        Func<IQueryable<Product>, IQueryable<Product>> expression,
        IObservable<IVirtualRequest> pagination,
        CancellationToken cancellationToken = default)
    {
        await InstallDatabaseIfNecessaryAsync(cancellationToken);
        return expression(Realm.All<Product>())
               .AsRealmCollection()
               .AsObservableChangeSet()
               .Virtualise(pagination)
               .Replay(1);
    }

    public async Task<IConnectableObservable<IChangeSet<Product>>> GetProductsChangesetAsync(
        Func<IQueryable<Product>, IQueryable<Product>> expression,
        CancellationToken cancellationToken = default)
    {
        await InstallDatabaseIfNecessaryAsync(cancellationToken);
        return expression(Realm.All<Product>())
               .AsRealmCollection()
               .AsObservableChangeSet()
               .Replay(1);
    }

    public async Task<IRealmCollection<Product>> GetProductsAsync(
        Func<IQueryable<Product>, IQueryable<Product>> expression,
        CancellationToken cancellationToken = default)
    {
        await InstallDatabaseIfNecessaryAsync(cancellationToken);
        var products = expression(Realm.All<Product>()).AsRealmCollection();
        return products;
    }

    public Product? GetProduct(Guid id) => Realm.Find<Product>(id);

    public async Task InstallDatabaseIfNecessaryAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(DatabasePath) && IsValidDatabase(DatabasePath))
        {
            return;
        }

        if (File.Exists(DatabasePath))
        {
            File.Delete(DatabasePath);
        }

        await using var source = await fileSystemService.OpenAppPackageFileAsync("products.realm", cancellationToken);
        await using var destinationStream = File.Create(DatabasePath);
        await source.CopyToAsync(destinationStream, cancellationToken);
    }

    private static bool IsValidDatabase(string databasePath)
    {
        try
        {
            using var realm = GetRealm(databasePath);
            return realm.All<Product>().Any();
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task CreateFakeProducts(int count, IProgress<int>? progress)
    {
        await Realm.WriteAsync(() => { Realm.RemoveAll<Product>(); });

        var price = 1;
        var faker = new Faker<Product>()
                    .RuleFor(p => p.Id, Guid.NewGuid)
                    .RuleFor(p => p.Name, f => f.Commerce.ProductName())
                    .RuleFor(p => p.Price, f => price++)
                    .RuleFor(p => p.ImageUrl, f => f.Image.PicsumUrl());
        var products = faker.Generate(count);

        var current = 0;
        const int chunkSize = 1000;
        foreach (var productsChunk in products.Chunk(chunkSize))
        {
            await Realm.WriteAsync(() =>
            {
                foreach (var product in productsChunk)
                {
                    Realm.Add(product);
                }
            });
            current += chunkSize;
            progress?.Report(current);
        }
    }

    private static PaginatedResponse<T> GetPaginatedResponse<T>(IReadOnlyCollection<T> items,
                                                                IVirtualRequest pagination)
    {
        var result = items
                     .Skip(pagination.StartIndex)
                     .Take(pagination.Size)
                     .ToList();
        return new PaginatedResponse<T>(result,
                                        result.Count,
                                        pagination.StartIndex,
                                        items.Count);
    }
}
