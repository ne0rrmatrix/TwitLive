using TwitLive.ViewModels;

namespace TwitLive.Views;

public partial class VideoPlayerPage : ContentPage
{
    public VideoPlayerPage(VideoPlayerViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
	}

    private void Webview_Navigating(System.Object sender, Microsoft.Maui.Controls.WebNavigatingEventArgs e)
    {
        if (e.Url.Contains("https://") || e.Url.Contains("http://"))
        {
            e.Cancel = true;
        }
    }
}