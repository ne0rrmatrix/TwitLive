﻿using System.Collections.ObjectModel;
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

	/// <summary>
	/// A Method that passes a Url <see cref="string"/> to <see cref="ShowPage"/>
	/// </summary>
	/// <param name="podcast">A Url <see cref="string"/></param>
	/// <returns></returns>
	[RelayCommand]
	public static async Task GotoShowPage(Podcast podcast)
	{
		var encodedUrl = HttpUtility.UrlEncode(podcast.Url);
		var navigationParameter = new Dictionary<string, object>
		{
			{ "Url", encodedUrl }
		};
		await Shell.Current.GoToAsync($"//ShowPage", navigationParameter);
	}

	public ICommand PullToRefreshCommand => new Command(async () =>
	{
		Podcasts.Clear();
		IsRefreshing = true;
		IsBusy = true;
		OnPropertyChanged(nameof(IsBusy));
		OnPropertyChanged(nameof(IsRefreshing));
		var item = await FeedService.GetPodcasts().ConfigureAwait(false);
		Podcasts = new ObservableCollection<Podcast>(item);
		IsBusy = false;
		IsRefreshing = false;
		OnPropertyChanged(nameof(IsBusy));
		OnPropertyChanged(nameof(IsRefreshing));
	});
}