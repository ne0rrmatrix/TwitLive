using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Web;
using System.Windows.Input;
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
            ThreadPool.QueueUserWorkItem(_ =>
            {
                LoadShows();
            });
        }
    }
    public ShowPageViewModel()
    {
        _shows = [];
    }
    private void LoadShows()
    {
        if (_url is null)
        {
            return;
        }
        var result = FeedService.GetShowList(_url);
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Shows = new ObservableCollection<Show>(result);
        });
    }

    /// <summary>
    /// A Method that passes a Url <see cref="string"/> to <see cref="ShowPage"/>
    /// </summary>
    /// <param name="show">A Url <see cref="string"/></param>
    /// <returns></returns>
    [RelayCommand]
    public static async Task GotoVideoPage(Show show)
    {
        var navigationParameter = new Dictionary<string, object>
        {
            { "Show", show }
        };
        await Shell.Current.GoToAsync($"//VideoPlayerPage", navigationParameter);
    }

    public ICommand PullToRefreshCommand => new Command(() =>
    {
        IsRefreshing = true;
        IsBusy = true;
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(IsRefreshing));
        if(Url is null)
        {
            IsBusy = false;
            IsRefreshing = false;
            return;
        }
        Shows = new ObservableCollection<Show>(FeedService.GetShowList(Url));
        IsBusy = false;
        IsRefreshing = false;
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(IsRefreshing));
    });
}
