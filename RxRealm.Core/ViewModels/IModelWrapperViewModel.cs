using RxRealm.Core.Models;

namespace RxRealm.Core.ViewModels;

public interface IModelWrapperViewModel<out TModel, out TId> where TModel : IHasId<TId>
{
    TModel Model { get; }
    public TId Id { get; }
}
