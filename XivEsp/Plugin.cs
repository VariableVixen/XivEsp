using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;

using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Utility;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;

using DotNet.Globbing;

using ImGuiNET;

namespace PrincessRTFM.XivEsp;

public class Plugin: IDalamudPlugin {
	public const string
		Name = "XivEsp",
		CommandSetSubstring = "/esp",
		CommandSetGlob = "/espg",
		CommandSetRegex = "/espr",
		CommandSearchForTargetSubstring = "/espt",
		CommandClearSearch = "/espc",
		CommandToggleHideWhenEmpty = "/esph",
		NoticeClickStatusToClearSearch = "Click to clear your current search.",
		NoticeUsageReminder = $"No search is currently active.\nUse {CommandSetSubstring}, {CommandSetGlob}, or {CommandSetRegex} to set a substring, glob, or regex search.",
		NoticeOnlyOneSearchAllowed = " Clears other search patterns on use.",
		StatusIndicatorSubstring = "S",
		StatusIndicatorGlob = "G",
		StatusIndicatorRegex = "R",
		StatusIndicatorNone = "N",
		IpcNameGetSubstringSearch = $"{Name}.GetSubstring", // void => string
		IpcNameGetGlobSearch = $"{Name}.GetGlob", // void => string
		IpcNameGetRegexSearch = $"{Name}.GetRegex", // void => string
		IpcNameGetUnifiedSearch = $"{Name}.GetSearch", // void => string [returns indicator letter defined above, colon, pattern - if any search present; indicator for no search alone - when no search present]
		IpcNameHasAnySearch = $"{Name}.HasAnySearch", // void => bool
		IpcNameClearSearch = $"{Name}.ClearSearch", // void => void [action, not func]
		IpcNameSetSubstringSearch = $"{Name}.SetSubstring", // string => void [action, not func]
		IpcNameSetGlobSearch = $"{Name}.SetGlob", // string => void [action, not func]
		IpcNameSetRegexSearch = $"{Name}.SetRegex"; // string => void [action, not func]
	public const ImGuiWindowFlags OverlayWindowFlags = ImGuiWindowFlags.None
		| ImGuiWindowFlags.NoDecoration // NoTitleBar, NoResize, NoScrollbar, NoCollapse
		| ImGuiWindowFlags.NoSavedSettings
		| ImGuiWindowFlags.NoMove
		| ImGuiWindowFlags.NoInputs // NoMouseInputs, NoNav
		| ImGuiWindowFlags.NoFocusOnAppearing
		| ImGuiWindowFlags.NoBackground
		| ImGuiWindowFlags.NoDocking;
	public const StringComparison NoCase = StringComparison.OrdinalIgnoreCase;

	private static readonly ConditionFlag[] disabledConditions = new ConditionFlag[] {
		ConditionFlag.OccupiedInCutSceneEvent,
		ConditionFlag.WatchingCutscene,
		ConditionFlag.WatchingCutscene78,
		ConditionFlag.BetweenAreas,
		ConditionFlag.BetweenAreas51,
		ConditionFlag.CreatingCharacter,
	};

	public static readonly ImmutableArray<char> GlobSpecialChars = ImmutableArray.Create('*', '?', '[', ']');

	public const float DrawCircleRadius = 11;
	public const float DrawLabelOffsetDistance = 4;
	public static readonly uint DrawColourTargetCircle = ImGui.ColorConvertFloat4ToU32(new(0, 0.8f, 0.2f, 1));
	public static readonly uint DrawColourLabelBackground = ImGui.ColorConvertFloat4ToU32(new(0, 0, 0, 0.45f));
	public static readonly uint DrawColourLabelText = ImGui.ColorConvertFloat4ToU32(new(0.8f, 0.8f, 0.8f, 1));

	private readonly ICallGateProvider<string>
		ipcGetSubstring,
		ipcGetGlob,
		ipcGetRegex,
		ipcGetUnified;
	private readonly ICallGateProvider<bool> ipcHasAnySearch;
	private readonly ICallGateProvider<object>
		ipcClearSearch;
	private readonly ICallGateProvider<string, object>
		ipcSetSubstring,
		ipcSetGlob,
		ipcSetRegex;

	internal Configuration Config { get; private set; }

	#region Services
	[PluginService] public static IDalamudPluginInterface Interface { get; private set; } = null!;
	[PluginService] public static IObjectTable GameObjects { get; private set; } = null!;
	[PluginService] public static IGameGui GameGui { get; private set; } = null!;
	[PluginService] public static ICommandManager CommandManager { get; private set; } = null!;
	[PluginService] public static IChatGui ChatGui { get; private set; } = null!;
	[PluginService] public static ICondition Condition { get; private set; } = null!;
	[PluginService] public static ITargetManager Target { get; private set; } = null!;

	public static IDtrBarEntry StatusEntry { get; private set; } = null!;
	public static string StatusText {
		get => StatusEntry?.Text?.TextValue ?? string.Empty;
		set {
			if (StatusEntry is not null)
				StatusEntry.Text = value;
		}
	}
	public static string StatusTitle {
		get => StatusEntry?.Tooltip?.TextValue ?? string.Empty;
		set {
			if (StatusEntry is not null)
				StatusEntry.Tooltip = value;
		}
	}
	public static Action? StatusAction {
		get => StatusEntry?.OnClick;
		set {
			if (StatusEntry is not null)
				StatusEntry.OnClick = value;
		}
	}
	#endregion

	public Plugin(IDtrBar dtr) {
		this.Config = Interface.GetPluginConfig() as Configuration ?? new();

		CommandManager.AddHandler(CommandSetSubstring, new(this.onCommand) {
			ShowInHelp = true,
			HelpMessage = "Set a case-insensitive substring to search for matchingly-named nearby objects, or display your current search pattern and type." + NoticeOnlyOneSearchAllowed,
		});
		CommandManager.AddHandler(CommandSetGlob, new(this.onCommand) {
			ShowInHelp = true,
			HelpMessage = "Set a case-insensitive glob pattern to search for matchingly-named nearby objects, or display your current search pattern and type." + NoticeOnlyOneSearchAllowed,
		});
		CommandManager.AddHandler(CommandSetRegex, new(this.onCommand) {
			ShowInHelp = true,
			HelpMessage = "Set a case-insensitive regex pattern to search for matchingly-named nearby objects, or display your current search pattern and type." + NoticeOnlyOneSearchAllowed,
		});
		CommandManager.AddHandler(CommandSearchForTargetSubstring, new(this.onCommand) {
			ShowInHelp = true,
			HelpMessage = "Set your search to the name of your current (hard or soft) target. Uses a plain substring." + NoticeOnlyOneSearchAllowed,
		});
		CommandManager.AddHandler(CommandClearSearch, new(this.onCommand) {
			ShowInHelp = true,
			HelpMessage = "Clear your current ESP search and stop tagging things.",
		});
		CommandManager.AddHandler(CommandToggleHideWhenEmpty, new(this.onCommand) {
			ShowInHelp = true,
			HelpMessage = "Toggle hiding the server info bar entry when no search is set.",
		});
		Interface.UiBuilder.Draw += this.onDraw;
		StatusEntry ??= dtr.Get(Name);
		StatusEntry.Shown = true;
		this.UpdateStatus();

		this.ipcGetSubstring = Interface.GetIpcProvider<string>(IpcNameGetSubstringSearch);
		this.ipcGetSubstring.RegisterFunc(this.GetSubstringSearch);

		this.ipcGetGlob = Interface.GetIpcProvider<string>(IpcNameGetGlobSearch);
		this.ipcGetGlob.RegisterFunc(this.GetGlobSearch);

		this.ipcGetRegex = Interface.GetIpcProvider<string>(IpcNameGetRegexSearch);
		this.ipcGetRegex.RegisterFunc(this.GetRegexSearch);

		this.ipcGetUnified = Interface.GetIpcProvider<string>(IpcNameGetUnifiedSearch);
		this.ipcGetUnified.RegisterFunc(this.GetUnifiedSearch);

		this.ipcHasAnySearch = Interface.GetIpcProvider<bool>(IpcNameHasAnySearch);
		this.ipcHasAnySearch.RegisterFunc(this.HasAnySearch);

		this.ipcClearSearch = Interface.GetIpcProvider<object>(IpcNameClearSearch);
		this.ipcClearSearch.RegisterAction(this.ClearSearch);

		this.ipcSetSubstring = Interface.GetIpcProvider<string, object>(IpcNameSetSubstringSearch);
		this.ipcSetSubstring.RegisterAction(this.SetSubstringSearch);

		this.ipcSetGlob = Interface.GetIpcProvider<string, object>(IpcNameSetGlobSearch);
		this.ipcSetGlob.RegisterAction(this.SetGlobSearch);

		this.ipcSetRegex = Interface.GetIpcProvider<string, object>(IpcNameSetRegexSearch);
		this.ipcSetRegex.RegisterAction(this.SetRegexSearch);
	}

	public bool CheckGameObject(IGameObject thing) => thing is not null && thing.IsValid() && thing.IsTargetable && !thing.IsDead && this.CheckMatch(thing);

	private void onDraw() {
		if (Condition.Any(disabledConditions))
			return;

		ImGuiViewportPtr gameWindow = ImGuiHelpers.MainViewport;
		ImGuiHelpers.ForceNextWindowMainViewport();
		ImGui.SetNextWindowPos(gameWindow.Pos);
		ImGui.SetNextWindowSize(gameWindow.Size);

		if (ImGui.Begin($"###{Name}Overlay", OverlayWindowFlags)) {
			ImGuiStylePtr style = ImGui.GetStyle();
			ImDrawListPtr draw = ImGui.GetWindowDrawList();
			Vector2 drawable = gameWindow.Size - style.DisplaySafeAreaPadding;

			foreach (IGameObject thing in GameObjects.Where(this.CheckGameObject)) {
				if (!GameGui.WorldToScreen(thing.Position, out Vector2 pos))
					continue;
				string label = thing.Name.TextValue;
				Vector2 size = ImGui.CalcTextSize(label);
				Vector2 offset = new(DrawCircleRadius + DrawLabelOffsetDistance);
				Vector2 inside = pos + offset;
				Vector2 outside = inside + size + (style.CellPadding * 2);
				if (outside.X >= drawable.X)
					offset.X = -(DrawCircleRadius + DrawLabelOffsetDistance + size.X + (style.CellPadding.X * 2));
				if (outside.Y >= drawable.Y)
					offset.Y = -(DrawCircleRadius + DrawLabelOffsetDistance + size.Y + (style.CellPadding.Y * 2));
				inside = pos + offset;
				outside = inside + size + (style.CellPadding * 2);

				draw.AddCircle(pos, DrawCircleRadius, DrawColourTargetCircle, 20, 3);
				draw.AddRectFilled(inside, outside, DrawColourLabelBackground, 5, ImDrawFlags.RoundCornersAll);
				draw.AddText(inside + style.CellPadding, DrawColourLabelText, label);
			}

		}

		ImGui.End();
	}
	private void onCommand(string command, string arguments) {
		if (command.Equals(CommandClearSearch, NoCase)) {
			this.Substring = null;
			this.GlobPattern = null;
			this.RegexPattern = null;
			this.UpdateStatus();
			this.PrintUpdatedSearch();
			return;
		}
		if (command.Equals(CommandToggleHideWhenEmpty, NoCase)) {
			this.Config.HideInfoBarEntryWhenNoSearchSet ^= true;
			Interface.SavePluginConfig(this.Config);
			PrintInfoBarState(this.Config.HideInfoBarEntryWhenNoSearchSet);
			return;
		}

		if (string.IsNullOrEmpty(arguments) && command is not CommandSearchForTargetSubstring) {
			this.PrintCurrentSearch();
			return;
		}

		try {
			switch (command) {
				case CommandSetSubstring:
					this.Substring = arguments;
					this.GlobPattern = null;
					this.RegexPattern = null;
					break;
				case CommandSetGlob:
					this.GlobPattern = arguments;
					this.Substring = null;
					this.RegexPattern = null;
					break;
				case CommandSetRegex:
					this.RegexPattern = arguments;
					this.Substring = null;
					this.GlobPattern = null;
					break;
				case CommandSearchForTargetSubstring: {
						if (Target.SoftTarget is IGameObject soft)
							this.onCommand(CommandSetSubstring, soft.Name.TextValue);
						else if (Target.Target is IGameObject hard)
							this.onCommand(CommandSetSubstring, hard.Name.TextValue);
						else
							PrintMissingTarget();
					}
					return;
				default: // unpossible!
					PrintDevFuckedUp();
					break;
			}
			this.UpdateStatus();
			this.PrintUpdatedSearch();
		}
		catch (ArgumentException) {
			PrintInvalidSearch();
		}
	}

	public void UpdateStatus() {
		string
			substring = this.Substring,
			glob = this.GlobPattern,
			regex = this.RegexPattern;
		if (!string.IsNullOrEmpty(substring)) {
			StatusText = $"{Name}: {StatusIndicatorSubstring}";
			StatusTitle = $"Substring search:\n{substring}\n{NoticeClickStatusToClearSearch}";
			StatusAction = this.ClearSearch;
		}
		else if (!string.IsNullOrEmpty(glob)) {
			StatusText = $"{Name}: {StatusIndicatorGlob}";
			StatusTitle = $"Glob search:\n{glob}\n{NoticeClickStatusToClearSearch}";
			StatusAction = this.ClearSearch;
		}
		else if (!string.IsNullOrEmpty(regex)) {
			StatusText = $"{Name}: {StatusIndicatorRegex}";
			StatusTitle = $"Regex search:\n{regex}\n{NoticeClickStatusToClearSearch}";
			StatusAction = this.ClearSearch;
		}
		else {
			StatusText = $"{Name}: {StatusIndicatorNone}";
			StatusTitle = NoticeUsageReminder;
			StatusAction = this.PrintCurrentSearch;
		}
	}

	#region IPC functions

	public string GetSubstringSearch() => this.Substring;
	public string GetGlobSearch() => this.GlobPattern;
	public string GetRegexSearch() => this.RegexPattern;
	public string GetUnifiedSearch() {
		string
			substring = this.Substring,
			glob = this.GlobPattern,
			regex = this.RegexPattern;
		return !string.IsNullOrEmpty(substring)
			? $"{StatusIndicatorSubstring}:{substring}"
			: !string.IsNullOrEmpty(glob)
			? $"{StatusIndicatorGlob}:{glob}"
			: !string.IsNullOrEmpty(regex)
			? $"{StatusIndicatorRegex}:{regex}"
			: StatusIndicatorNone;
	}

	public bool HasAnySearch() => !string.IsNullOrEmpty(this.Substring) || !string.IsNullOrEmpty(this.GlobPattern) || !string.IsNullOrEmpty(this.RegexPattern);

	public void ClearSearch() => this.onCommand(CommandClearSearch, string.Empty);

	public void SetSubstringSearch(string pattern) => this.onCommand(CommandSetSubstring, pattern);
	public void SetGlobSearch(string pattern) => this.onCommand(CommandSetGlob, pattern);
	public void SetRegexSearch(string pattern) => this.onCommand(CommandSetRegex, pattern);

	#endregion

	#region Chat utilities
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
	internal static SeStringBuilder StartChatMessage() => new SeStringBuilder().AddUiForeground(ChatColourPluginName).AddText($"[{Name}]").AddUiForegroundOff();

	public static void PrintInfoBarState(bool hiddenWhenEmpty) {
		ChatGui.Print(StartChatMessage()
			.AddText("Server info bar entry will be ")
			.AddUiForeground(ChatColourInfoBarState)
			.AddText(hiddenWhenEmpty ? "visible" : "hidden")
			.AddUiForegroundOff()
			.AddText(" when no search is set.")
			.BuiltString);
	}
	public static void PrintInvalidSearch() {
		ChatGui.PrintError(StartChatMessage()
			.AddUiForeground(ChatColourError)
			.AddText(" Invalid pattern, please check your syntax")
			.AddUiForegroundOff()
			.BuiltString
		);
	}
	public static void PrintMissingTarget() {
		ChatGui.PrintError(StartChatMessage()
			.AddUiForeground(ChatColourError)
			.AddText(" You don't have a target")
			.AddUiForegroundOff()
			.BuiltString
		);
	}
	public void PrintUpdatedSearch() {
		SeStringBuilder msg = StartChatMessage();
		if (!string.IsNullOrEmpty(this.Substring)) {
			msg
				.AddText(" Set substring pattern: ")
				.AddUiForeground(ChatColourSearchSubstring)
				.AddText(this.Substring)
				.AddUiForegroundOff();
		}
		else if (this.Glob is not null) {
			msg
				.AddText(" Set glob pattern: ")
				.AddUiForeground(ChatColourSearchGlob)
				.AddText(this.GlobPattern)
				.AddUiForegroundOff();
			if (!GlobSpecialChars.Any(this.GlobPattern.Contains)) {
				msg
					.AddUiForeground(ChatColourGlobNotSubstring)
					.AddText("\nWarning: globs are ")
					.AddItalics("not")
					.AddText(" substring searches!")
					.AddUiForegroundOff()
					.AddText($" If you want to match your pattern anywhere in an object's name, use {CommandSetSubstring} instead!");
			}
		}
		else if (this.Regex is not null) {
			msg
				.AddText(" Set regex pattern: ")
				.AddUiForeground(ChatColourSearchGlob)
				.AddText(this.RegexPattern)
				.AddUiForegroundOff();
		}
		else {
			msg
				.AddUiForeground(ChatColourSearchCleared)
				.AddText(" Cleared search pattern")
				.AddUiForegroundOff();
		}
		ChatGui.Print(msg.BuiltString);
	}
	public void PrintCurrentSearch() {
		SeStringBuilder msg = StartChatMessage();
		if (!string.IsNullOrEmpty(this.Substring)) {
			msg
				.AddUiForeground(ChatColourSearchSubstring)
				.AddText("[Substring]")
				.AddUiForegroundOff()
				.AddText(this.Substring);
		}
		else if (this.Glob is not null) {
			msg
				.AddUiForeground(ChatColourSearchGlob)
				.AddText("[Glob]")
				.AddUiForegroundOff()
				.AddText(this.GlobPattern);
		}
		else if (this.Regex is not null) {
			msg
				.AddUiForeground(ChatColourSearchRegex)
				.AddText("[Regex]")
				.AddUiForegroundOff()
				.AddText(this.RegexPattern);
		}
		else {
			msg
				.AddUiForeground(ChatColourNoSearchFound)
				.AddText(" No search active")
				.AddUiForegroundOff();
		}
		ChatGui.Print(msg.BuiltString);
	}
	public static void PrintDevFuckedUp() {
		ChatGui.Print(StartChatMessage()
			.AddUiForeground(ChatColourError)
			.AddText(" Internal error: unexpected state")
			.AddUiForegroundOff()
			.BuiltString
		);
	}

	#endregion

	#region Name matching
	public const RegexOptions PatternMatchOptions = RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline;
	public static GlobOptions GlobMatchOptions { get; } = new() {
		Evaluation = new() {
			CaseInsensitive = true,
		}
	};

	public bool CheckMatch(string name) {
		return !string.IsNullOrEmpty(name)
			&& (!string.IsNullOrEmpty(this.Substring)
				? name.Contains(this.Substring, NoCase)
				: this.Glob is not null
				? this.Glob.IsMatch(name)
				: this.Regex is not null && this.Regex.IsMatch(name)
			);
	}
	public bool CheckMatch(IGameObject thing) => this.CheckMatch(thing.Name.TextValue);

	private string substringSearch = string.Empty;
	[AllowNull]
	public string Substring {
		get => this.substringSearch;
		set => this.substringSearch = string.IsNullOrEmpty(value) ? string.Empty : value;
	}

	public Glob? Glob { get; private set; }
	[AllowNull]
	public string GlobPattern {
		get => this.Glob?.ToString() ?? string.Empty;
		set => this.Glob = string.IsNullOrEmpty(value) ? null : Glob.Parse(value, GlobMatchOptions);
	}

	public Regex? Regex { get; private set; }
	[AllowNull]
	public string RegexPattern {
		get => this.Regex?.ToString() ?? string.Empty;
		set => this.Regex = string.IsNullOrEmpty(value) ? null : new Regex(value, PatternMatchOptions);
	}
	#endregion

	#region Disposable
	private bool disposed;
	protected virtual void Dispose(bool disposing) {
		if (this.disposed)
			return;
		this.disposed = true;

		if (disposing) {
			CommandManager.RemoveHandler(CommandSetSubstring);
			CommandManager.RemoveHandler(CommandSetGlob);
			CommandManager.RemoveHandler(CommandSetRegex);
			CommandManager.RemoveHandler(CommandClearSearch);
			Interface.UiBuilder.Draw -= this.onDraw;
			StatusEntry.Remove();
		}
	}
	public void Dispose() {
		this.Dispose(true);
		GC.SuppressFinalize(this);
	}
	#endregion
}
