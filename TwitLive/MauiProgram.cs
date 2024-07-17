using TwitLive.Primitives;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using Microsoft.Extensions.Logging;
using TwitLive.Interfaces;
using TwitLive.ViewModels;
using TwitLive.Views;
using TwitLive.Database;

#if WINDOWS
using TwitLive.Handlers;
#endif

namespace TwitLive;
public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiCommunityToolkitMediaElement()
			.UseMauiCommunityToolkitCore()
			.UseMauiCommunityToolkit()
			.UseMauiApp<App>().ConfigureFonts(fonts =>
		{
			fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			fonts.AddFont("Roboto-Regular.ttf", "RobotoRegular");
		});
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
		builder.Services.AddSingleton<PodcastPageViewModel>();

		builder.Services.AddTransient<ShowPage>();
		builder.Services.AddTransient<ShowPageViewModel>();
#if WINDOWS
        builder.Services.AddSingleton<VideoPlayerPage>();
#else
		builder.Services.AddTransient<VideoPlayerPage>();
#endif
		builder.Services.AddSingleton<VideoPlayerViewModel>();

		builder.Services.AddTransient<DownloadsPage>();
		builder.Services.AddTransient<DownloadsPageViewModel>();

		builder.Services.AddSingleton<BasePageViewModel>();
		builder.Services.AddSingleton<IDownload, DownloadManager>();
		builder.Services.AddSingleton<IDb, Db>();
		return builder.Build();
	}
}