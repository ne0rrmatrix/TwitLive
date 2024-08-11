using System.Collections.ObjectModel;
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
	ObservableCollection<Show> shows;
	IDb db { get; set; }
#pragma warning disable IDE0044
	CancellationTokenSource cancellationToken;
#pragma warning restore IDE0044

	public DownloadsPageViewModel(IDb db)
	{
		this.db = db;
		shows = []; 
		cancellationToken = new();
		GetDispatcher.Dispatcher?.Dispatch(async () => { await GetShows(cancellationToken.Token).ConfigureAwait(false); OnPropertyChanged(nameof(Shows)); });
		WeakReferenceMessenger.Default.Register<NavigationMessage>(this, (r, m) => HandleMessage(m));
	}

	void HandleMessage(NavigationMessage message)
	{
		ArgumentNullException.ThrowIfNull(App.Download);

		if (App.Download.shows.Count == 0 || !message.Value)
		{
			GetDispatcher.Dispatcher?.Dispatch(() =>
			{
				System.Diagnostics.Debug.WriteLine("Clearing Download Message");
				PercentageLabel = string.Empty;
				IsBusy = false;
			});
		}
		else if (message.Value)
		{
			GetDispatcher.Dispatcher?.Dispatch(() =>
			{
				PullToRefreshCommand.Execute(this);
			});
		}
	}

	public async Task GetShows(CancellationToken cancellationToken = default)
	{
		var downloads = await db.GetShowsAsync(cancellationToken).ConfigureAwait(false);
		GetDispatcher.Dispatcher?.Dispatch(() => Shows = new ObservableCollection<Show>(downloads));
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
		await GetShows(cancellationToken.Token).ConfigureAwait(false);
	}

	public ICommand PullToRefreshCommand => new Command(async () =>
	{
		Shows.Clear();
		IsRefreshing = true;
		IsBusy = true;
		OnPropertyChanged(nameof(IsBusy));
		OnPropertyChanged(nameof(IsRefreshing));
		await GetShows(cancellationToken.Token).ConfigureAwait(false);
		IsBusy = false;
		IsRefreshing = false;
		OnPropertyChanged(nameof(IsBusy));
		OnPropertyChanged(nameof(IsRefreshing));
	});

	protected override void Dispose(bool disposing)
	{

		WeakReferenceMessenger.Default.Unregister<NavigationMessage>(this);
		cancellationToken?.Dispose();
		base.Dispose(disposing);
	}
}
