using Dalamud.Configuration;

namespace PrincessRTFM.XivEsp;

internal class Configuration: IPluginConfiguration {
	public int Version { get; set; } = 1;

	public bool HideInfoBarEntryWhenNoSearchSet = false;
}
