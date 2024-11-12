using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using TwitLive.Interfaces;
using TwitLive.Models;
using TwitLive.Primitives;

namespace TwitLive.ViewModels;
[QueryProperty("Show", "Show")]
public partial class VideoPlayerViewModel : BasePageViewModel
{
	Show show = new();
	public Show Show
	{
		get => show;
		set
		{
			SetProperty(ref show, value);
		}
	}
	
	readonly IDb db;

	public IDb Db => db;

	public VideoPlayerViewModel(IDb db)
	{
		this.db = db;
		WeakReferenceMessenger.Default.Register<NavigationMessage>(this, (r, m) => ThreadPool.QueueUserWorkItem((state) => HandleMessage()));
	}

	void HandleMessage()
	{
		if(App.Download?.shows.Count > 0)
		{
			return;
		}
		Dispatcher?.Dispatch(() =>
		{
			PercentageLabel = string.Empty;
			IsBusy = false;
		});
	}
}