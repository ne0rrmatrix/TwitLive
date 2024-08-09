using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TwitLive.Primitives;
using TwitLive.Services;

namespace TwitLive.ViewModels;
public partial class BasePageViewModel : ObservableObject, IDisposable
{
	[ObservableProperty]
	double percentagBar;
	[ObservableProperty]
	int orientation;
	[ObservableProperty]
	string percentageLabel = string.Empty;
	[ObservableProperty]
	bool isRefreshing;
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(IsNotBusy))]
	bool isBusy;
	public bool IsNotBusy => !IsBusy;
	public DisplayInfo MyMainDisplay { get; set; }
	
	bool disposedValue;
	readonly CancellationToken cancellationToken;
	public CancellationToken CancellationToken => cancellationToken;
	public readonly FeedService FeedService;
	public BasePageViewModel()
	{
		cancellationToken = new();
		MyMainDisplay = new();
		FeedService = new FeedService();
		Orientation = IdiomOrientation.Span;
		OnPropertyChanged(nameof(Orientation));
		ArgumentNullException.ThrowIfNull(App.Download);
		App.Download.ProgressChanged += Progress_ProgressChanged;
	}

	[RelayCommand]
	public Task SetIsBusy()
	{
		System.Diagnostics.Trace.TraceInformation("BasePageViewModel");
		IsBusy = false;
		return Task.CompletedTask;
	}

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
				if(App.Download is not null)
				{
					App.Download.ProgressChanged -= Progress_ProgressChanged;
				}

				DeviceDisplay.MainDisplayInfoChanged -= DeviceDisplayMainDisplayInfoChanged;
				FeedService.Dispose();
			}
			disposedValue = true;
		}
	}

	public void Progress_ProgressChanged(object? sender, DownloadProgressEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(App.Download);
		GetDispatcher.Dispatcher?.Dispatch(() => 
		{ 
			double temp = e.Percentage;
			PercentagBar = temp/100;
			IsBusy = true;
			PercentageLabel = $"Percent done: {temp}%";
			OnPropertyChanged(nameof(PercentageLabel));
			OnPropertyChanged(nameof(IsBusy));
		});
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