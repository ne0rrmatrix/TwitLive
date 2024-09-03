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

	protected override Window CreateWindow(IActivationState? activationState)
	{
		Window window = base.CreateWindow(activationState);
		if(Download?.shows.Count > 0)
		{
			window.Destroying += (s,e) => Download?.shows[0]?.CancellationTokenSource.Cancel();
		}
		return window;
	}
}