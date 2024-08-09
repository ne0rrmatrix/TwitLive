using TwitLive.Models;
using TwitLive.Primitives;

namespace TwitLive.Interfaces;
public interface IDownload
{
	public double Percentage { get; set; }
	public List<Show> shows { get; set; }
	public Task<DownloadStatus> DownloadAsync(Show show, CancellationToken token = default);
	public EventHandler<DownloadProgressEventArgs>? ProgressChanged { get; set; }
}
