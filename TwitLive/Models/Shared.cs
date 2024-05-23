using CommunityToolkit.Mvvm.ComponentModel;

namespace TwitLive.Models;
public partial class Shared: ObservableObject
{
    string _title = string.Empty;
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }
    string _description = string.Empty;
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }
    string _link = string.Empty;
    public string Link
    {
        get => _link;
        set => SetProperty(ref _link, value);
    }
    string _image = string.Empty;
    public string Image
    {
        get => _image;
        set => SetProperty(ref _image, value);
    }
    string _url = string.Empty;
    public string Url
    {
        get => _url;
        set => SetProperty(ref _url, value);
    }
    DateTime _pubDate = DateTime.Now;
    public DateTime PubDate
    {
        get => _pubDate;
        set => SetProperty(ref _pubDate, value);
    }
}
