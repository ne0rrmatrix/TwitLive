using System.Collections.ObjectModel;
using System.Web;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MetroLog;
using TwitLive.Interfaces;
using TwitLive.Models;
using TwitLive.Primitives;
using TwitLive.Services;

namespace TwitLive.ViewModels;
[QueryProperty("Url", "Url")]
public partial class ShowPageViewModel : BasePageViewModel
{
	[ObservableProperty]
	ObservableCollection<Show> shows;
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
	readonly IDb db;

	public ShowPageViewModel(IDb db)
	{
		shows = [];
		this.db = db;
		cancellationToken = new();
		WeakReferenceMessenger.Default.Register<NavigationMessage>(this, async (r, m) => await HandleMessage(m));
	}

	async Task HandleMessage(NavigationMessage message)
	{
		ArgumentNullException.ThrowIfNull(App.Download);
		if (message.Status is null)
		{
			logger.Info("Message Status is null");
			await LoadShows(cancellationToken.Token);
			return;
		}
		if(!message.Value)
		{
			return;
		}
		if (App.Download.shows.Count == 0)
		{
			GetDispatcher.Dispatcher?.Dispatch(() =>
			{
				logger.Info("Clearing Download Message");
				PercentageLabel = string.Empty;
				IsBusy = false;
				OnPropertyChanged(nameof(PercentageLabel));
				OnPropertyChanged(nameof(IsBusy));
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
		var result = await FeedService.GetShowListAsync(url, cancellationToken).ConfigureAwait(false);
		var downloaded = await db.GetShowsAsync(cancellationToken).ConfigureAwait(false);

		if(downloaded is not null && downloaded.Count > 0)
		{
			downloaded.FindAll(item => (result.Find(x => x.Url == item.Url) is not null))
			.ForEach(y =>
			{
				if(result.Find(x => x.Url == y.Url) is not null)
				{
					result[result.FindIndex(x => x.Url == y.Url)] = y;
				}
			});
		}

		GetDispatcher.Dispatcher?.Dispatch(() => Shows =new ObservableCollection<Show>(result));
	}

	[RelayCommand]
	public void Cancel(Show show)
	{
		ArgumentNullException.ThrowIfNull(show.CancellationTokenSource.Token);
		logger.Info("Cancelling download");
		var item = App.Download?.shows.Find(x => x.Url == show.Url);
		item?.CancellationTokenSource.Cancel();
	}

	[RelayCommand]
	public static async Task GotoVideoPage(Show show, CancellationToken cancellationToken = default)
	{
		if (File.Exists(show.FileName))
		{
			show.Url = show.FileName;
		}

		var navigationParameter = new Dictionary<string, object>
		{
			{ "Show", show }
		};
		await Shell.Current.GoToAsync($"//VideoPlayerPage", navigationParameter).WaitAsync(cancellationToken).ConfigureAwait(false);
		WeakReferenceMessenger.Default.Send(new NavigationMessage(true, null));
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
		var item = App.Download.shows.Find(x => x.Url == show.Url);
		QueDownload(item);
	}

	void QueDownload(Show? show)
	{
		ArgumentNullException.ThrowIfNull(show);
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
						await RemoveShowDownload(show);
						await Shell.Current.DisplayAlert("Download", "Download Complete", "Ok").ConfigureAwait(false);
						WeakReferenceMessenger.Default.Send(new NavigationMessage(true, DownloadStatus.Downloaded));
						break;
					case DownloadStatus.Error:
						await RemoveShowDownload(show);
						await Shell.Current.DisplayAlert("Download", "Download Failed", "Ok").ConfigureAwait(false);
						WeakReferenceMessenger.Default.Send(new NavigationMessage(false, DownloadStatus.NotDownloaded));
						break;
					case DownloadStatus.Cancelled:
						await RemoveShowDownload(show);
						await Shell.Current.DisplayAlert("Download", "Download Cancelled", "Ok").ConfigureAwait(false);
						WeakReferenceMessenger.Default.Send(new NavigationMessage(true, DownloadStatus.Cancelled));
						break;
				}
			});
		});
	}

	async Task RemoveShowDownload(Show show)
	{
		var item = Shows.ToList().Find(x => x.Url == show.Url);
		if(item is null)
		{
			return;
		}
		var dbShows = await db.GetShowsAsync(CancellationToken.None).ConfigureAwait(false);
		var downloadedShows = dbShows.Find(x => x.Url == show.Url);
		if (downloadedShows is not null)
		{
			item.IsDownloaded = downloadedShows.IsDownloaded;
			item.IsDownloading = downloadedShows.IsDownloading;
			item.Status = downloadedShows.Status;
		}
		else
		{
			item.IsDownloaded = false;
			item.IsDownloading = false;
			item.Status = DownloadStatus.NotDownloaded;
			item.CancellationTokenSource = new();
		}
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