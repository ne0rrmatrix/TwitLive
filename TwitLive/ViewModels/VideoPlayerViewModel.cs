using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Web;
using TwitLive.Models;
using TwitLive.Services;

namespace TwitLive.ViewModels;
[QueryProperty(nameof(Show), "Show")]
public partial class VideoPlayerViewModel : BasePageViewModel
{
    private Show _show;
    public Show Show
    {
        get => _show;
        set => SetProperty(ref _show, value);
    }
  
    public VideoPlayerViewModel()
    {
        _show ??= new Show();
    }
}
