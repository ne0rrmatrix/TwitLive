using TwitLive.ViewModels;

namespace TwitLive.Views;

public partial class DownloadsPage : ContentPage
{
	public DownloadsPage(DownloadsPageViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}

	void Webview_Navigating(System.Object sender, Microsoft.Maui.Controls.WebNavigatingEventArgs e)
	{
		if (e.Url.Contains("https://") || e.Url.Contains("http://"))
		{
			e.Cancel = true;
		}
	}
}