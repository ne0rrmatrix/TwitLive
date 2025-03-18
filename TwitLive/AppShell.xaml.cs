using CommunityToolkit.Mvvm.Messaging;
using TwitLive.Interfaces;
using TwitLive.Primitives;

namespace TwitLive;
public partial class AppShell : Shell
{
	readonly IDb db;
	public AppShell(IDb db)
	{
		InitializeComponent();
		this.db = db;
	}

	protected override void OnNavigated(ShellNavigatedEventArgs args)
	{
		base.OnNavigated(args);

		WeakReferenceMessenger.Default.Send(new NavigationMessage(true, DownloadStatus.NotDownloaded, null));
	}

	protected override async void OnDisappearing()
	{
		base.OnDisappearing();
		if(App.Download is null || App.Download.shows.Count == 0)
		{
			return;
		}
		App.Download.StopDownloads = true;
		await App.Download.CurrentShow.CancellationTokenSource.CancelAsync();

		foreach (var item in App.Download.shows)
		{
			await db.DeleteShowAsync(item, CancellationToken.None);
		}
		App.Download.shows.Clear();
	}
}