namespace TwitLive.Primitives;

public class DownloadProgressEventArgs : EventArgs
{
	public double Percentage { get; set; }
	public DownloadStatus Status { get; }
	public DownloadProgressEventArgs(DownloadStatus status, double Percentage)
	{
		this.Status = status;
		this.Percentage = Percentage;
	}
}