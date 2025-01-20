using CommunityToolkit.Mvvm.ComponentModel;

namespace TwitLive.Models;
public partial class Shared : ObservableObject
{
	[ObservableProperty]
	public partial string Title { get; set; } = string.Empty;
	[ObservableProperty]
	public partial string Description { get; set; } = string.Empty;
	[ObservableProperty]
	public partial string Link { get; set; } =  string.Empty;
	[ObservableProperty]
	public partial string Image { get; set; } = string.Empty;
	[ObservableProperty]
	public partial string Url { get; set; } = string.Empty;
	[ObservableProperty]
	public partial DateTime PubDate { get; set; } = DateTime.Now;
}