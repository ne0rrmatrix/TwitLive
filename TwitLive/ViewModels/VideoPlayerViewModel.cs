using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using TwitLive.Interfaces;
using TwitLive.Models;
using TwitLive.Primitives;

namespace TwitLive.ViewModels;
[QueryProperty(nameof(Show), "Show")]
public partial class VideoPlayerViewModel : BasePageViewModel
{
	[ObservableProperty]
	Show show;

	[ObservableProperty]
	IDb db;

	public VideoPlayerViewModel(IDb db)
	{
		show ??= new();
		this.db = db;
		WeakReferenceMessenger.Default.Register<NavigationMessage>(this, (r, m) => ThreadPool.QueueUserWorkItem((state) => HandleMessage()));
	}

	void HandleMessage()
	{
		if(App.Download?.shows.Count > 0)
		{
			return;
		}
		GetDispatcher.Dispatcher?.Dispatch(() =>
		{
			PercentageLabel = string.Empty;
			IsBusy = false;
		});
	}
}