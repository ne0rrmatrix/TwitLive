using TwitLive.ViewModels;

namespace TwitLive.Views;

public partial class PodcastPage : ContentPage
{
	public PodcastPage(PodcastPageViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
    }
}