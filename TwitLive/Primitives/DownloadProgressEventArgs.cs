namespace TwitLive.Primitives;

public class DownloadProgressEventArgs : EventArgs
{
	public DownloadStatus Status { get; }
	public DownloadProgressEventArgs(DownloadStatus status)
	{
		this.Status = status;
	}
}