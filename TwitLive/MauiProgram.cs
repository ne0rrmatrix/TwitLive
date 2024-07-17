using TwitLive.Primitives;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using Microsoft.Extensions.Logging;
using TwitLive.Interfaces;
using TwitLive.ViewModels;
using MetroLog.Targets;
using TwitLive.Views;
using TwitLive.Database;
using MetroLog.Operators;
using MetroLog;
using LoggerFactory = MetroLog.LoggerFactory;
using MetroLog.Maui;
using LogLevel = MetroLog.LogLevel;


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
		#region Logging
		var config = new LoggingConfiguration();

#if RELEASE
        config.AddTarget(
            LogLevel.Info,
            LogLevel.Fatal,
            new StreamingFileTarget(Path.Combine(
                FileSystem.CacheDirectory,
                "MetroLogs"), retainDays: 2));
#else
		// Will write logs to the Debug output
		config.AddTarget(
			LogLevel.Trace,
			LogLevel.Fatal,
			new TraceTarget());
#endif

		// will write logs to the console output (Logcat for android)
		config.AddTarget(
			LogLevel.Info,
			LogLevel.Fatal,
			new ConsoleTarget());

		config.AddTarget(
			LogLevel.Info,
			LogLevel.Fatal,
			new MemoryTarget(2048));

		LoggerFactory.Initialize(config);
		#endregion
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
		builder.Services.AddSingleton(LogOperatorRetriever.Instance);
		builder.Services.AddSingleton<IDownload, DownloadManager>();
		builder.Services.AddSingleton<IDb, Db>();
		return builder.Build();
	}
}