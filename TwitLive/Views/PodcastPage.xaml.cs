using CommunityToolkit.Maui.Core;
using TwitLive.ViewModels;

namespace TwitLive.Views;
public partial class PodcastPage : ContentPage
{
	public PodcastPage(PodcastPageViewModel viewModel)
	{
		InitializeComponent();
		
		BindingContext = viewModel;
	}
	protected override void OnNavigatedTo(NavigatedToEventArgs args)
	{
		base.OnNavigatedTo(args);
#if IOS || ANDROID || MACCATALYST
		if (Application.Current?.PlatformAppTheme == AppTheme.Dark)
		{
			CommunityToolkit.Maui.Core.Platform.StatusBar.SetColor(Colors.Black);
			CommunityToolkit.Maui.Core.Platform.StatusBar.SetStyle(StatusBarStyle.LightContent);
		}
		else
		{
			CommunityToolkit.Maui.Core.Platform.StatusBar.SetColor(Color.FromArgb("#E9E9E9"));
			CommunityToolkit.Maui.Core.Platform.StatusBar.SetStyle(StatusBarStyle.DarkContent);
		}
#endif
	}
}