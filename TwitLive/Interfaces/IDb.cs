using TwitLive.Models;

namespace TwitLive.Interfaces;
public interface IDb
{
	public Task<List<Show>> GetShowsAsync();
	public Task<Show> GetShowAsync(string title);
	public Task SaveShowAsync(Show show);

	public Task DeleteShowAsync(Show show);
	public Task DeleteAllShows();
	public Task SaveAllShowsAsync(List<Show> shows);
	public Task Init();
}
