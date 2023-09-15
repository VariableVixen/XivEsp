namespace PrincessRTFM.XivEsp;

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using DotNet.Globbing;

using ImGuiNET;

public class Plugin: IDalamudPlugin {
	public const string
		NoticeOnlyOneSearchAllowed = " Clears other search patterns on use.",
		CommandSetSubstring = "/esp",
		CommandSetGlob = "/espg",
		CommandSetRegex = "/espr",
		CommandClearSearch = "/espc";
	public const ImGuiWindowFlags OverlayWindowFlags = ImGuiWindowFlags.None
		| ImGuiWindowFlags.NoDecoration // NoTitleBar, NoResize, NoScrollbar, NoCollapse
		| ImGuiWindowFlags.NoSavedSettings
		| ImGuiWindowFlags.NoMove
		| ImGuiWindowFlags.NoInputs // NoMouseInputs, NoNav
		| ImGuiWindowFlags.NoFocusOnAppearing
		| ImGuiWindowFlags.NoBackground
		| ImGuiWindowFlags.NoDocking;
	private const StringComparison nocase = StringComparison.OrdinalIgnoreCase;

	public static readonly ImmutableArray<char> GlobSpecialChars = ImmutableArray.Create('*', '?', '[', ']');

	public const float DrawCircleRadius = 11;
	public const float DrawLabelOffsetDistance = 4;
	public static readonly uint DrawColourTargetCircle = ImGui.ColorConvertFloat4ToU32(new(0, 0.8f, 0.2f, 1));
	public static readonly uint DrawColourLabelBackground = ImGui.ColorConvertFloat4ToU32(new(0, 0, 0, 0.45f));
	public static readonly uint DrawColourLabelText = ImGui.ColorConvertFloat4ToU32(new(0.8f, 0.8f, 0.8f, 1));

	public string Name { get; } = typeof(Plugin).Assembly.GetName().Name ?? "XivEsp";

	#region Services
	[PluginService] public static DalamudPluginInterface Interface { get; private set; } = null!;
	[PluginService] public static IObjectTable GameObjects { get; private set; } = null!;
	[PluginService] public static IGameGui GameGui { get; private set; } = null!;
	[PluginService] public static ICommandManager CommandManager { get; private set; } = null!;
	[PluginService] public static ChatGui ChatGui { get; private set; } = null!;
	#endregion

	public Plugin() {
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
		CommandManager.AddHandler(CommandClearSearch, new(this.onCommand) {
			ShowInHelp = true,
			HelpMessage = "Clear your current ESP search and stop tagging things.",
		});
		Interface.UiBuilder.Draw += this.onDraw;
	}

	public bool CheckGameObject(GameObject thing) => GameObject.IsValid(thing) && thing.IsTargetable && !thing.IsDead && this.CheckMatch(thing);

	private void onDraw() {
		ImGuiViewportPtr gameWindow = ImGuiHelpers.MainViewport;
		ImGuiHelpers.ForceNextWindowMainViewport();
		ImGui.SetNextWindowPos(gameWindow.Pos);
		ImGui.SetNextWindowSize(gameWindow.Size);

		if (ImGui.Begin($"###{this.Name}Overlay", OverlayWindowFlags)) {
			ImGuiStylePtr style = ImGui.GetStyle();
			ImDrawListPtr draw = ImGui.GetWindowDrawList();
			Vector2 drawable = gameWindow.Size - style.DisplaySafeAreaPadding;

			foreach (GameObject thing in GameObjects.Where(this.CheckGameObject)) {
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
		if (command.Equals(CommandClearSearch, nocase)) {
			this.Substring = null;
			this.GlobPattern = null;
			this.RegexPattern = null;
			this.ShowUpdatedSearch();
			return;
		}

		if (string.IsNullOrEmpty(arguments)) {
			this.ShowCurrentSearch();
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
			}
			this.ShowUpdatedSearch();
		}
		catch (ArgumentException) {
			this.InvalidSearchPattern();
		}
	}

	#region Chat utilities
	public const ushort
		ChatColourPluginName = 57,
		ChatColourSearchSubstring = 34,
		ChatColourSearchGlob = 43,
		ChatColourSearchRegex = 48,
		ChatColourGlobNotSubstring = 12,
		ChatColourSearchCleared = 22,
		ChatColourNoSearchFound = 14,
		ChatColourInvalidSearchPattern = 17;
	internal SeStringBuilder startChatMessage() => new SeStringBuilder().AddUiForeground(ChatColourPluginName).AddText($"[{this.Name}]").AddUiForegroundOff();

	public void InvalidSearchPattern() {
		ChatGui.PrintError(this.startChatMessage()
			.AddUiForeground(ChatColourInvalidSearchPattern)
			.AddText(" Invalid pattern, please check your syntax")
			.AddUiForegroundOff()
			.BuiltString
		);
	}
	public void ShowUpdatedSearch() {
		SeStringBuilder msg = this.startChatMessage();
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
	public void ShowCurrentSearch() {
		SeStringBuilder msg = this.startChatMessage();
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
				? name.Contains(this.Substring, nocase)
				: this.Glob is not null
				? this.Glob.IsMatch(name)
				: this.Regex is not null && this.Regex.IsMatch(name)
			);
	}
	public bool CheckMatch(GameObject thing) => this.CheckMatch(thing.Name.TextValue);

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
		}
	}
	public void Dispose() {
		this.Dispose(true);
		GC.SuppressFinalize(this);
	}
	#endregion
}
