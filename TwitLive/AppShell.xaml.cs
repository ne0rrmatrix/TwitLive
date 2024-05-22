using TwitLive.Views;

namespace TwitLive;
public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute("//PodcastPage", typeof(PodcastPage));
        Routing.RegisterRoute("//ShowPage", typeof(ShowPage));
        Routing.RegisterRoute("//VideoPlayerPage", typeof(VideoPlayerPage));
    }
}
