using TwitLive.ViewModels;

namespace TwitLive;
public partial class App : Application
{
    public static VideoPlayerViewModel VideoPlayerVM { get; set; } = new VideoPlayerViewModel();
    public App()
    {
        InitializeComponent();
        MainPage = new AppShell();
    }
}
