using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Web;
using TwitLive.Models;
using TwitLive.Services;
using TwitLive.Views;

namespace TwitLive.ViewModels;
[QueryProperty("Url", "Url")]
public partial class ShowPageViewModel : BasePageViewModel
{
    [ObservableProperty]
    private ObservableCollection<Show>? _shows;
    private string? _url;
    public string? Url
    {
        get => _url;
        set 
        {
            var item = HttpUtility.UrlDecode(value);
            SetProperty(ref _url, item);
            LoadShows();       
        }
    }
    public ShowPageViewModel()
    {
    }
    private void LoadShows()
    {
        if (_url is null)
        {
            return;
        }
        Title = "Shows";
        Shows = new ObservableCollection<Show>(FeedService.GetShowList(_url));
    }

    /// <summary>
    /// A Method that passes a Url <see cref="string"/> to <see cref="ShowPage"/>
    /// </summary>
    /// <param name="show">A Url <see cref="string"/></param>
    /// <returns></returns>
    [RelayCommand]
    public async Task GotoVideoPage(Show show)
    {
        var navigationParameter = new Dictionary<string, object>
        {
            { "Show", show }
        };
        await Shell.Current.GoToAsync($"//VideoPlayerPage", navigationParameter);
    }
}
