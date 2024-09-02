using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using MetroLog;
using MetroLog.Operators;
using MetroLog.Targets;
using TwitLive.Database;
using TwitLive.Interfaces;
using TwitLive.Primitives;
using TwitLive.ViewModels;
using TwitLive.Views;
using LoggerFactory = MetroLog.LoggerFactory;
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
			.UseMauiCommunityToolkit(options =>
			{
				options.SetShouldEnableSnackbarOnWindows(true);
				options.SetShouldSuppressExceptionsInBehaviors(true);
			})
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
#if ANDROID
			handlers.AddHandler(typeof(Shell), typeof(TwitLive.Platforms.Android.CustomShellRenderer));
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
		builder.Services.AddSingleton<PodcastPage>();
		builder.Services.AddSingleton<PodcastPageViewModel>();

		builder.Services.AddSingleton<ShowPage>();
		builder.Services.AddSingleton<ShowPageViewModel>();

		builder.Services.AddTransient<VideoPlayerPage>();
		builder.Services.AddSingleton<VideoPlayerViewModel>();

		builder.Services.AddSingleton<DownloadsPage>();
		builder.Services.AddSingleton<DownloadsPageViewModel>();

		builder.Services.AddSingleton<BasePageViewModel>();
		builder.Services.AddSingleton(LogOperatorRetriever.Instance);
		builder.Services.AddSingleton<IDownload, DownloadManager>();
		builder.Services.AddSingleton<IDb, Db>();
		return builder.Build();
	}
}