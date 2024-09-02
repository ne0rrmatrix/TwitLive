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
	List<Show> shows;
	IDb db { get; set; }
	public DownloadsPageViewModel(IDb db)
	{
		this.db = db;
		shows = []; 
		GetDispatcher.Dispatcher?.Dispatch(async () => { await GetShows(CancellationToken.None).ConfigureAwait(false); OnPropertyChanged(nameof(Shows)); });
		WeakReferenceMessenger.Default.Register<NavigationMessage>(this,async (r, m) => await HandleMessage());
	}

	async Task HandleMessage()
	{
		if (App.Download?.shows.Count == 0)
		{
			GetDispatcher.Dispatcher?.Dispatch(() =>
			{
				PercentageLabel = string.Empty;
				IsBusy = false;
			});
		}
		Shows.Clear();
		await GetShows(CancellationToken.None).ConfigureAwait(false);
	}

	async Task GetShows(CancellationToken cancellationToken = default)
	{
		var downloads = await db.GetShowsAsync(cancellationToken).ConfigureAwait(false);
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
		}
		
		var CurrentShows = await db.GetShowsAsync();
		var orphanedShow = CurrentShows.Find(x => x.Url == show.Url);
		await db.DeleteShowAsync(orphanedShow).ConfigureAwait(false);
		Shows.Clear();
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
