using System.Collections.ObjectModel;
using System.Web;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TwitLive.Models;
using TwitLive.Services;
using TwitLive.Views;

namespace TwitLive.ViewModels;
[QueryProperty("Url", "Url")]
public partial class ShowPageViewModel : BasePageViewModel
{
	[ObservableProperty]
	ObservableCollection<Show> shows;
	string? url;
	public string? Url
	{
		get => url;
		set
		{
			var item = HttpUtility.UrlDecode(value);
			SetProperty(ref url, item);
			ThreadPool.QueueUserWorkItem(async (state) =>
			{
				await LoadShows(CancellationToken).ConfigureAwait(false);
			});
		}
	}
	public ShowPageViewModel()
	{
		shows = [];
	}
	async Task LoadShows(CancellationToken cancellationToken = default)
	{
		if (url is null)
		{
			return;
		}
		var result = await FeedService.GetShowListAsync(url, cancellationToken).ConfigureAwait(false);
		GetDispatcher.Dispatcher?.Dispatch(() => Shows = new ObservableCollection<Show>(result));
	}

	/// <summary>
	/// A Method that passes a Url <see cref="string"/> to <see cref="ShowPage"/>
	/// </summary>
	/// <param name="show">A Url <see cref="string"/></param>
	/// <returns></returns>
	[RelayCommand]
	public static async Task GotoVideoPage(Show show, CancellationToken cancellationToken = default)
	{
		var navigationParameter = new Dictionary<string, object>
		{
			{ "Show", show }
		};
		await Shell.Current.GoToAsync($"//VideoPlayerPage", navigationParameter).WaitAsync(cancellationToken).ConfigureAwait(false);
	}

	public ICommand PullToRefreshCommand => new Command(async () =>
	{
		if (Url is null)
		{
			IsBusy = false;
			IsRefreshing = false;
			return;
		}
		Shows.Clear();
		IsRefreshing = true;
		IsBusy = true;
		OnPropertyChanged(nameof(IsBusy));
		OnPropertyChanged(nameof(IsRefreshing));

		var updatedShows = await FeedService.GetShowListAsync(Url).ConfigureAwait(false);
		Shows = new ObservableCollection<Show>(updatedShows);
		IsBusy = false;
		IsRefreshing = false;
		OnPropertyChanged(nameof(IsBusy));
		OnPropertyChanged(nameof(IsRefreshing));
	});
}