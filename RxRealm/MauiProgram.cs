using DevExpress.Maui;
using FFImageLoading.Maui;
using Microsoft.Extensions.Logging;
using RxRealm.Core;
using RxRealm.Core.Services;
using RxRealm.Reactive;
using RxRealm.Services;

namespace RxRealm;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .UseFFImageLoading()
            .UseDevExpress();

        builder.Services
               .AddTransient<IFileSystemService, FileSystemService>()
               .AddCore()
               .ConfigureReactiveUI();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();
        ServiceLocator.Services = app.Services;
        return app;
    }
}
