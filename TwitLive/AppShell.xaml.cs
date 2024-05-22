using TwitLive.Views;

namespace TwitLive;
public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(PodcastPage), typeof(PodcastPage));
        Routing.RegisterRoute(nameof(ShowPage), typeof(ShowPage));
        Routing.RegisterRoute(nameof(VideoPlayerPage), typeof(VideoPlayerPage));
    }

    /// <summary>
    /// Method navigates user to Main Page.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    async void GotoFirstPage(object sender, EventArgs e)
    {
        FlyoutIsPresented = false;
        await Current.GoToAsync($"{nameof(PodcastPage)}");
    }

    /// <summary>
    /// Method navigates user to Video Player Page.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    async void GotoVideoPage(object sender, EventArgs e)
    {
        FlyoutIsPresented = false;
        await Current.GoToAsync($"//VideoPlayerPage");
    }
}
