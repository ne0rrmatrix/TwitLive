using CommunityToolkit.Mvvm.Messaging.Messages;

namespace TwitLive.Primitives;
public class NavigationMessage : ValueChangedMessage<bool>
{
	readonly DownloadStatus? status;
	public DownloadStatus? Status => status;
	public bool value => Value;
	public NavigationMessage(bool value, DownloadStatus? status) : base(value)
	{
		this.status = status;
	}
}
