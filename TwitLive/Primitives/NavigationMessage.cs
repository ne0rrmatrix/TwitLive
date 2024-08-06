using CommunityToolkit.Mvvm.Messaging.Messages;

namespace TwitLive.Primitives;
public class NavigationMessage : ValueChangedMessage<bool>
{
	public NavigationMessage(bool value) : base(value)
	{
	}
}
