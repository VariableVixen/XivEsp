using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;

namespace VariableVixen.XivEsp;

internal static class Extensions {

	internal static SeStringBuilder AddText(this SeStringBuilder message, string text, ushort foreground) {
		if (foreground == 0)
			return message.AddText(text);

		message.AddUiForeground(foreground);
		message.AddText(text);
		message.AddUiForegroundOff();

		return message;
	}

	internal static void Print(this SeString message) {
		XivChatType channel = Service.Config.ChatLogChannel;
		if (channel is XivChatType.None)
			channel = Service.Interface.GeneralChatType;
		Service.ChatGui.Print(new() {
			Type = channel,
			Message = message,
		});
	}
	internal static void Print(this SeStringBuilder message) => message.BuiltString.Print();

}
