using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;
using TwitLive.Primitives;

namespace TwitLive.Models;
[Table("Shows")]
public partial class Show : Shared, IDisposable
{
	[PrimaryKey, AutoIncrement, Column("Id")]
	public int Id { get; set; }
	bool disposedValue;
	CancellationTokenSource cancellationTokenSource = new();
	[Ignore]
	public CancellationTokenSource CancellationTokenSource
	{
		get => cancellationTokenSource;
		set => SetProperty(ref cancellationTokenSource, value);
	}
	[ObservableProperty]
	public partial int Position { get; set; } = 0;
	[ObservableProperty]
	public partial string FileName { get; set; } = string.Empty;
	[ObservableProperty]
	public partial DownloadStatus Status { get;set; } = DownloadStatus.NotDownloaded;
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