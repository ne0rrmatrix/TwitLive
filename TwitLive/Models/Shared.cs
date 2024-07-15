using CommunityToolkit.Mvvm.ComponentModel;

namespace TwitLive.Models;
public partial class Shared : ObservableObject
{
	string title = string.Empty;
	public string Title
	{
		get => title;
		set => SetProperty(ref title, value);
	}
	string description = string.Empty;
	public string Description
	{
		get => description;
		set => SetProperty(ref description, value);
	}
	string link = string.Empty;
	public string Link
	{
		get => link;
		set => SetProperty(ref link, value);
	}
	string image = string.Empty;
	public string Image
	{
		get => image;
		set => SetProperty(ref image, value);
	}
	string url = string.Empty;
	public string Url
	{
		get => url;
		set => SetProperty(ref url, value);
	}
	DateTime pubDate = DateTime.Now;
	public DateTime PubDate
	{
		get => pubDate;
		set => SetProperty(ref pubDate, value);
	}
}