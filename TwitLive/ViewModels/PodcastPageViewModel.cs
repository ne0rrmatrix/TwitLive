using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Web;
using System.Windows.Input;
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
    public static async Task GotoShowPage(Podcast podcast)
    {
        var encodedUrl = HttpUtility.UrlEncode(podcast.Url);
        var navigationParameter = new Dictionary<string, object>
        {
            { "Url", encodedUrl }
        };
        await Shell.Current.GoToAsync($"//ShowPage", navigationParameter);
    }

    public ICommand PullToRefreshCommand => new Command(() =>
    {
        IsRefreshing = true;
        IsBusy = true;
        var item = PodcastService.GetPodcasts();
        Podcasts = new ObservableCollection<Podcast>(item);
        IsBusy = false;
        IsRefreshing = false;
    });
}
