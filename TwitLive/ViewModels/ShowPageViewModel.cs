using System.Web;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using TwitLive.Models;
using TwitLive.Primitives;
using TwitLive.Services;
using TwitLive.Views;

namespace TwitLive.ViewModels;
[QueryProperty("Url", "Url")]
public partial class ShowPageViewModel : BasePageViewModel
{
	List<Show> shows;
	public List<Show> Shows
	{
		get => shows;
		set => SetProperty(ref shows, value);
	}

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
		foreach (var item in App.Download.show)
		{
			result[result.FindIndex(x => x.Url == item.Url)] = item;
		}
		result.FindAll(item => (File.Exists(item.FileName))).ForEach(x => x.IsDownloaded = true);
		GetDispatcher.Dispatcher?.Dispatch(() => Shows = result);
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
	public void DownloadShow(Show show)
	{
		ArgumentNullException.ThrowIfNull(App.Download);
		show.Status = DownloadStatus.Downloading;
		if (show.CancellationTokenSource.IsCancellationRequested)
		{
			show.CancellationTokenSource.Dispose();
			show.CancellationTokenSource = new();
		}
		ThreadPool.QueueUserWorkItem(async (state) =>
		{
			var result = await App.Download.DownloadAsync(show, progress, show.CancellationTokenSource.Token).ConfigureAwait(false);
			GetDispatcher.Dispatcher?.Dispatch(async () =>
			{
				switch (result)
				{
					case DownloadStatus.Downloaded:
						show.Status = DownloadStatus.Downloaded;
						await Shell.Current.DisplayAlert("Download", "Download Complete", "Ok").ConfigureAwait(false);
						break;
					case DownloadStatus.Error:
						show.Status = DownloadStatus.Error;
						await Shell.Current.DisplayAlert("Download", "Download Failed", "Ok").ConfigureAwait(false);
						break;
					case DownloadStatus.Cancelled:
						show.Status = DownloadStatus.Cancelled;
						await Shell.Current.DisplayAlert("Download", "Download Cancelled", "Ok").ConfigureAwait(false);
						break;
					case DownloadStatus.Downloading:
						show.Status = DownloadStatus.Downloading;
						break;
					case DownloadStatus.NotDownloaded:
						show.Status = DownloadStatus.NotDownloaded;
						break;
				}
			});
		});
	}

	/// <summary>
	/// A Method that passes a Url <see cref="string"/> to <see cref="ShowPage"/>
	/// </summary>
	/// <param name="show">A Url <see cref="string"/></param>
	/// <returns></returns>
	[RelayCommand]
	public static async Task GotoVideoPage(Show show, CancellationToken cancellationToken = default)
	{
		if(File.Exists(show.FileName))
		{
			show.Url = show.FileName;
		}
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
		await LoadShows(CancellationToken.None).ConfigureAwait(false);
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