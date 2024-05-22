using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using TwitLive.Models;

namespace TwitLive.Services;

/// <summary>
/// A class that manages getting <see cref="Podcast"/> and <see cref="Show"/> from RSS feeds.
/// </summary>
public static class FeedService
{
    /// <summary>
    /// Get OPML file from web and return list of current Podcasts.
    /// </summary>
    /// <returns><see cref="List{T}"/> <see cref="string"/> of URLs</returns>
    public static List<string> GetPodcastFeed()
    {
        List<string> list = [];
        try
        {
            var item = "https://feeds.twit.tv/twitshows_video_hd.opml";
            var reader = new XmlTextReader(item);
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        while (reader.MoveToNextAttribute()) // Read the attributes.
                        {
                            if (reader.Name == "xmlUrl")
                            {
                                list.Add(reader.Value);
                            }
                        }
                        break;
                }
            }
            return list;
        }
        catch
        {
            return list;
        }
    }

    /// <summary>
    /// Method <c>GetPodcastList</c> return Feed from URL.
    /// </summary>
    /// <param name="item">The URL of <see cref="Podcast"/></param> 
    /// <returns><see cref="Podcast"/></returns>
    public static Podcast GetPodcastList(string? item)
    {
        Podcast feed = new();
        if (item == null)
        {
            return feed;
        }
        try
        {
            foreach (var level1Element in XElement.Load(item).Elements("channel"))
            {
                feed.Title = level1Element.Element("title")?.Value ?? string.Empty;
                feed.Description = level1Element.Element("description")?.Value ?? string.Empty;
                feed.Url = item;

                foreach (var level2Element in level1Element.Elements("image"))
                {
                    feed.Image = level2Element.Element("url")?.Value ?? string.Empty;
                }
            }

            return feed;
        }
        catch
        {
            return feed;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="items">The Url of the <see cref="Show"/></param>
    /// <returns><see cref="List{T}"/> <see cref="Show"/></returns>
    public static List<Show> GetShowList(string items)
    {
        List<Show> shows = [];
        XmlDocument rssDoc = new();
        try
        {
            var iTunesNamespace = "http://www.itunes.com/dtds/podcast-1.0.dtd";
            var mediaNamespace = "http://search.yahoo.com/mrss/";
            rssDoc.Load(items);
            var mgr = new XmlNamespaceManager(rssDoc.NameTable);
            mgr.AddNamespace("itunes", iTunesNamespace);
            mgr.AddNamespace("media", mediaNamespace);
            var rssNodes = rssDoc.SelectNodes("/rss/channel/item");
            if (rssNodes == null)
            {
                return shows;
            }
            for (var i = 0; i < rssNodes.Count; i++)
            {
                if (rssNodes[i] == null)
                {
                    continue;
                }
                Show show = new()
                {
                    Description = rssNodes[i]?.SelectSingleNode("description") != null ? rssNodes[i]?.SelectSingleNode("description")?.InnerText ?? string.Empty : string.Empty,
                    PubDate = ConvertToDateTime(rssNodes[i]?.SelectSingleNode("pubDate") != null ? rssNodes[i]?.SelectSingleNode("pubDate")?.InnerText ?? string.Empty : string.Empty),
                    Title = rssNodes[i]?.SelectSingleNode("title") != null ? rssNodes[i]?.SelectSingleNode("title")?.InnerText ?? string.Empty: string.Empty,
                    Url = rssNodes[i]?.SelectSingleNode("enclosure", mgr) != null ? rssNodes[i]?.SelectSingleNode("enclosure", mgr)?.Attributes?["url"]?.InnerText ?? string.Empty: string.Empty,
                    Image = rssNodes[i]?.SelectSingleNode("itunes:image", mgr) != null ? rssNodes[i]?.SelectSingleNode("itunes:image", mgr)?.Attributes?["href"]?.InnerText ?? string.Empty : string.Empty,
                };
                shows.Add(show);
            }
            return shows;
        }
        catch
        {
            return Enumerable.Empty<Show>().ToList();
        }
    }

    /// <summary>
    /// Method returns <see cref="DateTime"/> object from string.
    /// </summary>
    /// <param name="dateTime"> DateTime <see cref="string"/></param>
    /// <returns><see cref="DateTime"/></returns>
    public static DateTime ConvertToDateTime(string dateTime)
    {
        return DateTime.Parse(dateTime.Remove(25), new CultureInfo("en-US"));
    }
}

