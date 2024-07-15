using Android.App;
using Android.Content.PM;
using Android.OS;

namespace TwitLive;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleInstance, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
	public MainActivity()
	{
	}
	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);
		AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
	}
	void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		System.Diagnostics.Trace.TraceError(e.ToString());
	}
}