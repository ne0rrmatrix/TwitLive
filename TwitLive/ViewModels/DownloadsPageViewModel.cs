using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TwitLive.Interfaces;
using TwitLive.Models;

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
		GetDispatcher.Dispatcher?.Dispatch(async () => { await GetShows(); OnPropertyChanged(nameof(Shows)); });
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
		
		var CurrentShows = await db.GetShowsAsync();
		var orphanedShow = CurrentShows.Find(x => x.Url == show.Url);
		if (orphanedShow is not null)
		{
			await db.DeleteShowAsync(orphanedShow).ConfigureAwait(false);
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
}
