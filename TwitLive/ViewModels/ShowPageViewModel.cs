using System.Web;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MetroLog;
using TwitLive.Database;
using TwitLive.Interfaces;
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

	readonly CancellationTokenSource cancellationToken;
	string? url;
	readonly ILogger logger = LoggerFactory.GetLogger(nameof(ShowPageViewModel));
	
	public ShowPageViewModel(IDb db)
	{
		shows = [];
		cancellationToken = new();
		System.Diagnostics.Trace.TraceInformation("ShowPageViewModel");
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
		foreach (var item in App.Download.show)
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
		show.Status = DownloadStatus.Downloading;
		if (show.CancellationTokenSource.IsCancellationRequested)
		{
			show.CancellationTokenSource.Dispose();
			show.CancellationTokenSource = new();
		}
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
						PercentageLabel = string.Empty;
						IsBusy = false;
						await Shell.Current.DisplayAlert("Download", "Download Complete", "Ok").ConfigureAwait(false);
						break;
					case DownloadStatus.Error:
						show.Status = DownloadStatus.Error;
						PercentageLabel = string.Empty;
						IsBusy = false;
						show.IsDownloaded = false;
						show.IsDownloading = false;
						File.Delete(show.FileName);
						await Shell.Current.DisplayAlert("Download", "Download Failed", "Ok").ConfigureAwait(false);
						break;
					case DownloadStatus.Cancelled:
						FileService.DeleteFile(show.FileName);
						show.IsDownloaded = false;
						show.IsDownloading = false;
						show.Status = DownloadStatus.Cancelled;
						PercentageLabel = string.Empty;
						IsBusy = false;
						await Shell.Current.DisplayAlert("Download", "Download Cancelled", "Ok").ConfigureAwait(false);
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
	public async Task GotoVideoPage(Show show, CancellationToken cancellationToken = default)
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
		WeakReferenceMessenger.Default.Send(new NavigationMessage (true));
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
		await LoadShows(CancellationToken.None).ConfigureAwait(false);
		IsBusy = false;
		IsRefreshing = false;
	});

	protected override void Dispose(bool disposing)
	{
		cancellationToken?.Dispose();
		base.Dispose(disposing);
	}
}