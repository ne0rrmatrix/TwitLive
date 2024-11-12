using System.Timers;
using CommunityToolkit.Maui.Core.Primitives;
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
	IDb? db;
	readonly ILogger logger = LoggerFactory.GetLogger(nameof(VideoPlayerPage));
	public VideoPlayerPage(VideoPlayerViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
		WeakReferenceMessenger.Default.Register<NavigationMessage>(this, (r, m) => HandleMessage(m));
	}
	void HandleMessage(NavigationMessage message)
	{
		if (message.Value is true && message.Show is null && mediaElement is not null)
		{
			if(mediaElement.CurrentState == MediaElementState.Playing)
			{
				mediaElement.Pause();
				logger.Info("Navigation event. Stopping Timer.");
			}
			StopTimer();
		}
	}
	async void MediaElement_MediaOpened(object? sender, EventArgs e)
	{
		logger.Info("Media opened");
		show = ((VideoPlayerViewModel)BindingContext).Show;
		db = ((VideoPlayerViewModel)BindingContext).Db;
		
		StopTimer();
		var result = await db.GetShowAsync(show).ConfigureAwait(true);
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
		if (show is null || db is null)
		{
			logger.Info("Show or db is null");
			return;
		}
		if (!string.IsNullOrEmpty(show.Title))
		{
			show.Position = (int)mediaElement.Position.TotalSeconds;
			await db.SaveShowAsync(show);
			logger.Info($"Show: {show.Title} at Position: {show.Position}");
		}
		else
		{
			logger.Info("Show title is empty");
		}
	}

	void StartTimer()
	{
		if (timer is not null)
		{
			timer.Stop();
			timer.Dispose();
		}
		timer = new Timer(5000);
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