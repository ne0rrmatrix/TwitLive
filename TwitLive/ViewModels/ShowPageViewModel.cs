using System.Collections.ObjectModel;
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
public partial class ShowPageViewModel(IDb db) : BasePageViewModel, IQueryAttributable
{
	[ObservableProperty]
	public partial ObservableCollection<Show> Shows { get; set; } = [];

	[ObservableProperty]
	public partial string Url { get; set; } = string.Empty;

	public async void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		if (query.TryGetValue("Url", out var urlObj) && urlObj is string url && !string.IsNullOrEmpty(url))
		{
			var item = HttpUtility.UrlDecode(url);
			Url = item;
			await LoadShows(CancellationToken.None).ConfigureAwait(false);
			WeakReferenceMessenger.Default.Register<NavigationMessage>(this, async (r, m) => await HandleMessage());
		}
	}

	readonly ILogger logger = LoggerFactory.GetLogger(nameof(ShowPageViewModel));
	readonly IDb db = db;

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
		await QueDownload(item);
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
		if(Dispatcher.IsDispatchRequired)
		{
			Dispatcher.Dispatch(() => Shows = new ObservableCollection<Show>(items));
			return;
		}
		else
		{
			Shows = new ObservableCollection<Show>(items);
		}
	}

	async Task QueDownload(Show show)
	{
		ArgumentNullException.ThrowIfNull(App.Download);
		IsBusy = true;

		var result = await App.Download.DownloadAsync(show, show.CancellationTokenSource.Token).ConfigureAwait(false);
		await HandleResult(result, show).ConfigureAwait(false);
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
		Dispatcher?.Dispatch(async () =>
		{
			var toast = Toast.Make(toastText, duration, fontSize);
			await toast.Show(CancellationToken.None).ConfigureAwait(false);
		});

		if (App.Download.shows.Count > 0)
		{
			await QueDownload(App.Download.shows[0]).ConfigureAwait(false);
			return;
		}

		Dispatcher?.Dispatch(() =>
		{
			PercentageLabel = string.Empty;
			IsBusy = false;
		});
	}

	async Task HandleMessage()
	{
		if (App.Download?.shows.Count == 0)
		{
			if(Dispatcher.IsDispatchRequired)
			{
				Dispatcher.Dispatch(() =>
				{
					PercentageLabel = string.Empty;
					IsBusy = false;
				});
			}
			else
			{
				PercentageLabel = string.Empty;
				IsBusy = false;
			}
		}
		await LoadShows(CancellationToken.None).ConfigureAwait(false);
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