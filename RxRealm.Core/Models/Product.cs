using Realms;

namespace RxRealm.Core.Models;

public partial class Product : IRealmObject
{
    [MapTo("_id")]
    [PrimaryKey]
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }
}