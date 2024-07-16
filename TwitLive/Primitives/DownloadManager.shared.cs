using TwitLive.Interfaces;
using TwitLive.Models;

namespace TwitLive.Primitives;
public partial class DownloadManager : IDownload
{
	HttpClient? client;
	public Show show { get; set; } = new Show();

	public DownloadManager()
	{
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
		this.show = show;
		var file = FileService.GetFileName(show.Url);
		ArgumentNullException.ThrowIfNull(file);
		ArgumentNullException.ThrowIfNull(progress);

		var url = show.Url;
		client ??= new HttpClient();
		try
		{
			FileService.DeleteFile(url);	
			var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
			if (!response.IsSuccessStatusCode)
			{
				System.Diagnostics.Trace.TraceError($"Error downloading file: {response.StatusCode}");
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
					System.Diagnostics.Trace.TraceInformation("Download cancelled");
					output.Close();
					FileService.DeleteFile(url);
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
		}
		catch (Exception ex)
		{
			System.Diagnostics.Trace.TraceError(ex.Message);
			File.Delete(show.FileName);
			return DownloadStatus.Error;
		}

		return DownloadStatus.Downloaded;
	}
}

public static class FileService
{
	public static void DeleteFile(string url)
	{
		var tempFile = GetFileName(url);
		try
		{
			if (File.Exists(tempFile))
			{
				File.Delete(tempFile);
				System.Diagnostics.Trace.TraceInformation($"Deleted file {tempFile}");
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Trace.TraceError(ex.Message);
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