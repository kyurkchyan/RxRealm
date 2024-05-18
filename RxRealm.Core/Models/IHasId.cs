namespace RxRealm.Core.Models;

public interface IHasId<out TId>
{
    public TId Id { get; }
}
