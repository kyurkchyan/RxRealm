namespace RxRealm.Core.ViewModels;

public interface IModelWrapperViewModel<out T>
{
    public T Model { get; }
}
