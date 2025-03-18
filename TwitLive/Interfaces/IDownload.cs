using TwitLive.Models;
using TwitLive.Primitives;

namespace TwitLive.Interfaces;
public interface IDownload
{
	bool StopDownloads { get; set; }
	double Percentage { get; set; }
	List<Show> shows { get; set; }
	Show CurrentShow { get; set; }
	Task DownloadAsync(Show show, CancellationToken token);
	Task QueDownload(Show? show);
	EventHandler<DownloadProgressEventArgs>? ProgressChanged { get; set; }
}
