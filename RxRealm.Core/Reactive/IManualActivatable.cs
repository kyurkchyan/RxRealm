using ReactiveUI;

namespace RxRealm.Core.Reactive;

public interface IManualActivatable : ICanActivate
{
    public void Activate();
    public void Deactivate();
}
