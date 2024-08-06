using CommunityToolkit.Mvvm.ComponentModel;
using TwitLive.Interfaces;
using TwitLive.Models;

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
	}
}