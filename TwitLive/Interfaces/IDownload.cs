
using TwitLive.Models;
using TwitLive.Primitives;

namespace TwitLive.Interfaces;
public interface IDownload
{
	public List<Show> show { get; set; }
	public Task<DownloadStatus> DownloadAsync(Show show, IProgress<double>? progress = default, CancellationToken token = default);
}
