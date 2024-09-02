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

	public async Task Init(CancellationToken cancellationToken = default)
	{
		if (db is not null)
		{
			return;
		}
		db = new SQLiteAsyncConnection(DbPath, Flags);
		logger.Info("Database created");
		await db.CreateTableAsync<Show>().WaitAsync(cancellationToken).ConfigureAwait(false);
		logger.Info("Table created");
	}
	
	public async Task<List<Show>> GetShowsAsync(CancellationToken cancellationToken = default)
	{
		await Init(cancellationToken);
		if (db is null)
		{
			return [];
		}
		return await db.Table<Show>().ToListAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
	}

	public async Task<Show> GetShowAsync(Show show, CancellationToken cancellationToken = default)
	{
		await Init(cancellationToken).ConfigureAwait(false);
		if (db is null)
		{
			return new Show();
		}
		return await db.Table<Show>().Where(i => i.Url == show.Url).FirstOrDefaultAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
	}

	public async Task SaveShowAsync(Show show, CancellationToken cancellationToken = default)
	{
		await Init(cancellationToken);
		if (db is null)
		{
			return;
		}
		var item = await db.Table<Show>().Where(i => i.Url == show.Url).FirstOrDefaultAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
		if (item is not null)
		{
			await db.DeleteAsync(show).WaitAsync(cancellationToken).ConfigureAwait(false);
			await db.InsertAsync(show).WaitAsync(cancellationToken).ConfigureAwait(false);
			return;
		}
		await db.InsertAsync(show).WaitAsync(cancellationToken).ConfigureAwait(false);
	}

	public async Task DeleteShowAsync(Show? show, CancellationToken cancellationToken = default)
	{
		if (show is null)
		{
			return;
		}
		await Init(cancellationToken).ConfigureAwait(false);
		if (db is null)
		{
			return;
		}
		var item = await db.Table<Show>().Where(i => i.Url == show.Url).FirstOrDefaultAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
		if (item is null)
		{
			logger.Error("DB Entry not found!");
			return;
		}
		await db.DeleteAsync(item).WaitAsync(cancellationToken).ConfigureAwait(false);
		logger.Info($"DB Entry: {item.Title} Deleted!");
	}

	public async Task DeleteAllShows(CancellationToken cancellationToken = default)
	{
		await Init(cancellationToken).ConfigureAwait(false);
		if (db is null)
		{
			return;
		}
		await db.DeleteAllAsync<Show>().WaitAsync(cancellationToken).ConfigureAwait(false);
	}

	public async Task SaveAllShowsAsync(List<Show> shows, CancellationToken cancellationToken = default)
	{
		await Init(cancellationToken);
		if (db is null)
		{
			return;
		}
		await db.InsertAllAsync(shows).WaitAsync(cancellationToken).ConfigureAwait(false);
	}
}