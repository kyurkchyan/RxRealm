using ReactiveUI;

namespace RxRealm.Reactive;

public interface ICustomActivatableView : IActivatableView, ICanActivate
{
    public void Activate();
    public void Deactivate();
}
