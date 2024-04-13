using Microsoft.Extensions.DependencyInjection;
using RxRealm.Core.Services;
using RxRealm.Core.ViewModels;

namespace RxRealm.Core;

public static class Startup
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        return services.AddTransient<ProductsViewModel>()
            .AddTransient<ProductsService>();
    }
}