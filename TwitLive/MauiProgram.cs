using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using Microsoft.Extensions.Logging;
using TwitLive.ViewModels;
using TwitLive.Views;
#if WINDOWS
using TwitLive.Handlers;
#endif

namespace TwitLive;
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>().ConfigureFonts(fonts =>
        {
            fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
        }).UseMauiCommunityToolkitMediaElement().UseMauiCommunityToolkitCore().UseMauiCommunityToolkit();
        builder.ConfigureMauiHandlers(handlers =>
        {
#if WINDOWS
            handlers.AddHandler<RefreshView, MauiRefreshViewHandler>();
#endif
        });
#if DEBUG
        builder.Logging.AddDebug();
#endif
        builder.Services.AddTransient<PodcastPage>();
        builder.Services.AddTransient<PodcastPageViewModel>();

        builder.Services.AddTransient<ShowPage>();
        builder.Services.AddTransient<ShowPageViewModel>();
#if WINDOWS
        builder.Services.AddSingleton<VideoPlayerPage>();
#else
        builder.Services.AddTransient<VideoPlayerPage>();
#endif
        builder.Services.AddSingleton<VideoPlayerViewModel>();
        
        return builder.Build();
    }
}