using TwitLive.Database;
using TwitLive.Interfaces;

namespace TwitLive;
public partial class App : Application
{
	public static IDownload? Download { get; private set; }
	public App(IDownload download)
	{
		Download = download;
		InitializeComponent();
		MainPage = new AppShell();
	}
}