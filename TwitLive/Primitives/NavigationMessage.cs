using CommunityToolkit.Mvvm.Messaging.Messages;
using TwitLive.Models;

namespace TwitLive.Primitives;
public class NavigationMessage : ValueChangedMessage<bool>
{
	readonly DownloadStatus? status;
	readonly Show? show;
	public DownloadStatus? Status => status;
	public bool value => Value;
	public Show? Show => show;
	public NavigationMessage(bool value, DownloadStatus? status, Show? show) : base(value)
	{
		this.show = show;
		this.status = status;
	}
}
