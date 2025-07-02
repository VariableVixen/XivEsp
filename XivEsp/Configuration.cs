using Dalamud.Configuration;
using Dalamud.Game.Text;

namespace VariableVixen.XivEsp;

internal class Configuration: IPluginConfiguration {
	public int Version { get; set; } = 1;

	public bool HideInfoBarEntryWhenNoSearchSet = false;
	public XivChatType ChatLogChannel = XivChatType.None;
}
