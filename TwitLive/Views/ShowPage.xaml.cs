using TwitLive.ViewModels;

namespace TwitLive.Views;

public partial class ShowPage : ContentPage
{
	public ShowPage(ShowPageViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	protected override void OnNavigatedTo(NavigatedToEventArgs args)
	{
		base.OnNavigatedTo(args);
		if (App.Download is null)
		{
			System.Diagnostics.Trace.TraceInformation("Download is null");
			return;
		}
		App.Download.StopDownloads = false;
	}
}