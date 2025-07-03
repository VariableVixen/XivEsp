using System.Linq;
using System.Numerics;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Utility;

using ImGuiNET;

using VariableVixen.XivEsp.Filters;

namespace VariableVixen.XivEsp;

public static class SearchManager {

	private static IGameObjectFilter? filter;
	public static IGameObjectFilter? Filter {
		get => filter;
		internal set {
			filter = value;
			UpdateStatusBar();
			Chat.PrintUpdatedSearch();
		}
	}

	public static void ClearSearch() => Filter = null;

	public static void UpdateStatusBar() {
		bool active = Filter is not null;
		if (!active && Service.Config.HideInfoBarEntryWhenNoSearchSet) {
			Service.StatusEntry.Shown = false;
			return;
		}
		Service.StatusEntry.Shown = true;

		if (active && Service.ClientState.IsPvP) {
			Service.StatusText = $"{Constants.PluginName}: {IGameObjectFilter.IdFilterDisabledInPvp}";
			Service.StatusTitle = Constants.NoticeDisabledInPvp;
			Service.StatusAction = null;
			return;
		}

		if (Filter is not null) {
			Service.StatusText = $"{Constants.PluginName}: {Filter.FilterId}";
			Service.StatusTitle = $"{Filter.FilterType} filter:\n{Filter.FilterLabel}\n{Constants.NoticeClickStatusToClearSearch}";
			Service.StatusAction = ClearSearch;
		}
		else {
			Service.StatusText = $"{Constants.PluginName}: {IGameObjectFilter.IdNoFilterSet}";
			Service.StatusTitle = Constants.NoticeUsageReminder;
			Service.StatusAction = Chat.PrintCurrentSearch;
		}
	}

	internal static void Render() {
		if (Filter is null || Service.ClientState.IsPvP || Service.Condition.Any(Constants.disabledConditions))
			return;

		ImGuiViewportPtr gameWindow = ImGuiHelpers.MainViewport;
		ImGuiHelpers.ForceNextWindowMainViewport();
		ImGui.SetNextWindowPos(gameWindow.Pos);
		ImGui.SetNextWindowSize(gameWindow.Size);

		if (ImGui.Begin($"###{Constants.PluginName}Overlay", Constants.OverlayWindowFlags)) {
			ImGuiStylePtr style = ImGui.GetStyle();
			ImDrawListPtr draw = ImGui.GetWindowDrawList();
			Vector2 drawable = gameWindow.Size - style.DisplaySafeAreaPadding;

			foreach (IGameObject thing in Service.GameObjects.Where(Filter.Test)) {
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

}
