using System;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;

using VariableVixen.XivEsp.Filters;

namespace VariableVixen.XivEsp;

public class Commands: IDisposable {

	internal Commands() {
		ICommandManager cmds = Service.CommandManager;

		cmds.AddHandler(Constants.Command, new(this.HandleCommand) {
			ShowInHelp = true,
			HelpMessage = "Check or change the active filter. Subcommands:"
				+ $"\n{Constants.Command} clear -> clear the current filter"
				+ $"\n{Constants.Command} target -> set a substring filter for the full name of your current (soft/hard/focus) target"
				+ $"\n{Constants.Command} substring|string|substr|sub (text) -> set a literal substring name filter"
				+ $"\n{Constants.Command} glob (pattern) -> set a glob name filter"
				+ $"\n{Constants.Command} regex (pattern) -> set a regex name filter"
				+ $"\n{Constants.Command} npc|any -> filter to tag all non-player objects"
				+ $"\n{Constants.Command} dol|gather -> filter to tag all gathering nodes"
				+ $"\n{Constants.Command} current|check -> show current filter (can also omit the subcommand)",
		});

	}

	internal void HandleCommand(string command, string arguments) {
		arguments = arguments.Trim();
		string subcommand = arguments;
		int firstSpace = arguments.IndexOf(' ');
		if (firstSpace > -1) {
			subcommand = arguments[..firstSpace];
			arguments = arguments[firstSpace..].Trim();
		}

		try {
			switch (subcommand.ToLower()) {
				case "clear":
					SearchManager.ClearSearch();
					break;
				case "target":
					if (Service.Targets.SoftTarget is IGameObject soft) {
						SearchManager.Filter = new NameSubstringFilter(soft.Name.TextValue);
					}
					else if (Service.Targets.Target is IGameObject hard) {
						SearchManager.Filter = new NameSubstringFilter(hard.Name.TextValue);
					}
					else if (Service.Targets.FocusTarget is IGameObject focus) {
						SearchManager.Filter = new NameSubstringFilter(focus.Name.TextValue);
					}
					else {
						Chat.PrintMissingTarget();
						return;
					}
					break;
				case "substring":
				case "string":
				case "substr":
				case "sub":
					SearchManager.Filter = new NameSubstringFilter(arguments);
					break;
				case "glob":
					SearchManager.Filter = new NameGlobFilter(arguments);
					break;
				case "regex":
					SearchManager.Filter = new NameRegexFilter(arguments);
					break;
				case "npc":
				case "any":
				case "all":
					SearchManager.Filter = new AnyNonPlayerFilter();
					break;
				case "dol":
				case "gather":
					SearchManager.Filter = new AnyGatheringNodeFilter();
					break;
				case "current":
				case "":
					Chat.PrintCurrentSearch();
					break;
				default: // unknown subcommand
					Chat.StartChatMessage().AddText($" Unknown subcommand: {subcommand}", Constants.ChatColourError).Print();
					break;
			}
		}
		catch (ArgumentException) {
			Chat.PrintInvalidSearch();
		}
	}

	#region Disposable
	private bool disposed;

	protected virtual void Dispose(bool disposing) {
		if (this.disposed)
			return;
		this.disposed = true;

		if (disposing) {
			ICommandManager cmds = Service.CommandManager;

			cmds.RemoveHandler(Constants.Command);
		}
	}

	public void Dispose() {
		this.Dispose(true);
		GC.SuppressFinalize(this);
	}

	#endregion

}
