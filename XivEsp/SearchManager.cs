using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Utility;

using DotNet.Globbing;

using ImGuiNET;

namespace PrincessRTFM.XivEsp;

public static class SearchManager {
	public const RegexOptions PatternMatchOptions = RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline;
	public static GlobOptions GlobMatchOptions { get; } = new() {
		Evaluation = new() {
			CaseInsensitive = true,
		}
	};

	public static bool HasAny => !string.IsNullOrEmpty(Substring) && !string.IsNullOrEmpty(GlobPattern) && !string.IsNullOrEmpty(RegexPattern);

	public static void ClearSearch() {
		substringSearch = string.Empty;
		Glob = null;
		Regex = null;
		Update();
	}
	public static void Update() {
		UpdateStatusBar();
		Chat.PrintUpdatedSearch();
	}

	public static bool CheckMatch(string name) {
		return !string.IsNullOrEmpty(name)
			&& (!string.IsNullOrEmpty(Substring)
				? name.Contains(Substring, Constants.StrCompNoCase)
				: Glob is not null
				? Glob.IsMatch(name)
				: Regex is not null && Regex.IsMatch(name)
			);
	}
	public static bool CheckMatch(IGameObject thing) => CheckMatch(thing.Name.TextValue);
	public static bool FilterGameObject(IGameObject thing) => thing is not null && thing.IsValid() && thing.IsTargetable && !thing.IsDead && CheckMatch(thing);

	public static void UpdateStatusBar() {
		string
			substring = Substring,
			glob = GlobPattern,
			regex = RegexPattern;
		bool
			hasSubstring = !string.IsNullOrEmpty(substring),
			hasGlob = !string.IsNullOrEmpty(glob),
			hasRegex = !string.IsNullOrEmpty(regex),
			hasAny = hasSubstring || hasGlob || hasRegex;

		if (!hasAny && Service.Config.HideInfoBarEntryWhenNoSearchSet) {
			Service.StatusEntry.Shown = false;
			return;
		}
		Service.StatusEntry.Shown = true;

		if (hasAny && Service.ClientState.IsPvP) {
			Service.StatusText = $"{Constants.PluginName}: {Constants.StatusIndicatorDisabled}";
			Service.StatusTitle = Constants.NoticeDisabledInPvp;
			Service.StatusAction = null;
			return;
		}

		if (hasSubstring) {
			Service.StatusText = $"{Constants.PluginName}: {Constants.StatusIndicatorSubstring}";
			Service.StatusTitle = $"Substring search:\n{substring}\n{Constants.NoticeClickStatusToClearSearch}";
			Service.StatusAction = ClearSearch;
		}
		else if (hasGlob) {
			Service.StatusText = $"{Constants.PluginName}: {Constants.StatusIndicatorGlob}";
			Service.StatusTitle = $"Glob search:\n{glob}\n{Constants.NoticeClickStatusToClearSearch}";
			Service.StatusAction = ClearSearch;
		}
		else if (hasRegex) {
			Service.StatusText = $"{Constants.PluginName}: {Constants.StatusIndicatorRegex}";
			Service.StatusTitle = $"Regex search:\n{regex}\n{Constants.NoticeClickStatusToClearSearch}";
			Service.StatusAction = ClearSearch;
		}
		else {
			Service.StatusText = $"{Constants.PluginName}: {Constants.StatusIndicatorNone}";
			Service.StatusTitle = Constants.NoticeUsageReminder;
			Service.StatusAction = Chat.PrintCurrentSearch;
		}
	}

	internal static void Render() {
		if (Service.ClientState.IsPvP || Service.Condition.Any(Constants.DisabledConditions.ToArray()))
			return;

		ImGuiViewportPtr gameWindow = ImGuiHelpers.MainViewport;
		ImGuiHelpers.ForceNextWindowMainViewport();
		ImGui.SetNextWindowPos(gameWindow.Pos);
		ImGui.SetNextWindowSize(gameWindow.Size);

		if (ImGui.Begin($"###{Constants.PluginName}Overlay", Constants.OverlayWindowFlags)) {
			ImGuiStylePtr style = ImGui.GetStyle();
			ImDrawListPtr draw = ImGui.GetWindowDrawList();
			Vector2 drawable = gameWindow.Size - style.DisplaySafeAreaPadding;

			foreach (IGameObject thing in Service.GameObjects.Where(SearchManager.FilterGameObject)) {
				if (!Service.GameGui.WorldToScreen(thing.Position, out Vector2 pos))
					continue;
				string label = thing.Name.TextValue;
				Vector2 size = ImGui.CalcTextSize(label);
				Vector2 offset = new(Constants.DrawCircleRadius + Constants.DrawLabelOffsetDistance);
				Vector2 inside = pos + offset;
				Vector2 outside = inside + size + (style.CellPadding * 2);
				if (outside.X >= drawable.X)
					offset.X = -(Constants.DrawCircleRadius + Constants.DrawLabelOffsetDistance + size.X + (style.CellPadding.X * 2));
				if (outside.Y >= drawable.Y)
					offset.Y = -(Constants.DrawCircleRadius + Constants.DrawLabelOffsetDistance + size.Y + (style.CellPadding.Y * 2));
				inside = pos + offset;
				outside = inside + size + (style.CellPadding * 2);

				draw.AddCircle(pos, Constants.DrawCircleRadius, Constants.DrawColourTargetCircle, 20, 3);
				draw.AddRectFilled(inside, outside, Constants.DrawColourLabelBackground, 5, ImDrawFlags.RoundCornersAll);
				draw.AddText(inside + style.CellPadding, Constants.DrawColourLabelText, label);
			}

		}

		ImGui.End();
	}

	#region Search values

	[AllowNull]
	public static string Substring {
		get => substringSearch;
		set {
			if (string.IsNullOrEmpty(value)) {
				substringSearch = string.Empty;
			}
			else {
				substringSearch = value;
				Glob = null;
				Regex = null;
			}
			Update();
		}
	}
	private static string substringSearch = string.Empty;

	[AllowNull]
	public static string GlobPattern {
		get => Glob?.ToString() ?? string.Empty;
		set {
			if (string.IsNullOrEmpty(value)) {
				Glob = null;
			}
			else {
				substringSearch = string.Empty;
				Glob = Glob.Parse(value, GlobMatchOptions);
				Regex = null;
			}
			Update();
		}
	}
	public static Glob? Glob { get; private set; }

	[AllowNull]
	public static string RegexPattern {
		get => Regex?.ToString() ?? string.Empty;
		set {
			if (string.IsNullOrEmpty(value)) {
				Regex = null;
			}
			else {
				substringSearch = string.Empty;
				Glob = null;
				Regex = new(value, PatternMatchOptions);
			}
			Update();
		}
	}
	public static Regex? Regex { get; private set; }

	#endregion
}
