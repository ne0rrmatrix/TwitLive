using SQLite;
using TwitLive.Primitives;

namespace TwitLive.Models;
[Table("Shows")]
public partial class Show : Shared, IDisposable
{
	[PrimaryKey, AutoIncrement, Column("Id")]
	public int ID { get; set; }
	bool disposedValue;
	CancellationTokenSource cancellationTokenSource = new();
	[Ignore]
	public CancellationTokenSource CancellationTokenSource
	{
		get => cancellationTokenSource;
		set => SetProperty(ref cancellationTokenSource, value);
	}
	int position;
	public int Position
	{
		get => position;
		set => SetProperty(ref position, value);
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
	[Ignore]
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