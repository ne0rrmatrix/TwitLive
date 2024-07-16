using System.Collections.ObjectModel;
using System.Web;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TwitLive.Models;
using TwitLive.Primitives;
using TwitLive.Services;
using TwitLive.Views;

namespace TwitLive.ViewModels;
[QueryProperty("Url", "Url")]
public partial class ShowPageViewModel : BasePageViewModel
{
	[ObservableProperty]
	ObservableCollection<Show> shows;

	double percentage = 0;
	readonly Progress<double> progress;
	readonly CancellationTokenSource cancellationToken;
	
	string? url;
	public string? Url
	{
		get => url;
		set
		{
			var item = HttpUtility.UrlDecode(value);
			SetProperty(ref url, item);
			ThreadPool.QueueUserWorkItem(async (state) =>
			{
				await LoadShows(cancellationToken.Token).ConfigureAwait(false);
			});
		}
	}
	public ShowPageViewModel()
	{
		shows = [];
		progress = new();
		cancellationToken = new();
		progress.ProgressChanged += Progress_ProgressChanged;
	}

	async Task LoadShows(CancellationToken cancellationToken = default)
	{
		if (url is null)
		{
			return;
		}
		ArgumentNullException.ThrowIfNull(App.Download);
		var result = await FeedService.GetShowListAsync(url, cancellationToken).ConfigureAwait(false);
		if (result.Exists(x => x.Url == App.Download.show.Url))
		{

			result[result.FindIndex(x => x.Url == App.Download.show.Url)] = App.Download.show;
		}
		GetDispatcher.Dispatcher?.Dispatch(() => Shows = new ObservableCollection<Show>(result));
	}

	[RelayCommand]
	public static Task Cancel(Show show)
	{
		ArgumentNullException.ThrowIfNull(show.CancellationTokenSource.Token);
		System.Diagnostics.Trace.TraceInformation("Cancelling download");
		show.CancellationTokenSource.Cancel();
		return Task.CompletedTask;
	}

	[RelayCommand]
	public async Task DownloadShow(Show show)
	{
		ArgumentNullException.ThrowIfNull(App.Download);
		show.IsDownloaded = false;
		show.IsDownloading = true;
		show.IsNotDownloaded = false;
		show.Status = DownloadStatus.Downloading;
		if(show.CancellationTokenSource.IsCancellationRequested)
		{
			show.CancellationTokenSource.Dispose();
			show.CancellationTokenSource = new();
		}
		var result = await App.Download.DownloadAsync(show,progress, show.CancellationTokenSource.Token).ConfigureAwait(false);
		if (result == DownloadStatus.Downloaded)
		{
			show.IsNotDownloaded = false;
			show.IsDownloading = false;
			show.IsDownloaded = true;
			show.Status = DownloadStatus.Downloaded;
			GetDispatcher.Dispatcher?.Dispatch(async () => await Shell.Current.DisplayAlert("Download", "Download Complete", "Ok").ConfigureAwait(false));
		}
		else if (result == DownloadStatus.Error)
		{
			show.IsNotDownloaded = true;
			show.IsDownloading = false;
			show.IsDownloaded = false;
			show.Status = DownloadStatus.Error;
			GetDispatcher.Dispatcher?.Dispatch(async () => await Shell.Current.DisplayAlert("Download", "Download Failed", "Ok").ConfigureAwait(false));
		}
		else if (result == DownloadStatus.Cancelled)
		{
			show.IsNotDownloaded = true;
			show.IsDownloading = false;
			show.IsDownloaded = false;
			show.Status = DownloadStatus.Cancelled;
			GetDispatcher.Dispatcher?.Dispatch(async () => await Shell.Current.DisplayAlert("Download", "Download Cancelled", "Ok").ConfigureAwait(false));
		}
	}

	/// <summary>
	/// A Method that passes a Url <see cref="string"/> to <see cref="ShowPage"/>
	/// </summary>
	/// <param name="show">A Url <see cref="string"/></param>
	/// <returns></returns>
	[RelayCommand]
	public static async Task GotoVideoPage(Show show, CancellationToken cancellationToken = default)
	{
		var navigationParameter = new Dictionary<string, object>
		{
			{ "Show", show }
		};
		await Shell.Current.GoToAsync($"//VideoPlayerPage", navigationParameter).WaitAsync(cancellationToken).ConfigureAwait(false);
	}

	public ICommand PullToRefreshCommand => new Command(async () =>
	{
		if (Url is null)
		{
			IsBusy = false;
			IsRefreshing = false;
			return;
		}
		Shows.Clear();
		IsRefreshing = true;
		IsBusy = true;
		OnPropertyChanged(nameof(IsBusy));
		OnPropertyChanged(nameof(IsRefreshing));

		var updatedShows = await FeedService.GetShowListAsync(Url).ConfigureAwait(false);
		Shows = new ObservableCollection<Show>(updatedShows);
		IsBusy = false;
		IsRefreshing = false;
		OnPropertyChanged(nameof(IsBusy));
		OnPropertyChanged(nameof(IsRefreshing));
	});

	protected override void Dispose(bool disposing)
	{
		progress.ProgressChanged -= Progress_ProgressChanged;
		cancellationToken?.Dispose();
		base.Dispose(disposing);
	}

	void Progress_ProgressChanged(object? sender, double e)
	{
		ThreadPool.QueueUserWorkItem((state) =>
		{
			var temp = Math.Floor(e);
			if (temp > percentage)
			{
				percentage = temp;
				System.Diagnostics.Trace.TraceInformation($"Progress: {percentage}");
			}
		});
	}
}