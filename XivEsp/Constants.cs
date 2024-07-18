using System;
using System.Collections.Immutable;

using Dalamud.Game.ClientState.Conditions;

using ImGuiNET;

namespace PrincessRTFM.XivEsp;

internal static class Constants {

	public const string
		PluginName = "XivEsp",

		CommandSetSubstring = "/esp",
		CommandSetGlob = "/espg",
		CommandSetRegex = "/espr",
		CommandSearchForTargetSubstring = "/espt",
		CommandClearSearch = "/espc",

		NoticeClickStatusToClearSearch = "Click to clear your current search.",
		NoticeUsageReminder = $"No search is currently active.\nUse {CommandSetSubstring}, {CommandSetGlob}, or {CommandSetRegex} to set a substring, glob, or regex search.",
		NoticeOnlyOneSearchAllowed = " Clears other search patterns on use.",
		NoticeDisabledInPvp = "ESP is disabled in PVP areas.",

		StatusIndicatorSubstring = "S",
		StatusIndicatorGlob = "G",
		StatusIndicatorRegex = "R",
		StatusIndicatorDisabled = "X",
		StatusIndicatorNone = "N",

		IpcNameGetSubstringSearch = $"{PluginName}.GetSubstring", // void => string
		IpcNameGetGlobSearch = $"{PluginName}.GetGlob", // void => string
		IpcNameGetRegexSearch = $"{PluginName}.GetRegex", // void => string
		IpcNameGetUnifiedSearch = $"{PluginName}.GetSearch", // void => string [returns indicator letter defined above, colon, pattern - if any search present; indicator for no search alone - when no search present]
		IpcNameHasAnySearch = $"{PluginName}.HasAnySearch", // void => bool
		IpcNameClearSearch = $"{PluginName}.ClearSearch", // void => void [action, not func]
		IpcNameSetSubstringSearch = $"{PluginName}.SetSubstring", // string => void [action, not func]
		IpcNameSetGlobSearch = $"{PluginName}.SetGlob", // string => void [action, not func]
		IpcNameSetRegexSearch = $"{PluginName}.SetRegex"; // string => void [action, not func]

	public const ImGuiWindowFlags OverlayWindowFlags = ImGuiWindowFlags.None
		| ImGuiWindowFlags.NoDecoration // NoTitleBar, NoResize, NoScrollbar, NoCollapse
		| ImGuiWindowFlags.NoSavedSettings
		| ImGuiWindowFlags.NoMove
		| ImGuiWindowFlags.NoInputs // NoMouseInputs, NoNav
		| ImGuiWindowFlags.NoFocusOnAppearing
		| ImGuiWindowFlags.NoBackground
		| ImGuiWindowFlags.NoDocking;

	public const ushort
		ChatColourPluginName = 57,
		ChatColourSearchSubstring = 34,
		ChatColourSearchGlob = 43,
		ChatColourSearchRegex = 48,
		ChatColourGlobNotSubstring = 12,
		ChatColourSearchCleared = 22,
		ChatColourNoSearchFound = 14,
		ChatColourInfoBarState = 67,
		ChatColourError = 17;

	public const StringComparison StrCompNoCase = StringComparison.OrdinalIgnoreCase;

	public const float DrawCircleRadius = 11;
	public const float DrawLabelOffsetDistance = 4;
	public static readonly uint DrawColourTargetCircle = ImGui.ColorConvertFloat4ToU32(new(0, 0.8f, 0.2f, 1));
	public static readonly uint DrawColourLabelBackground = ImGui.ColorConvertFloat4ToU32(new(0, 0, 0, 0.45f));
	public static readonly uint DrawColourLabelText = ImGui.ColorConvertFloat4ToU32(new(0.8f, 0.8f, 0.8f, 1));

	public static readonly ImmutableArray<char> GlobSpecialChars = ['*', '?', '[', ']'];

	public static readonly ImmutableArray<ConditionFlag> DisabledConditions = [
		ConditionFlag.OccupiedInCutSceneEvent,
		ConditionFlag.WatchingCutscene,
		ConditionFlag.WatchingCutscene78,
		ConditionFlag.BetweenAreas,
		ConditionFlag.BetweenAreas51,
		ConditionFlag.CreatingCharacter,
	];

}
