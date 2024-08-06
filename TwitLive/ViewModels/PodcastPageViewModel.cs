using System.Collections.ObjectModel;
using System.Web;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TwitLive.Models;
using TwitLive.Services;
using TwitLive.Views;

namespace TwitLive.ViewModels;
public partial class PodcastPageViewModel : BasePageViewModel
{
	[ObservableProperty]
	ObservableCollection<Podcast> podcasts;
	public PodcastPageViewModel()
	{
		podcasts = [];
		ThreadPool.QueueUserWorkItem(async (state) =>
		{
			var item = await FeedService.GetPodcasts().ConfigureAwait(false);
			GetDispatcher.Dispatcher?.Dispatch(() => Podcasts = new ObservableCollection<Podcast>(item));
		});
	}
	protected void OnAppearing()
	{
		IsBusy = false;
	}

	[RelayCommand]
	public static async Task GotoShowPage(Podcast podcast, CancellationToken cancellationToken = default)
	{
		var encodedUrl = HttpUtility.UrlEncode(podcast.Url);
		var navigationParameter = new Dictionary<string, object>
		{
			{ "Url", encodedUrl }
		};
		await Shell.Current.GoToAsync($"//ShowPage", navigationParameter).WaitAsync(cancellationToken).ConfigureAwait(false);
	}

	public ICommand PullToRefreshCommand => new Command(async () =>
	{
		Podcasts.Clear();
		IsRefreshing = true;
		IsBusy = true;
		OnPropertyChanged(nameof(IsBusy));
		OnPropertyChanged(nameof(IsRefreshing));
		var item = await FeedService.GetPodcasts(CancellationToken).ConfigureAwait(false);
		Podcasts = new ObservableCollection<Podcast>(item);
		IsBusy = false;
		IsRefreshing = false;
		OnPropertyChanged(nameof(IsBusy));
		OnPropertyChanged(nameof(IsRefreshing));
	});
}