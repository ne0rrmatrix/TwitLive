using System.Web;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MetroLog;
using TwitLive.Models;
using TwitLive.Primitives;
using TwitLive.Services;

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

#pragma warning disable IDE0044
	CancellationTokenSource cancellationToken;
#pragma warning restore IDE0044
	string? url;
	readonly ILogger logger = LoggerFactory.GetLogger(nameof(ShowPageViewModel));
	
	public ShowPageViewModel()
	{
		shows = [];
		cancellationToken = new();
		System.Diagnostics.Trace.TraceInformation("ShowPageViewModel");
		WeakReferenceMessenger.Default.Register<NavigationMessage>(this, (r, m) => HandleMessage(m));
	}

	void HandleMessage(NavigationMessage message)
	{
		ArgumentNullException.ThrowIfNull(App.Download);
		if (message.Status is null)
		{
			return;
		}
		if(!message.Value)
		{
			App.Download.shows.Clear();
			return;
		}
		if (App.Download.shows.Count == 0)
		{
			GetDispatcher.Dispatcher?.Dispatch(() =>
			{
				System.Diagnostics.Debug.WriteLine("Clearing Download Message");
				PercentageLabel = string.Empty;
				IsBusy = false;
			});

			return;
		}
		if (App.Download.shows.Count > 0)
		{
			QueDownload(App.Download.shows[0]);
		}
	}

	async Task LoadShows(CancellationToken cancellationToken = default)
	{
		if (url is null)
		{
			return;
		}
		ArgumentNullException.ThrowIfNull(App.Download);
		var result = await FeedService.GetShowListAsync(url, cancellationToken).ConfigureAwait(false);
		if(result is null)
		{
			return;
		}
		foreach (var item in App.Download.shows)
		{
			result[result.FindIndex(x => x.Url == item.Url)] = item;
		}
		result.FindAll(item => (File.Exists(item.FileName))).ForEach(x => x.IsDownloaded = true);
		GetDispatcher.Dispatcher?.Dispatch(() => Shows = result);
	}

	[RelayCommand]
	public Task Cancel(Show show)
	{
		ArgumentNullException.ThrowIfNull(show.CancellationTokenSource.Token);
		logger.Info("Cancelling download");
		show.CancellationTokenSource.Cancel();
		return Task.CompletedTask;
	}

	[RelayCommand]
	public void DownloadShow(Show show)
	{
		ArgumentNullException.ThrowIfNull(App.Download);
		if (App.Download.shows.Count > 1)
		{
			App.Download.shows.Add(show);
			return;
		}
		App.Download.shows.Add(show);
		QueDownload(show);
	}

	void QueDownload(Show show)
	{
		ArgumentNullException.ThrowIfNull(App.Download);
		IsBusy = true;
		show.Status = DownloadStatus.Downloading;
		ThreadPool.QueueUserWorkItem(async (state) =>
		{
			var result = await App.Download.DownloadAsync(show, show.CancellationTokenSource.Token).ConfigureAwait(false);
			GetDispatcher.Dispatcher?.Dispatch(async () =>
			{
				switch (result)
				{
					case DownloadStatus.Downloaded:
						show.Status = DownloadStatus.Downloaded;
						show.IsDownloading = false;
						show.IsDownloaded = true;
						await Shell.Current.DisplayAlert("Download", "Download Complete", "Ok").ConfigureAwait(false);
						await Task.Delay(1000, cancellationToken.Token).ConfigureAwait(false);
						WeakReferenceMessenger.Default.Send(new NavigationMessage(true, DownloadStatus.Downloaded));
						break;
					case DownloadStatus.Error:
						show.Status = DownloadStatus.Error;
						show.IsDownloaded = false;
						show.IsDownloading = false;
						File.Delete(show.FileName);
						await Shell.Current.DisplayAlert("Download", "Download Failed", "Ok").ConfigureAwait(false);
						WeakReferenceMessenger.Default.Send(new NavigationMessage(false, DownloadStatus.NotDownloaded));
						break;
					case DownloadStatus.Cancelled:
						FileService.DeleteFile(show.FileName);
						show.IsDownloaded = false;
						show.IsDownloading = false;
						show.Status = DownloadStatus.Cancelled;
						show.CancellationTokenSource = new();
						await Shell.Current.DisplayAlert("Download", "Download Cancelled", "Ok").ConfigureAwait(false);
						WeakReferenceMessenger.Default.Send(new NavigationMessage(true, DownloadStatus.Cancelled));
						break;
				}
			});
		});
	}
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
		WeakReferenceMessenger.Default.Send(new NavigationMessage (true,null));
	}

	public ICommand PullToRefreshCommand => new Command(async () =>
	{
		if (Url is null || string.IsNullOrEmpty(url))
		{
			IsBusy = false;
			IsRefreshing = false;
			return;
		}
		Shows.Clear();
		IsRefreshing = true;
		IsBusy = true;
		await LoadShows(cancellationToken.Token).ConfigureAwait(false);
		IsBusy = false;
		IsRefreshing = false;
	});

	protected override void Dispose(bool disposing)
	{
		WeakReferenceMessenger.Default.Unregister<NavigationMessage>(this);
		cancellationToken?.Dispose();
		base.Dispose(disposing);
	}
}