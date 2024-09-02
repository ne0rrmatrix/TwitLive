using System.Web;
using System.Windows.Input;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
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
	List<Show> shows;

	string url;
	
	public string Url
	{
		get => url;
		set
		{
			var item = HttpUtility.UrlDecode(value);
			SetProperty(ref url, item);
			ThreadPool.QueueUserWorkItem(async (state) =>
			{
				await LoadShows(CancellationToken.None).ConfigureAwait(false);
			});
		}
	}
	

	readonly ILogger logger = LoggerFactory.GetLogger(nameof(ShowPageViewModel));
	readonly IDb db;

	public ShowPageViewModel(IDb db)
	{
		url = string.Empty;
		this.db = db;
		shows = [];
		WeakReferenceMessenger.Default.Register<NavigationMessage>(this,(r, m) => ThreadPool.QueueUserWorkItem(async (state) => await HandleMessage(m)));
	}

	[RelayCommand]
	public async Task Cancel(Show show)
	{
		ArgumentNullException.ThrowIfNull(App.Download);
		logger.Info("Cancelling download");
		var item = App.Download.shows.Find(x => x.Url == show.Url);
		if(item is null)
		{
			logger.Info("Cancel Item is null");
			return;
		}
		if (App.Download.shows.Count > 1 && show.Url != App.Download.CurrentShow?.Url)
		{
			logger.Info("Cancel Item is not current show");
			await db.DeleteShowAsync(show, CancellationToken.None).ConfigureAwait(false);
			ArgumentNullException.ThrowIfNull(item);
			App.Download.shows.Remove(item);
			WeakReferenceMessenger.Default.Send(new NavigationMessage(true, DownloadStatus.NotDownloaded, item));
			return;
		}
		await item.CancellationTokenSource.CancelAsync().ConfigureAwait(false);
	}

	[RelayCommand]
	public async Task DownloadShow(Show show)
	{
		ArgumentNullException.ThrowIfNull(App.Download);
		FileService.DeleteFile(show.Url);
		show.Status = DownloadStatus.Downloading;
		App.Download.shows.Add(show);
		await db.SaveShowAsync(show, CancellationToken.None).ConfigureAwait(false);
		WeakReferenceMessenger.Default.Send(new NavigationMessage(true, DownloadStatus.Downloading, show));
		if (App.Download.shows.Count > 1)
		{
			return;
		}
		var item = App.Download.shows.Find(x => x.Url == show.Url);
		ArgumentNullException.ThrowIfNull(item);
		QueDownload(item);
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
		WeakReferenceMessenger.Default.Send(new NavigationMessage (true,DownloadStatus.NotDownloaded, null));
	}

	async Task LoadShows(CancellationToken cancellationToken = default)
	{
		var items = await FeedService.GetShowListAsync(Url, cancellationToken).ConfigureAwait(false) ?? [];
		var downloads = await db.GetShowsAsync(CancellationToken.None).ConfigureAwait(false) ?? [];

		for (int i = 0; i < items.Count; i++)
		{
			var temp = downloads.Find(x => x.Url == items[i].Url);
			if (temp is not null)
			{
				items[i].Status = temp.Status;
			}
		}
		GetDispatcher.Dispatcher?.Dispatch(() => Shows = items);
	}

	void QueDownload(Show show)
	{
		ArgumentNullException.ThrowIfNull(App.Download);
		IsBusy = true;

		ThreadPool.QueueUserWorkItem(async (state) =>
		{
			var result = await App.Download.DownloadAsync(show, show.CancellationTokenSource.Token).ConfigureAwait(false);
			await HandleResult(result, show).ConfigureAwait(false);
		});
	}

	async Task HandleResult(DownloadStatus result, Show show)
	{
		ArgumentNullException.ThrowIfNull(App.Download);
		string toastText = string.Empty;
		double fontSize = 14;
		ToastDuration duration = ToastDuration.Short;

		switch (result)
		{
			case DownloadStatus.Downloaded:
				show.Status = DownloadStatus.Downloaded;
				await db.SaveShowAsync(show, CancellationToken.None).ConfigureAwait(false);
				WeakReferenceMessenger.Default.Send(new NavigationMessage(true, DownloadStatus.Downloaded, show));
				toastText = "Download Complete";
				await Task.Delay(1000, CancellationToken.None).ConfigureAwait(false);
				break;
			case DownloadStatus.NotDownloaded:
				FileService.DeleteFile(show.FileName);
				show.CancellationTokenSource = new();
				await db.DeleteShowAsync(show, CancellationToken.None).ConfigureAwait(false);
				WeakReferenceMessenger.Default.Send(new NavigationMessage(true, DownloadStatus.NotDownloaded, show));
				toastText = "Download Cancelled";
				break;
		}
		var temp = App.Download.shows.Find(x => x.Url == show.Url);
		if (temp is not null)
		{
			App.Download.shows.Remove(temp);
		}
		GetDispatcher.Dispatcher?.Dispatch(async () =>
		{
			var toast = Toast.Make(toastText, duration, fontSize);
			await toast.Show(CancellationToken.None).ConfigureAwait(false);
		});

		if (App.Download.shows.Count > 0)
		{
			QueDownload(App.Download.shows[0]);
			return;
		}

		GetDispatcher.Dispatcher?.Dispatch(() =>
		{
			PercentageLabel = string.Empty;
			IsBusy = false;
		});
	}

	async Task HandleMessage(NavigationMessage m)
	{
		if (App.Download?.shows.Count == 0)
		{
			GetDispatcher.Dispatcher?.Dispatch(() =>
			{
				PercentageLabel = string.Empty;
				IsBusy = false;
			});
		}

		var show = Shows.Find(x => x.Url == m.Show?.Url);
		if (show is not null)
		{
			logger.Info($"Updating show status: {show.Title} :Status: {m.Status}");
			GetDispatcher.Dispatcher?.Dispatch(() => show.Status = m.Status);
		}

		var downloads = await db.GetShowsAsync(CancellationToken.None).ConfigureAwait(false) ?? [];
		if (Shows.Count == 0 || downloads.Count == 0)
		{
			logger.Info("Shows or Downloads is empty");
			await LoadShows(CancellationToken.None).ConfigureAwait(false);
			return;
		}

		foreach (var item in downloads)
		{
			var temp = Shows.Find(x => x.Url == item.Url);
			if (temp is not null)
			{
				logger.Info($"Updating download status: {temp.Title} :Status: {item.Status}");
				GetDispatcher.Dispatcher?.Dispatch(() => temp.Status = item.Status);
			}
		}
	}

	public ICommand PullToRefreshCommand => new Command(async () =>
	{
		if (string.IsNullOrEmpty(Url))
		{
			IsBusy = false;
			IsRefreshing = false;
			return;
		}
		Shows.Clear();
		IsRefreshing = true;
		await LoadShows(CancellationToken.None).ConfigureAwait(false);
		IsRefreshing = false;
	});

	protected override void Dispose(bool disposing)
	{
		WeakReferenceMessenger.Default.Unregister<NavigationMessage>(this);
		base.Dispose(disposing);
	}
}