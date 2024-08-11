namespace TwitLive.Primitives;

public class DownloadProgressEventArgs : EventArgs
{
	public double Percentage { get; set; }
	public DownloadStatus Status { get; }
	public bool IsBusy { get; set; }
	public DownloadProgressEventArgs(DownloadStatus status, double Percentage, bool isBusy)
	{
		this.Status = status;
		this.Percentage = Percentage;
		this.IsBusy = isBusy;
	}
}