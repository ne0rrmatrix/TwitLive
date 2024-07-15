using System.Web;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TwitLive.Models;

namespace TwitLive.ViewModels;
[QueryProperty(nameof(Show), "Show")]
public partial class VideoPlayerViewModel : BasePageViewModel
{
	Show show;
	public Show Show
	{
		get => show;
		set => SetProperty(ref show, value);
	}

	public VideoPlayerViewModel()
	{
		show ??= new Show();
	}
}