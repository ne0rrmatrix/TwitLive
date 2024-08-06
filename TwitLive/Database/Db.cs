using MetroLog;
using SQLite;
using TwitLive.Interfaces;
using TwitLive.Models;

namespace TwitLive.Database;
public class Db : IDb
{
	public static string DbPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyData.db");
	SQLiteAsyncConnection? db;
	public const SQLite.SQLiteOpenFlags Flags = SQLite.SQLiteOpenFlags.ReadWrite | SQLite.SQLiteOpenFlags.Create | SQLite.SQLiteOpenFlags.SharedCache;
	readonly ILogger logger =  LoggerFactory.GetLogger(nameof(Db));
	public Db()
	{
	}
	public async Task Init()
	{
		if (db is not null)
		{
			return;
		}
		db = new SQLiteAsyncConnection(DbPath, Flags);
		logger.Info("Database created");
		await db.CreateTableAsync<Show>();
		logger.Info("Table created");
	}
	public async Task<List<Show>> GetShowsAsync()
	{
		await Init();
		if (db is null)
		{
			return [];
		}
		return await db.Table<Show>().ToListAsync();
	}

	public async Task<Show> GetShowAsync(string title)
	{
		await Init();
		if (db is null)
		{
			return new Show();
		}
		return await db.Table<Show>().Where(i => i.Title == title).FirstOrDefaultAsync();
	}

	public async Task SaveShowAsync(Show show)
	{
		await Init();
		if (db is null)
		{
			return;
		}
		var item = await db.Table<Show>().Where(i => i.Title == show.Title).FirstOrDefaultAsync();
		if (item is null)
		{
			await db.InsertAsync(show);
		}
		else
		{
			await db.UpdateAsync(show);
		}
	}

	public async Task DeleteShowAsync(Show show)
	{
		await Init();
		if (db is null)
		{
			return;
		}
		var item = await db.Table<Show>().Where(i => i.Title == show.Title).FirstOrDefaultAsync();
		if (item is null)
		{
			return;
		}
		await db.DeleteAsync(show);
	}

	public async Task DeleteAllShows()
	{
		await Init();
		if (db is null)
		{
			return;
		}
		await db.DeleteAllAsync<Show>();
	}

	public async Task SaveAllShowsAsync(List<Show> shows)
	{
		await Init();
		if (db is null)
		{
			return;
		}
		await db.InsertAllAsync(shows);
	}
}