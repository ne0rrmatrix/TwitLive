using TwitLive.ViewModels;

namespace TwitLive.Views;

public partial class ShowPage : ContentPage
{
	public ShowPage(ShowPageViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}