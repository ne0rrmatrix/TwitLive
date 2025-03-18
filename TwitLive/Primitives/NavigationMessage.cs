using CommunityToolkit.Mvvm.Messaging.Messages;
using TwitLive.Models;

namespace TwitLive.Primitives;
public class NavigationMessage(bool value, DownloadStatus status, Show? show) : ValueChangedMessage<bool>(value)
{
	readonly DownloadStatus status = status;
	readonly Show? show = show;
	public DownloadStatus Status => status;
	public bool value => Value;
	public Show? Show => show;
}
