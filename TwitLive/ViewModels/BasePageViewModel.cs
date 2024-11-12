using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetroLog;
using TwitLive.Primitives;
using TwitLive.Services;

namespace TwitLive.ViewModels;
public partial class BasePageViewModel : ObservableObject, IDisposable
{
	[ObservableProperty]
	double percentagBar;
	[ObservableProperty]
	int span;
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
	readonly ILogger logger = LoggerFactory.GetLogger(nameof(BasePageViewModel));
	public BasePageViewModel()
	{
		ArgumentNullException.ThrowIfNull(App.Download);
		cancellationToken = new();
		MyMainDisplay = new();
		FeedService = new FeedService();
		Span = IdiomOrientation.Span;
		OnPropertyChanged(nameof(Span));
		DeviceDisplay.Current.MainDisplayInfoChanged += DeviceDisplayMainDisplayInfoChanged;
		App.Download.ProgressChanged += Progress_ProgressChanged;
	}

	[RelayCommand]
	public void SetIsBusy() => IsBusy = true;

	public void DeviceDisplayMainDisplayInfoChanged(object? sender, DisplayInfoChangedEventArgs e)
	{
		MyMainDisplay = DeviceDisplay.Current.MainDisplayInfo;
		OnPropertyChanged(nameof(MyMainDisplay));
		Span = IdiomOrientation.Span;
		OnPropertyChanged(nameof(Span));
	}

	public static IDispatcher Dispatcher => Application.Current?.Dispatcher ?? throw new FormatException("Dispatcher is not found.");
	public record struct IdiomOrientation
	{
		public static DisplayOrientation Orientation => DeviceDisplay.Current.MainDisplayInfo.Orientation;
		public static DisplayInfo MainDisplayInfo => DeviceDisplay.Current.MainDisplayInfo;
		public static DevicePlatform Platform => DeviceInfo.Current.Platform;
		public static DeviceIdiom Idiom => DeviceInfo.Current.Idiom;

		public static int Span
		{
			get
			{
				if ((int)MainDisplayInfo.Width <= 1920
					&& (int)MainDisplayInfo.Width != 0
					&& Platform == DevicePlatform.WinUI)
				{
					return 2;
				}
				if((int)MainDisplayInfo.Width <= 1920
					&& (int)MainDisplayInfo.Width != 0
					&& Platform == DevicePlatform.Android && DeviceInfo.Idiom != DeviceIdiom.Phone)
				{
					return 2;
				}
				if (Idiom == DeviceIdiom.Phone)
				{
					return Orientation == DisplayOrientation.Portrait ? 1 : 2;
				}
				if (Idiom == DeviceIdiom.Tablet || Platform == DevicePlatform.iOS)
				{
					return Orientation == DisplayOrientation.Portrait ? 2 : 3;
				}
				return Idiom == DeviceIdiom.Desktop ? 3 : 2;
			}
		}
	}

	public void Progress_ProgressChanged(object? sender, DownloadProgressEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(App.Download);
		Dispatcher?.Dispatch(() =>
		{
			double temp = e.Percentage;
			PercentagBar = temp / 100;
			IsBusy = true;
			PercentageLabel = $"Percent done: {temp}%";
			OnPropertyChanged(nameof(PercentageLabel));
			OnPropertyChanged(nameof(IsBusy));
		});
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