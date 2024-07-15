using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Controls;

namespace TwitLive.Handlers;

public class MauiRefreshViewHandler : RefreshViewHandler
{
	public MauiRefreshViewHandler()
	{
	}

	protected override void ConnectHandler(RefreshContainer nativeView)
	{
		if (PlatformView != null)
		{
			PlatformView.ManipulationMode = Microsoft.UI.Xaml.Input.ManipulationModes.All;
			PlatformView.ManipulationDelta += PlatformView_ManipulationDelta;
		}

		base.ConnectHandler(nativeView);
	}

	protected override void DisconnectHandler(RefreshContainer nativeView)
	{
		if (PlatformView != null)
		{
			PlatformView.ManipulationDelta -= PlatformView_ManipulationDelta;
		}

		base.DisconnectHandler(nativeView);
	}

	void PlatformView_ManipulationDelta(object sender, Microsoft.UI.Xaml.Input.ManipulationDeltaRoutedEventArgs e)
	{
		if (e.PointerDeviceType is Microsoft.UI.Input.PointerDeviceType.Touch)
		{
			return; // Already managed by the RefreshContainer control itself
		}

		const double minimumCumulativeY = 20;
		double cumulativeY = e.Cumulative.Translation.Y;

		if (cumulativeY > minimumCumulativeY && VirtualView is not null && !VirtualView.IsRefreshing)
		{
			VirtualView.IsRefreshing = true;
		}
	}
}