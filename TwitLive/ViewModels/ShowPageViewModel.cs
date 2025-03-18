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
public partial class ShowPageViewModel : BasePageViewModel, IQueryAttributable
{
	readonly ILogger logger = LoggerFactory.GetLogger(nameof(ShowPageViewModel));
	readonly IDb db;

	[ObservableProperty]
	public partial ObservableCollection<Show> Shows { get; set; } = [];

	[ObservableProperty]
	public partial string Url { get; set; } = string.Empty;

	public ShowPageViewModel(IDb db)
	{
		this.db = db;
		WeakReferenceMessenger.Default.Register<NavigationMessage>(this, HandleMessage);
	}

	public async void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		if (query.TryGetValue("Url", out var urlObj) && urlObj is string url && !string.IsNullOrEmpty(url))
		{
			var item = HttpUtility.UrlDecode(url);
			Url = item;
			await LoadShows(CancellationToken.None).ConfigureAwait(false);
		}
	}

	[RelayCommand]
	public async Task Cancel(Show show)
	{
		ArgumentNullException.ThrowIfNull(App.Download);
		logger.Info("Cancelling download");
		if (App.Download.shows.Count > 1 || show.Url != App.Download.CurrentShow?.Url)
		{
			logger.Info("Cancel Item is not current show");
			await db.DeleteShowAsync(show, CancellationToken.None).ConfigureAwait(false);
			show.CancellationTokenSource = new();
			var item = App.Download.shows.FirstOrDefault(x => x.Url == show.Url) ?? throw new InvalidOperationException();
			App.Download.shows.Remove(item);
			WeakReferenceMessenger.Default.Send(new NavigationMessage(true, DownloadStatus.NotDownloaded, show));
		}
		else
		{
			logger.Info("Cancel Item is current show");
			await show.CancellationTokenSource.CancelAsync().ConfigureAwait(false);
			show.CancellationTokenSource = new();
		}
	}

	[RelayCommand]
	public static void DownloadShow(Show show)
	{
		System.Diagnostics.Debug.WriteLine($"Downloading {show.Url}");
		show.Status = DownloadStatus.Downloading;
		App.Download?.shows.Add(show);
		WeakReferenceMessenger.Default.Send(new NavigationMessage(true, DownloadStatus.Downloading, show));
		ArgumentNullException.ThrowIfNull(App.Download);
		ThreadPool.QueueUserWorkItem(async (temp) => await App.Download.QueDownload(show).ConfigureAwait(false));
		System.Diagnostics.Debug.WriteLine("Queued download");

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
		Dispatcher.Dispatch(() => Shows = [.. items]);
	}

	void HandleMessage(object? sender, NavigationMessage e)
	{
		if (sender is null || e.Show is null)
		{
			logger.Info("Sender or Show is null");
			return;
		}
		
		Dispatcher.Dispatch(() =>
		{
			var item = Shows.ToList().Find(x => x.Url == e.Show?.Url);
			if (item is not null)
			{
				item.Status = e.Status;
			}
			PercentageLabel = string.Empty;
			IsBusy = false;
		});
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