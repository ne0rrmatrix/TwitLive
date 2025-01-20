using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using TwitLive.Interfaces;
using TwitLive.Models;
using TwitLive.Primitives;

namespace TwitLive.ViewModels;
public partial class DownloadsPageViewModel : BasePageViewModel
{
	[ObservableProperty]
	public partial List<Show> Shows { get; set; }
	readonly IDb db;
	public DownloadsPageViewModel(IDb db)
	{
		this.db = db;
		Shows = []; 
		WeakReferenceMessenger.Default.Register<NavigationMessage>(this,async (r, m) => await HandleMessage());
	}

	async Task HandleMessage()
	{
		if (App.Download?.shows.Count == 0)
		{
			Dispatcher?.Dispatch(() =>
			{
				PercentageLabel = string.Empty;
				IsBusy = false;
			});
		}
		await GetShows(CancellationToken.None).ConfigureAwait(false);
	}

	async Task GetShows(CancellationToken cancellationToken = default)
	{
		var temp = new List<Show>();
		Shows.Clear();

		var downloads = await db.GetShowsAsync(cancellationToken).ConfigureAwait(false);
		temp.AddRange(downloads.Where(download => File.Exists(download.FileName) && download.Status == DownloadStatus.Downloaded));
		
		Dispatcher?.Dispatch(() => Shows = temp);
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
		}
		WeakReferenceMessenger.Default.Send(new NavigationMessage(true, DownloadStatus.NotDownloaded, show));
		await GetShows(CancellationToken.None).ConfigureAwait(false);
	}

	public ICommand PullToRefreshCommand => new Command(async () =>
	{
		Shows.Clear();
		IsRefreshing = true;
		IsBusy = true;
		await GetShows(CancellationToken.None).ConfigureAwait(false);
		IsBusy = false;
		IsRefreshing = false;
	});

	protected override void Dispose(bool disposing)
	{
		WeakReferenceMessenger.Default.Unregister<NavigationMessage>(this);
		base.Dispose(disposing);
	}
}
