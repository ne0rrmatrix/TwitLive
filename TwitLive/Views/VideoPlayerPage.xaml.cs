using TwitLive.ViewModels;

namespace TwitLive.Views;

public partial class VideoPlayerPage : ContentPage
{
	public VideoPlayerPage(VideoPlayerViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
    }
}