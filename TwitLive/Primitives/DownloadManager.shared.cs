using MetroLog;
using TwitLive.Interfaces;
using TwitLive.Models;

namespace TwitLive.Primitives;
public partial class DownloadManager : IDownload
{
	HttpClient? client;
	public EventHandler<DownloadProgressEventArgs>? ProgressChanged { get; set; }
	protected virtual void OnProgressChanged(DownloadProgressEventArgs e) => ProgressChanged?.Invoke(null, e);
	public List<Show> show { get; set; }
	IDb db { get; set; }
	readonly ILogger logger = LoggerFactory.GetLogger(nameof(DownloadManager));
	public DownloadManager(IDb db)
	{
		show = [];
		this.db = db;
	}
	public void UseCustomHttpClient(HttpClient? httpClient)
	{
		ArgumentNullException.ThrowIfNull(httpClient);
		httpClient.Dispose();
		httpClient = null;
		client = httpClient;
	}
	public async Task<DownloadStatus> DownloadAsync(Show show, IProgress<double>? progress = default, CancellationToken token = default)
	{
		var file = FileService.GetFileName(show.Url);
		ArgumentNullException.ThrowIfNull(file);
		ArgumentNullException.ThrowIfNull(progress);
		show.IsDownloaded = !show.IsDownloaded;
		show.IsDownloading = !show.IsDownloading;
		show.Status = DownloadStatus.Downloading;
		this.show.Add(show);
		
		var url = show.Url;
		client ??= new HttpClient();
		try
		{
			FileService.DeleteFile(url);	
			var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
			if (!response.IsSuccessStatusCode)
			{
				logger.Info($"Error downloading file: {response.StatusCode}");
				show.IsDownloaded = false;
				show.IsDownloading = false;
				this.show.Remove(show);
				OnProgressChanged(new DownloadProgressEventArgs(DownloadStatus.Error));
				return DownloadStatus.Error;
			}

			var total = response.Content.Headers.ContentLength ?? -1L;
			using var stream = await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
			using var output = new FileStream(show.FileName, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
			var buffer = new byte[2048];
			var isMoreToRead = true;
			var totalRead = 0L;
			do
			{
				if(token.IsCancellationRequested)
				{
					logger.Info("Download cancelled");
					output.Close();
					FileService.DeleteFile(url);
					show.IsDownloaded = false;
					show.IsDownloading = false;
					this.show.Remove(show);
					OnProgressChanged(new DownloadProgressEventArgs(DownloadStatus.Cancelled));
					return DownloadStatus.Cancelled;
				}
				var read = await stream.ReadAsync(buffer, token).ConfigureAwait(false);
				if (read == 0)
				{
					isMoreToRead = false;
				}
				else
				{
					await output.WriteAsync(buffer.AsMemory(0, read), token).ConfigureAwait(false);
					totalRead += read;
					progress.Report((totalRead * 1d) / (total * 1d) * 100);
				}
			} while (isMoreToRead);
			output.Close();
			show.IsDownloading = false;
			show.IsDownloaded = true;
			await db.SaveShowAsync(show).ConfigureAwait(false);
			this.show.Remove(show);
			logger.Info("Download complete");
			OnProgressChanged(new DownloadProgressEventArgs(DownloadStatus.Downloaded));

		}
		catch (Exception ex)
		{
			logger.Info($"Error downloading file: {ex.Message}");
			File.Delete(show.FileName);
			show.IsDownloaded = false;
			show.IsDownloading = false;
			this.show.Remove(show);
			OnProgressChanged(new DownloadProgressEventArgs(DownloadStatus.Error));
			return DownloadStatus.Error;
		}
		return DownloadStatus.Downloaded;
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
		var filename = System.IO.Path.GetFileName(temp);
		return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), filename);
	}
}