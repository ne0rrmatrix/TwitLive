using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using TwitLive.Interfaces;
using TwitLive.Models;
using TwitLive.Primitives;

namespace TwitLive.ViewModels;
public partial class VideoPlayerViewModel : BasePageViewModel, IQueryAttributable
{
	[ObservableProperty]
	public partial Show Show { get; set; }
	public void ApplyQueryAttributes(IDictionary<string, object> query)
	{
		if (query.TryGetValue("Show", out var showObj) && showObj is Show show)
		{
			Show = show;
		}
	}

	public IDb Db { get; }

	public VideoPlayerViewModel(IDb db)
	{
		Show = new();
		this.Db = db;
		WeakReferenceMessenger.Default.Register<NavigationMessage>(this, (r, m) => HandleMessage());
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