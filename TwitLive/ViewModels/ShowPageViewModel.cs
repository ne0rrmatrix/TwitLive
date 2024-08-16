using System.Web;
using System.Windows.Input;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
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
#pragma warning disable IDE0044
	CancellationTokenSource cancellationTokenSource;
	CancellationTokenSource cancellationToken;
#pragma warning restore IDE0044

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

	string? url;
	readonly ILogger logger = LoggerFactory.GetLogger(nameof(ShowPageViewModel));
	readonly IDb db;

	public ShowPageViewModel(IDb db)
	{
		this.db = db;
		shows = [];
		cancellationTokenSource = new();
		cancellationToken = new();
		System.Diagnostics.Trace.TraceInformation("ShowPageViewModel");
		WeakReferenceMessenger.Default.Register<NavigationMessage>(this,async (r, m) => await HandleMessage(m));
	}

	async Task HandleMessage(NavigationMessage m)
	{
		if(!m.Value)
		{
			var showDB = await db.GetShowsAsync(CancellationToken.None).ConfigureAwait(false) ?? [];
			showDB.ForEach(x =>
			{
				var item = Shows.Find(y => y.Url == x.Url) ?? new();
				item.IsDownloaded = x.IsDownloaded;
				item.IsDownloading = x.IsDownloading;
			});
			foreach (var show in Shows)
			{
				var temp = showDB.Find(x => x.Url == show.Url) ?? new();
				temp.IsDownloaded = show.IsDownloaded;
				temp.IsDownloading = show.IsDownloading;
			}
			return;
		}
		var item = Shows.Find(x => x.Url == m.Show?.Url) ?? new();
		switch(m.Status)
		{
			case DownloadStatus.Downloaded:
				item.IsDownloaded = true;
				item.IsDownloading = false;
				break;
			case DownloadStatus.Cancelled:
				item.IsDownloaded = false;
				item.IsDownloading = false;
				break;
		}
	}

	async Task LoadShows(CancellationToken cancellationToken = default)
	{
		var result = await FeedService.GetShowListAsync(url, cancellationToken).ConfigureAwait(false) ?? [];
		
		var showDB = await db.GetShowsAsync(cancellationToken).ConfigureAwait(false) ?? [];
		foreach (var item in showDB)
		{
			if(!result.Exists(x => x.Url == item.Url))
			{
				continue;
			}
			result[result.FindIndex(x => x.Url == item.Url)] = item;
		}
		
		GetDispatcher.Dispatcher?.Dispatch(() => Shows = result);
	}

	[RelayCommand]
	public void Cancel(Show show)
	{
		ArgumentNullException.ThrowIfNull(show.CancellationTokenSource.Token);
		logger.Info("Cancelling download");
		var item = App.Download?.shows.Find(x => x.Url == show.Url);
		ArgumentNullException.ThrowIfNull(item);
		item.CancellationTokenSource.Cancel();
	}

	[RelayCommand]
	public async Task DownloadShow(Show show)
	{
		ArgumentNullException.ThrowIfNull(App.Download);
		FileService.DeleteFile(show.Url);
		
		var CurrentShows = await db.GetShowsAsync(cancellationToken.Token);
		var orphanedShow = CurrentShows.Find(x => x.Url == show.Url);
		await db.DeleteShowAsync(orphanedShow, cancellationToken.Token).ConfigureAwait(false);

		show.IsDownloaded = true;
		show.IsDownloading = true;
		show.Status = DownloadStatus.Downloading;
		await db.SaveShowAsync(show, cancellationToken.Token).ConfigureAwait(false);

		if (App.Download.shows.Count > 0)
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
				show.IsDownloaded = true;
				show.IsDownloading = false;
				await db.SaveShowAsync(show, cancellationToken.Token).ConfigureAwait(false);
				WeakReferenceMessenger.Default.Send(new NavigationMessage(true, DownloadStatus.Downloaded, show));
				toastText = "Download Complete";
				await Task.Delay(1000, cancellationToken.Token).ConfigureAwait(false);
				break;
			case DownloadStatus.Cancelled:
				FileService.DeleteFile(show.FileName);
				show.CancellationTokenSource = new();
				show.Status = DownloadStatus.Cancelled;
				show.IsDownloaded = false;
				show.IsDownloading = false;
				await db.SaveShowAsync(show, cancellationToken.Token).ConfigureAwait(false);
				WeakReferenceMessenger.Default.Send(new NavigationMessage(true, DownloadStatus.Cancelled, show));
				toastText = "Download Cancelled";
				break;
		}

		GetDispatcher.Dispatcher?.Dispatch(async () =>
		{
			var toast = Toast.Make(toastText, duration, fontSize);
			await toast.Show(cancellationTokenSource.Token).ConfigureAwait(false);
		});
		
		if (App.Download.shows.Count > 0)
		{
			QueDownload(App.Download.shows[0]);
			return;
		}

		logger.Info("Clearing now download status info");
		GetDispatcher.Dispatcher?.Dispatch(() =>
		{
			PercentageLabel = string.Empty;
			IsBusy = false;
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
		WeakReferenceMessenger.Default.Send(new NavigationMessage (true,null, null));
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
		cancellationTokenSource?.Dispose();
		base.Dispose(disposing);
	}
}