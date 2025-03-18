using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using MetroLog;
using TwitLive.Interfaces;
using TwitLive.Models;

namespace TwitLive.Primitives;
public partial class DownloadManager :ObservableObject, IDisposable, IDownload
{
	
	[ObservableProperty]
	public partial string PercentageLabel { get; set; } = string.Empty;
	[ObservableProperty]
	public partial double Percentage { get; set; }
	public bool StopDownloads { get; set; } = false;
	readonly HttpClient? client;
#pragma warning disable IDE0044
	CancellationTokenSource cancellationToken;
#pragma warning restore IDE0044
	bool disposedValue;

	public EventHandler<DownloadProgressEventArgs>? ProgressChanged { get; set; }
	protected virtual void OnProgressChanged(DownloadProgressEventArgs e) => ProgressChanged?.Invoke(this, e);

	[ObservableProperty]
	public partial List<Show> shows { get; set; }

	[ObservableProperty]
	public partial Show CurrentShow { get; set; }
	IDb db { get; set; }
	readonly ILogger logger = LoggerFactory.GetLogger(nameof(DownloadManager));

	public DownloadManager(IDb db)
	{
		shows = [];
		CurrentShow = new();
		this.db = db;
		cancellationToken = new();
		client ??= new HttpClient();
	}

	public async Task QueDownload(Show? show)
	{
		if (show is null)
		{
			return;
		}
		await db.SaveShowAsync(show, CancellationToken.None).ConfigureAwait(false);
		if (shows.Count > 1)
		{
			return;
		}
		await DownloadAsync(show, show.CancellationTokenSource.Token).ConfigureAwait(false);
	}

	void HandleResult(DownloadStatus result, Show show)
	{
		ArgumentNullException.ThrowIfNull(App.Download);
		string toastText = string.Empty;
		double fontSize = 14;
		ToastDuration duration = ToastDuration.Short;
		Application.Current?.Dispatcher?.Dispatch(async () =>
		{
			switch (result)
			{
				case DownloadStatus.Downloaded:
					show.Status = DownloadStatus.Downloaded;
					await db.SaveShowAsync(show, CancellationToken.None).ConfigureAwait(false);
					WeakReferenceMessenger.Default.Send(new NavigationMessage(true, DownloadStatus.Downloaded, show));
					toastText = "Download Complete";
					break;
				case DownloadStatus.NotDownloaded:
					FileService.DeleteFile(show.FileName);
					show.CancellationTokenSource = new();
					show.Status = DownloadStatus.NotDownloaded;
					await db.DeleteShowAsync(show, CancellationToken.None).ConfigureAwait(false);
					WeakReferenceMessenger.Default.Send(new NavigationMessage(true, DownloadStatus.NotDownloaded, show));
					toastText = "Download Cancelled";
					break;
			}
		});
		
		Percentage = 0;
		CurrentShow = new();
		PercentageLabel = string.Empty;
		this.shows.Remove(show);
		Application.Current?.Dispatcher.Dispatch(async () =>
		{
			var toast = Toast.Make(toastText, duration, fontSize);
			await toast.Show(CancellationToken.None).ConfigureAwait(false);
		});

		if (shows.Count > 0 && !StopDownloads)
		{
			ThreadPool.QueueUserWorkItem(async (state) => await DownloadAsync(shows[0], CancellationToken.None).ConfigureAwait(false));
			return;
		}

		Application.Current?.Dispatcher.Dispatch(() =>
		{
			PercentageLabel = string.Empty;
		});
	}

	public async Task DownloadAsync(Show show, CancellationToken token)
	{
		var file = FileService.GetFileName(show.Url);
	
		ArgumentNullException.ThrowIfNull(file);
		ArgumentNullException.ThrowIfNull(client);
		try
		{
			CurrentShow = show;
			var response = await client.GetAsync(show.Url, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
			var total = response.Content.Headers.ContentLength ?? -1L;
			using var stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
			using var output = new FileStream(show.FileName, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
			var buffer = new byte[2048];
			var isMoreToRead = true;
			var totalRead = 0L;
			do
			{
				var read = await stream.ReadAsync(buffer, token).ConfigureAwait(false);
				if (read == 0)
				{
					isMoreToRead = false;
				}
				else
				{
					await output.WriteAsync(buffer.AsMemory(0, read), token).ConfigureAwait(false);
					totalRead += read;
					PercentageLabel = $"";

					var temp = Math.Floor((totalRead * 1d) / (total * 1d) * 100);
				
					if (temp > Percentage)
					{
						Percentage = temp;
						PercentageLabel = $"Percent done: {Percentage}%";
						OnProgressChanged(new DownloadProgressEventArgs(DownloadStatus.Downloaded, Percentage));
						logger.Info($"Download Progress: {Percentage}");
					}
				}
			} while (isMoreToRead);

			output.Close();
			
			logger.Info("Download complete");
			OnProgressChanged(new DownloadProgressEventArgs(DownloadStatus.Downloaded, Percentage));
			HandleResult(DownloadStatus.Downloaded, show);
		}
		catch
		{
			OnProgressChanged(new DownloadProgressEventArgs(DownloadStatus.NotDownloaded, 0));
			HandleResult(DownloadStatus.NotDownloaded, show);
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			if (disposing)
			{
				WeakReferenceMessenger.Default.Unregister<NavigationMessage>(this);
				cancellationToken?.Dispose();
				client?.Dispose();
			}

			disposedValue = true;
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}

public static class FileService
{
	static readonly ILogger logger = LoggerFactory.GetLogger(nameof(FileService));
	public static void DeleteFile(string url)
	{
		var tempFile = GetFileName(url);
		try
		{
			if (File.Exists(tempFile))
			{
				File.Delete(tempFile);
				logger.Info($"Deleted file {tempFile}");
			}
		}
		catch (Exception ex)
		{
			logger.Error($"Error deleting file: {tempFile}, Messsage: {ex.Message}");
		}
	}

	/// <summary>
	/// Get file name from Url <see cref="string"/>
	/// </summary>
	/// <param name="url">A URL <see cref="string"/></param>
	/// <returns>Filename <see cref="string"/> with file extension</returns>
	public static string GetFileName(string url)
	{
		var temp = new Uri(url).LocalPath;
		var filename = Path.GetFileName(temp);
		return Path.Combine(TwitLive.Database.Db.SaveDirectory, filename);
	}
}