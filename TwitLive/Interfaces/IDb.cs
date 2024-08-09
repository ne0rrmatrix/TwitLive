using TwitLive.Models;

namespace TwitLive.Interfaces;
public interface IDb
{
	public Task<List<Show>> GetShowsAsync(CancellationToken cancellationToken = default);
	public Task<Show> GetShowAsync(string title, CancellationToken cancellationToken = default);
	public Task SaveShowAsync(Show show, CancellationToken cancellationToken = default);
	public Task DeleteShowAsync(Show show, CancellationToken cancellationToken = default);
	public Task DeleteAllShows(CancellationToken cancellationToken = default);
	public Task SaveAllShowsAsync(List<Show> shows, CancellationToken cancellationToken = default);
	public Task Init(CancellationToken cancellationToken = default);
}
