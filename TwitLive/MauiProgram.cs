using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using Downloader;
using Microsoft.Extensions.Logging;
using TwitLive.ViewModels;
using TwitLive.Views;

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
        }).UseMauiCommunityToolkitMediaElement().UseMauiCommunityToolkitCore();
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
        builder.Services.AddSingleton<VideoPlayerPage>();
#endif
        builder.Services.AddSingleton<VideoPlayerViewModel>();
        
        return builder.Build();
    }
}