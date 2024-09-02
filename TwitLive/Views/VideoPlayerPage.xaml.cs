using System.Timers;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.Messaging;
using MetroLog;
using TwitLive.Interfaces;
using TwitLive.Models;
using TwitLive.Primitives;
using TwitLive.ViewModels;
using Timer = System.Timers.Timer;

namespace TwitLive.Views;

public partial class VideoPlayerPage : ContentPage, IDisposable
{
	Timer? timer;
	bool disposedValue;

	Show? show;
	IDb? item;
	readonly ILogger logger = LoggerFactory.GetLogger(nameof(VideoPlayerPage));
	public VideoPlayerPage(VideoPlayerViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
		WeakReferenceMessenger.Default.Register<NavigationMessage>(this, (r, m) => HandleMessage(m));
	}
	protected override void OnNavigatedTo(NavigatedToEventArgs args)
	{
		base.OnNavigatedTo(args);
#if IOS || ANDROID || MACCATALYST
		if (Application.Current?.PlatformAppTheme == AppTheme.Dark)
		{
			CommunityToolkit.Maui.Core.Platform.StatusBar.SetColor(Colors.Black);
			CommunityToolkit.Maui.Core.Platform.StatusBar.SetStyle(StatusBarStyle.LightContent);
		}
		else
		{
			CommunityToolkit.Maui.Core.Platform.StatusBar.SetColor(Color.FromArgb("#E9E9E9"));
			CommunityToolkit.Maui.Core.Platform.StatusBar.SetStyle(StatusBarStyle.DarkContent);
		}
#endif
	}
	void HandleMessage(NavigationMessage message)
	{
		if (message.Show is not null)
		{
			logger.Info("Did not navigate to video player. Not resetting videoplayer timer.");
			return;
		}
		logger.Info("Resetting timer in video player");
		StopTimer();
		mediaElement.MediaOpened -= MediaElement_MediaOpened;
		if (BindingContext is not VideoPlayerViewModel currentShow)
		{
			logger.Info("BindingContext is not VideoPlayerViewModel");
			show = null;
			item = null;
			return;
		}
		logger.Info($"Navigated to video player with show: {currentShow.Show.Title}");
		show = currentShow.Show;
		item = currentShow.Db;
		mediaElement.MediaOpened += MediaElement_MediaOpened;
	}
	
	async void MediaElement_MediaOpened(object? sender, EventArgs e)
	{
		ArgumentNullException.ThrowIfNull(show);
		ArgumentNullException.ThrowIfNull(item);
		logger.Info("Media opened");
		var result = await item.GetShowAsync(show).ConfigureAwait(true);
		if (result is null)
		{
			StartTimer();
			logger.Info("Previous position not found");
		}
		else if (result.Position is > 0)
		{
			logger.Info($"Loading previous position: {result.Position}");
			Application.Current?.Dispatcher.Dispatch(async () => 
			{
				await mediaElement.SeekTo(TimeSpan.FromSeconds(result.Position)).ConfigureAwait(true);
				StartTimer();
			});
		}
		else
		{
			logger.Info("Error: Neither show nor position found!");
		}
	}
	
	async void UpdatePlayedTime(object? sender, ElapsedEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(show);
		ArgumentNullException.ThrowIfNull(item);
		if (!string.IsNullOrEmpty(show.Title))
		{
			show.Position = (int)mediaElement.Position.TotalSeconds;
			await item.SaveShowAsync(show);
			logger.Info($"Show: {show.Title} at Position: {show.Position}");
		}
		else
		{
			logger.Info("Show title is empty");
		}
	}

	static void Webview_Navigating(System.Object sender, Microsoft.Maui.Controls.WebNavigatingEventArgs e)
	{
		if (e.Url.Contains("https://") || e.Url.Contains("http://"))
		{
			e.Cancel = true;
		}
	}

	void StartTimer()
	{
		if (timer is not null)
		{
			timer.Stop();
			timer.Dispose();
		}
		timer = new System.Timers.Timer(5000);
		timer.Elapsed += UpdatePlayedTime;
		timer.Start();
	}

	void StopTimer()
	{
		if (timer is not null)
		{
			timer.Elapsed -= UpdatePlayedTime;
			timer.Stop();
			timer.Dispose();
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			mediaElement.MediaOpened -= MediaElement_MediaOpened;
			if (disposing && timer is not null)
			{
				timer.Elapsed -= UpdatePlayedTime;
				timer.Stop();
				timer.Dispose();
			}
			disposedValue = true;
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}