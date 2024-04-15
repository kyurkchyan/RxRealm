using ReactiveUI;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;
using Splat.ModeDetection;

namespace RxRealm.Reactive;

public static class Startup
{
    public static IServiceCollection ConfigureReactiveUI(this IServiceCollection services)
    {
        ModeDetector.OverrideModeDetector(Mode.Run);
        services.UseMicrosoftDependencyResolver();
        IMutableDependencyResolver resolver = Locator.CurrentMutable;
        resolver.InitializeSplat();
        resolver.InitializeReactiveUI();

        return services
            .AddTransient<IActivationForViewFetcher, ActivationForViewFetcher>();
    }
}
