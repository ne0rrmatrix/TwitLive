using TwitLive.ViewModels;

namespace TwitLive.Views;

public partial class DownloadsPage : ContentPage
{
	public DownloadsPage(DownloadsPageViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}