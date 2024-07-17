using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TwitLive.Interfaces;
using TwitLive.Models;
using TwitLive.Primitives;

namespace TwitLive.ViewModels;
public partial class DownloadsPageViewModel : BasePageViewModel
{
	List<Show> shows;
	public List<Show> Shows
	{
		get => shows;
		set => SetProperty(ref shows, value);
	}
	IDb db { get; set; }
	IDownload download { get; set; }
	public DownloadsPageViewModel(IDb db, IDownload download)
	{
		this.db = db;
		this.download = download;
		shows = []; 
		GetDispatcher.Dispatcher?.Dispatch(async () => { await GetShows(); OnPropertyChanged(nameof(Shows)); });
		this.download.ProgressChanged += Download_ProgressChanged;
	}

	async void Download_ProgressChanged(object? sender, DownloadProgressEventArgs e)
	{
		if(e.Status == DownloadStatus.Downloaded)
		{
			Shows.Clear();
			await GetShows().ConfigureAwait(false);
		}
	}

	public async Task GetShows()
	{
		var downloads = await db.GetShowsAsync().ConfigureAwait(false);
		GetDispatcher.Dispatcher?.Dispatch(() => Shows = downloads);
		OnPropertyChanged(nameof(Shows));
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
	}

	[RelayCommand]
	public async Task DeleteShow(Show show)
	{
		await db.DeleteShowAsync(show);
		if(File.Exists(show.FileName))
		{
			File.Delete(show.FileName);
			System.Diagnostics.Trace.TraceInformation($"Deleted {show.FileName}");
		}
		else
		{
			System.Diagnostics.Trace.TraceInformation($"File {show.FileName} not found");
		}
		Shows.Clear();
		await GetShows().ConfigureAwait(false);
	}
	public ICommand PullToRefreshCommand => new Command(async () =>
	{
		Shows.Clear();
		IsRefreshing = true;
		IsBusy = true;
		OnPropertyChanged(nameof(IsBusy));
		OnPropertyChanged(nameof(IsRefreshing));
		await GetShows().ConfigureAwait(false);
		IsBusy = false;
		IsRefreshing = false;
		OnPropertyChanged(nameof(IsBusy));
		OnPropertyChanged(nameof(IsRefreshing));
	});
	protected override void Dispose(bool disposing)
	{
		download.ProgressChanged -= Download_ProgressChanged;
		base.Dispose(disposing);
	}
}
