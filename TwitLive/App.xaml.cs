using TwitLive.Interfaces;

namespace TwitLive;
public partial class App : Application
{
	readonly AppShell appShell;
	public static IDownload? Download { get; private set; }
	public App(IDownload download, AppShell appShell)
	{
		Download = download;
		InitializeComponent();
		this.appShell = appShell;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		Window window =  new(appShell);
		if (Download?.shows.Count > 0)
		{
			window.Destroying += (s, e) => Download?.shows[0]?.CancellationTokenSource.Cancel();
		}
		return window;
	}
}