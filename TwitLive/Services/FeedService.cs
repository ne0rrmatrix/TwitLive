using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using TwitLive.Models;
using TwitLive.Primitives;

namespace TwitLive.Services;

/// <summary>
/// A class that manages getting <see cref="Podcast"/> and <see cref="Show"/> from RSS feeds.
/// </summary>
public class FeedService : IDisposable
{
	readonly HttpClient httpClient;
	bool disposedValue;

	public FeedService()
	{
		httpClient = new HttpClient();
	}

	public async Task<List<Podcast>> GetPodcasts(CancellationToken cancellationToken = default)
	{
		var Podcasts = new List<Podcast>();
		var podcastList = await GetPodcastFeed(cancellationToken).ConfigureAwait(false);
		foreach (var item in podcastList)
		{
			var podcast = await GetPodcastList(item,cancellationToken).ConfigureAwait(false);
			Podcasts.Add(podcast);
		}
		return Podcasts;
	}

	/// <summary>
	/// Get OPML file from web and return list of current Podcasts.
	/// </summary>
	/// <returns><see cref="List{T}"/> <see cref="string"/> of URLs</returns>
	async Task<List<string>> GetPodcastFeed(CancellationToken cancellationToken = default)
	{
		List<string> list = [];
		string url = $"https://feeds.twit.tv/twitshows_video_hd.opml";

		try
		{
			string xmlContent = await httpClient.GetStringAsync(url,cancellationToken).ConfigureAwait(false);

			using var stringReader = new StringReader(xmlContent);
			using var xmlReader = XmlReader.Create(stringReader);
			while (xmlReader.Read())
			{
				if (xmlReader.NodeType == XmlNodeType.Element)
				{
					while (xmlReader.MoveToNextAttribute())
					{
						if (xmlReader.Name == "xmlUrl")
						{
							list.Add(xmlReader.Value);
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			// Log the exception or handle it as needed
			System.Diagnostics.Trace.TraceError($"An error occurred: {ex.Message}");
		}

		return list;
	}

	/// <summary>
	/// Method <c>GetPodcastList</c> return Feed from URL.
	/// </summary>
	/// <param name="url">The URL of <see cref="Podcast"/></param> 
	/// <returns><see cref="Podcast"/></returns>
	public async Task<Podcast> GetPodcastList(string? url, CancellationToken cancellationToken = default)
	{
		Podcast feed = new();
		if (string.IsNullOrEmpty(url))
		{
			return feed;
		}
		try
		{
			string xmlContent = await httpClient.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
			XElement xElement = XElement.Parse(xmlContent);

			foreach (var level1Element in xElement.Elements("channel"))
			{
				feed.Title = level1Element.Element("title")?.Value ?? string.Empty;
				feed.Description = level1Element.Element("description")?.Value ?? string.Empty;
				feed.Url = url;

				foreach (var level2Element in level1Element.Elements("image"))
				{
					feed.Image = level2Element.Element("url")?.Value ?? string.Empty;
				}
			}

			return feed;
		}
		catch (Exception ex)
		{
			System.Diagnostics.Trace.TraceError($"An error occurred: {ex.Message}");
			return feed;
		}
	}

	/// <summary>
	/// Get <see cref="Show"/> list from URL.
	/// </summary>
	/// <param name="url">The Url of the <see cref="Show"/></param>
	/// <returns><see cref="List{T}"/> <see cref="Show"/></returns>
	public async Task<List<Show>> GetShowListAsync(string? url, CancellationToken cancellationToken = default)
	{
		if(string.IsNullOrEmpty(url))
		{
			return [];
		}
		List<Show> shows = [];
		XmlDocument rssDoc = [];

		try
		{
			string xmlContent = await httpClient.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
			rssDoc.LoadXml(xmlContent);

			var iTunesNamespace = "http://www.itunes.com/dtds/podcast-1.0.dtd";
			var mediaNamespace = "http://search.yahoo.com/mrss/";

			var mgr = new XmlNamespaceManager(rssDoc.NameTable);
			mgr.AddNamespace("itunes", iTunesNamespace);
			mgr.AddNamespace("media", mediaNamespace);

			var rssNodes = rssDoc.SelectNodes("/rss/channel/item");

			if (rssNodes == null)
			{
				return shows;
			}

			foreach (XmlNode node in rssNodes)
			{
				if (node == null)
				{
					continue;
				}

				Show show = new()
				{
					Status = DownloadStatus.NotDownloaded,
					Description = node.SelectSingleNode("description")?.InnerText ?? string.Empty,
					PubDate = ConvertToDateTime(node.SelectSingleNode("pubDate")?.InnerText ?? string.Empty),
					Title = node.SelectSingleNode("title")?.InnerText ?? string.Empty,
					Url = node.SelectSingleNode("enclosure", mgr)?.Attributes?["url"]?.InnerText ?? string.Empty,
					Image = node.SelectSingleNode("itunes:image", mgr)?.Attributes?["href"]?.InnerText ?? string.Empty,
				};
				show.FileName = FileService.GetFileName(show.Url);
				shows.Add(show);
			}

			return shows;
		}
		catch (Exception ex)
		{
			System.Diagnostics.Trace.TraceError($"An error occurred: {ex.Message}");
			return shows;
		}
	}

	/// <summary>
	/// Method returns <see cref="DateTime"/> object from string.
	/// </summary>
	/// <param name="dateTime"> DateTime <see cref="string"/></param>
	/// <returns><see cref="DateTime"/></returns>
	static DateTime ConvertToDateTime(string dateTime)
	{
		return DateTime.Parse(dateTime.Remove(25), new CultureInfo("en-US"));

	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			if (disposing)
			{
				httpClient.Dispose();
			}
			disposedValue = true;
		}
	}
	 ~FeedService()
	{
	     Dispose(disposing: false);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}