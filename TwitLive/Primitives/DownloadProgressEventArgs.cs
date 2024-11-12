namespace TwitLive.Primitives;

public class DownloadProgressEventArgs(DownloadStatus status, double Percentage) : EventArgs
{
	public double Percentage { get; set; } = Percentage;
	public DownloadStatus Status { get; } = status;
}