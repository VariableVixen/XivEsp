using System;

using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace VariableVixen.XivEsp;

internal class Service {

	[PluginService] public static IDalamudPluginInterface Interface { get; private set; } = null!;
	[PluginService] public static IObjectTable GameObjects { get; private set; } = null!;
	[PluginService] public static IGameGui GameGui { get; private set; } = null!;
	[PluginService] public static ICommandManager CommandManager { get; private set; } = null!;
	[PluginService] public static IChatGui ChatGui { get; private set; } = null!;
	[PluginService] public static ICondition Condition { get; private set; } = null!;
	[PluginService] public static ITargetManager Targets { get; private set; } = null!;
	[PluginService] public static IClientState ClientState { get; private set; } = null!;

	[PluginService] public static Plugin Instance { get; internal set; } = null!;
	[PluginService] public static Configuration Config { get; internal set; } = null!;

	public static IPC IPC { get; internal set; } = null!;
	public static Commands Commands { get; internal set; } = null!;
	public static WindowSystem Windows { get; private set; } = null!;

	[PluginService] public static IDtrBar DtrBar { get; private set; } = null!;
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

	public Service() {
		IPC = new(Interface);
		Commands = new();
		StatusEntry = DtrBar.Get(Constants.PluginName);
		Windows = new(Instance.GetType().FullName);
	}
}
