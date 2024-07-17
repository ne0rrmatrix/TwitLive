using TwitLive.Primitives;

namespace TwitLive.Models;
public partial class Show : Shared, IDisposable
{
	CancellationTokenSource cancellationTokenSource = new();
	public CancellationTokenSource CancellationTokenSource
	{
		get => cancellationTokenSource;
		set => SetProperty(ref cancellationTokenSource, value);
	}
	string fileName = string.Empty;
	public string FileName
	{
		get => fileName;
		set => SetProperty(ref fileName, value);
	}
	bool isDownloaded = false;
	public bool IsDownloaded
	{
		get => isDownloaded;
		set => SetProperty(ref isDownloaded, value);
	}
	bool isDownloading = false;
	public bool IsDownloading
	{
		get => isDownloading;
		set => SetProperty(ref isDownloading, value);
	}
	DownloadStatus status = DownloadStatus.NotDownloaded;
	bool disposedValue;

	public DownloadStatus Status
	{
		get => status;
		set => SetProperty(ref status, value);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			if (disposing)
			{
				cancellationTokenSource?.Dispose();
			}
			disposedValue = true;
		}
	}

	~Show()
	{
	     Dispose(disposing: false);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}