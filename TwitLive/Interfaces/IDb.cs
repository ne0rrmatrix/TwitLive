using TwitLive.Models;

namespace TwitLive.Interfaces;
public interface IDb
{
	Task<List<Show>> GetShowsAsync(CancellationToken cancellationToken = default);
	Task<Show> GetShowAsync(Show show, CancellationToken cancellationToken = default);
	Task SaveShowAsync(Show show, CancellationToken cancellationToken = default);
	Task DeleteShowAsync(Show? show, CancellationToken cancellationToken = default);
	Task DeleteAllShows(CancellationToken cancellationToken = default);
	Task SaveAllShowsAsync(List<Show> shows, CancellationToken cancellationToken = default);
	Task Init(CancellationToken cancellationToken = default);
}
