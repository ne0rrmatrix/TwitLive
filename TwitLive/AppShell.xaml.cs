using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.Messaging;
using TwitLive.Primitives;
using TwitLive.Views;

namespace TwitLive;
public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute("//PodcastPage", typeof(PodcastPage));
		Routing.RegisterRoute("//ShowPage", typeof(ShowPage));
		Routing.RegisterRoute("//VideoPlayerPage", typeof(VideoPlayerPage));
		Routing.RegisterRoute("//DownloadsPage", typeof(DownloadsPage));
	}

	protected override void OnNavigated(ShellNavigatedEventArgs args)
	{
		base.OnNavigated(args);
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1416 // Validate platform compatibility
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

#pragma warning restore CA1416 // Validate platform compatibility
#pragma warning restore IDE0079 // Remove unnecessary suppression
#endif
		WeakReferenceMessenger.Default.Send(new NavigationMessage(true, DownloadStatus.NotDownloaded, null));
		System.Diagnostics.Debug.WriteLine($"Navigated to {args.Current.Location}");
	}

#if ANDROID
	protected override bool OnBackButtonPressed()
	{
		// The Windows back button calls Shell.OnBackButtonPressed and then invokes Page.BackButtonBehvior, Android does not. 
		BackButtonBehavior? bb = CurrentPage.GetPropertyIfSet(BackButtonBehaviorProperty, returnIfNotSet: null as BackButtonBehavior);
		if (bb is not null)
		{
			if (bb.IsVisible && bb.IsEnabled && bb.Command != null)
			{
				bb.Command?.Execute(null);
			}

			return true; // do not do anything further
		}
		return base.OnBackButtonPressed();
	}
#endif
}