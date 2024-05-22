using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Web;
using TwitLive.Models;
using TwitLive.Services;
using TwitLive.Views;

namespace TwitLive.ViewModels;
public partial class PodcastPageViewModel : BasePageViewModel
{
    [ObservableProperty]
    private ObservableCollection<Podcast> _podcasts;
    public PodcastPageViewModel()
    {
        Title = "Podcasts";
        var item = PodcastService.GetPodcasts();
        _podcasts = new ObservableCollection<Podcast>(item);
    }

    /// <summary>
    /// A Method that passes a Url <see cref="string"/> to <see cref="ShowPage"/>
    /// </summary>
    /// <param name="podcast">A Url <see cref="string"/></param>
    /// <returns></returns>
    [RelayCommand]
    public async Task GotoShowPage(Podcast podcast)
    {
        var encodedUrl = HttpUtility.UrlEncode(podcast.Url);
        await Shell.Current.GoToAsync($"{nameof(ShowPage)}?Url={encodedUrl}");
    }
}
