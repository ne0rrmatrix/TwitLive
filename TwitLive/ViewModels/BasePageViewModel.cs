using CommunityToolkit.Mvvm.ComponentModel;
using TwitLive.Services;

namespace TwitLive.ViewModels;
public partial class BasePageViewModel : ObservableObject, IDisposable
{
	readonly CancellationToken cancellationToken;
	public CancellationToken CancellationToken => cancellationToken;
	public readonly FeedService FeedService;
	[ObservableProperty]
	bool isRefreshing;

	/// <summary>
	/// A <see cref="bool"/> instance managed by this class. 
	/// </summary>
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsNotBusy))]
	bool isBusy;

	/// <summary>
	/// A <see cref="bool"/> public property managed by this class.
	/// </summary>
	public bool IsNotBusy => !IsBusy;

	/// <summary>
	/// The <see cref="DisplayInfo"/> instance managed by this class.
	/// </summary>
	public DisplayInfo MyMainDisplay { get; set; }

	/// <summary>
	/// an <see cref="int"/> instance managed by this class. Used to set <see cref="Span"/> of <see cref="GridItemsLayout"/>
	/// </summary>
	[ObservableProperty]
	int orientation;
	bool disposedValue;

	public BasePageViewModel()
	{
		cancellationToken = new();
		MyMainDisplay = new();
		FeedService = new FeedService();
		DeviceDisplay.MainDisplayInfoChanged += DeviceDisplayMainDisplayInfoChanged;
		Orientation = IdiomOrientation.Span;
		OnPropertyChanged(nameof(Orientation));
	}

	/// <summary>
	/// <c>DeviceDisplay_MainDisplayInfoChanged</c> is a method that sets <see cref="Orientation"/>
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	public void DeviceDisplayMainDisplayInfoChanged(object? sender, DisplayInfoChangedEventArgs e)
	{
		MyMainDisplay = DeviceDisplay.Current.MainDisplayInfo;
		OnPropertyChanged(nameof(MyMainDisplay));
		Orientation = IdiomOrientation.Span;
		OnPropertyChanged(nameof(Orientation));
	}
	public record struct GetDispatcher
	{
		public static IDispatcher? Dispatcher
		{
			get
			{
				return Application.Current?.Dispatcher;
			}
		}
	}
	public record struct IdiomOrientation
	{
		public static DeviceIdiom Idiom
		{
			get
			{
				return DeviceInfo.Current.Idiom;
			}
		}
		public static DisplayOrientation Orientation
		{
			get
			{
				return DeviceDisplay.Current.MainDisplayInfo.Orientation;
			}
		}
		public static int Span
		{
			get
			{
				if ((int)DeviceDisplay.Current.MainDisplayInfo.Width <= 1920
					&& (int)DeviceDisplay.Current.MainDisplayInfo.Width != 0
					&& DeviceInfo.Current.Platform == DevicePlatform.WinUI)
				{
					return 2;
				}
				if (Idiom == DeviceIdiom.Phone)
				{
					return Orientation == DisplayOrientation.Portrait ? 1 : 2;
				}
				if (Idiom == DeviceIdiom.Tablet || DeviceInfo.Current.Platform == DevicePlatform.iOS)
				{
					return Orientation == DisplayOrientation.Portrait ? 2 : 3;
				}
				return Idiom == DeviceIdiom.Desktop ? 3 : 2;
			}
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			if (disposing)
			{
				DeviceDisplay.MainDisplayInfoChanged -= DeviceDisplayMainDisplayInfoChanged;
				FeedService.Dispose();
			}
			disposedValue = true;
		}
	}

	~BasePageViewModel()
	{
	     Dispose(disposing: false);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}