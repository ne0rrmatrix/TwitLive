using TwitLive.Models;

namespace TwitLive.Services;
public static class PodcastService
{
    public static List<Podcast> GetPodcasts()
    {
        var Podcasts = new List<Podcast>();
        var podcastList = FeedService.GetPodcastFeed();
        foreach (var item in podcastList)
        {
            var podcast = FeedService.GetPodcastList(item);
            Podcasts.Add(podcast);
        }
        return Podcasts;
    }
}
