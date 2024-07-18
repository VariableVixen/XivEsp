using System;
using System.Numerics;

using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

using ImGuiNET;

namespace PrincessRTFM.XivEsp;

internal class ConfigWindow: Window {
	internal ConfigWindow() : base($"{Constants.PluginName}Config", ImGuiNET.ImGuiWindowFlags.AlwaysAutoResize) {
		this.AllowClickthrough = false;
		this.AllowPinning = true;
		this.SizeConstraints = new WindowSizeConstraints() {
			MinimumSize = new(500, 200),
		};
	}
	public override void OnOpen() {
		base.OnOpen();
		this.Collapsed = false;
		this.CollapsedCondition = ImGuiCond.Appearing;
	}

	public override void Draw() {
		bool save = false;
		if (Service.ClientState.IsPvP) {
			// TODO pvp warning
		}

		bool dtrVisModeChanged = ImGui.Checkbox("Hide server info bar entry when no search is set?", ref Service.Config.HideInfoBarEntryWhenNoSearchSet);
		save |= dtrVisModeChanged;

		ImGui.Spacing();
		ImGui.Spacing();

		ImGui.TextUnformatted("Default chat channel for messages: ");
		ImGui.SameLine();
		save |= EnumCombo("###DefaultChatMessageChannel", ref Service.Config.ChatLogChannel);
		ImGui.Indent();
		ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.7f, 0.7f, 0.7f, 1f));
		ImGui.TextUnformatted("If you set this to \"None\", then Dalamud's default (defined in \"/xlsettings\") will be used.");
		ImGui.TextUnformatted($"Currently, that channel is {Service.Interface.GeneralChatType}.");
		ImGui.PopStyleColor();
		ImGui.Unindent();

		if (save) {
			Service.Interface.SavePluginConfig(Service.Config);
			SearchManager.UpdateStatusBar();
			if (dtrVisModeChanged)
				Chat.PrintInfoBarState();
		}
	}

	protected static bool EnumCombo<T>(string label, ref T refValue) where T : Enum {
		using ImRaii.IEndObject combo = ImRaii.Combo(label, refValue.ToString());
		if (!combo)
			return false;

		foreach (Enum enumValue in Enum.GetValues(refValue.GetType())) {
			if (!ImGui.Selectable(enumValue.ToString(), enumValue.Equals(refValue)))
				continue;

			refValue = (T)enumValue;
			return true;
		}

		return false;
	}
}
