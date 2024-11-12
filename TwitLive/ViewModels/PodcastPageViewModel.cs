using System.Collections.ObjectModel;
using System.Web;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using TwitLive.Models;
using TwitLive.Primitives;
using TwitLive.Services;

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
			await LoadPodcasts(CancellationToken.None);
		});
		WeakReferenceMessenger.Default.Register<NavigationMessage>(this, (r, m) => HandleMessage(m));
	}

	async Task LoadPodcasts(CancellationToken cancellationToken = default)
	{
		var item = await FeedService.GetPodcasts(cancellationToken).ConfigureAwait(false);
		Dispatcher?.Dispatch(() => Podcasts = new ObservableCollection<Podcast>(item));
	}

	void HandleMessage(NavigationMessage message)
	{
		if (App.Download?.shows.Count == 0 || !message.Value)
		{
			System.Diagnostics.Debug.WriteLine("Clearing Download Message");
			Dispatcher?.Dispatch(() =>
			{
				PercentageLabel = string.Empty;
				IsBusy = false;
			});
		}
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
		OnPropertyChanged(nameof(IsRefreshing));
		var item = await FeedService.GetPodcasts(CancellationToken.None).ConfigureAwait(false);
		
		Podcasts = new ObservableCollection<Podcast>(item);
		IsRefreshing = false;
		OnPropertyChanged(nameof(IsRefreshing));
	});

	protected override void Dispose(bool disposing)
	{
		WeakReferenceMessenger.Default.Unregister<NavigationMessage>(this);
		base.Dispose(disposing);
	}
}