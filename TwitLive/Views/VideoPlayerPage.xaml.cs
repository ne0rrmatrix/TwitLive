using System.Timers;
using CommunityToolkit.Mvvm.Messaging;
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

	public VideoPlayerPage(VideoPlayerViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
		WeakReferenceMessenger.Default.Register<NavigationMessage>(this, (r, m) => HandleMessage());
	}
	
	public void HandleMessage()
	{
		StopTimer();
		mediaElement.MediaOpened -= MediaElement_MediaOpened;
		if (BindingContext is not VideoPlayerViewModel currentShow)
		{
			show = null;
			item = null;
			return;
		}
		mediaElement.MediaOpened += MediaElement_MediaOpened;
		show = currentShow.Show;
		item = currentShow.Db;
	}
	
	async void MediaElement_MediaOpened(object? sender, EventArgs e)
	{
		ArgumentNullException.ThrowIfNull(show);
		ArgumentNullException.ThrowIfNull(item);
		var result = await item.GetShowAsync(show.Title).ConfigureAwait(true);
		if (result is null)
		{
			StartTimer();
			System.Diagnostics.Debug.WriteLine("Position not found");
		}
		else if (result.Position is > 0)
		{
			System.Diagnostics.Debug.WriteLine($"Position loaded: {result.Position}");
			await MainThread.InvokeOnMainThreadAsync(async () => 
			{
				await mediaElement.SeekTo(TimeSpan.FromSeconds(result.Position)).ConfigureAwait(true); 
				StartTimer(); 
			});
		}
	}
	
	async void UpdatePlayedTime(object? sender, ElapsedEventArgs e)
	{
		if (item is not null && show is not null && !string.IsNullOrEmpty(show.Title))
		{
			show.Position = (int)mediaElement.Position.TotalSeconds;
			await item.SaveShowAsync(show);
			System.Diagnostics.Debug.WriteLine($"Show: {show.Title} at Position: {show.Position}");
		}
		else
		{
			System.Diagnostics.Debug.WriteLine("Item or Show is null");
		}
	}

	void Webview_Navigating(System.Object sender, Microsoft.Maui.Controls.WebNavigatingEventArgs e)
	{
		if (e.Url.Contains("https://") || e.Url.Contains("http://"))
		{
			e.Cancel = true;
		}
	}

	public void StartTimer()
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

	public void StopTimer()
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
			if (disposing && timer is not null)
			{
				timer.Elapsed -= UpdatePlayedTime;
				timer.Stop();
				timer.Dispose();
				mediaElement.MediaOpened -= MediaElement_MediaOpened;
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