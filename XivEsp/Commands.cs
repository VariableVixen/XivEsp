using System;

using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;

namespace VariableVixen.XivEsp;

public class Commands: IDisposable {

	internal Commands() {
		ICommandManager cmds = Service.CommandManager;

		cmds.AddHandler(Constants.CommandSetSubstring, new(this.HandleCommand) {
			ShowInHelp = true,
			HelpMessage = "Set a case-insensitive substring to search for matchingly-named nearby objects, or display your current search pattern and type." + Constants.NoticeOnlyOneSearchAllowed,
		});
		cmds.AddHandler(Constants.CommandSetGlob, new(this.HandleCommand) {
			ShowInHelp = true,
			HelpMessage = "Set a case-insensitive glob pattern to search for matchingly-named nearby objects, or display your current search pattern and type." + Constants.NoticeOnlyOneSearchAllowed,
		});
		cmds.AddHandler(Constants.CommandSetRegex, new(this.HandleCommand) {
			ShowInHelp = true,
			HelpMessage = "Set a case-insensitive regex pattern to search for matchingly-named nearby objects, or display your current search pattern and type." + Constants.NoticeOnlyOneSearchAllowed,
		});
		cmds.AddHandler(Constants.CommandSearchForTargetSubstring, new(this.HandleCommand) {
			ShowInHelp = true,
			HelpMessage = "Set your search to the name of your current (soft, hard, or focus) target. Uses a plain substring." + Constants.NoticeOnlyOneSearchAllowed,
		});
		cmds.AddHandler(Constants.CommandClearSearch, new(this.HandleCommand) {
			ShowInHelp = true,
			HelpMessage = "Clear your current ESP search and stop tagging things.",
		});

	}

	internal void HandleCommand(string command, string arguments) {
		if (command.Equals(Constants.CommandClearSearch, Constants.StrCompNoCase)) {
			SearchManager.ClearSearch();
			return;
		}

		if (string.IsNullOrEmpty(arguments) && command is not Constants.CommandSearchForTargetSubstring) {
			Chat.PrintCurrentSearch();
			return;
		}

		try {
			switch (command) {
				case Constants.CommandSetSubstring:
					SearchManager.Substring = arguments;
					break;
				case Constants.CommandSetGlob:
					SearchManager.GlobPattern = arguments;
					break;
				case Constants.CommandSetRegex:
					SearchManager.RegexPattern = arguments;
					break;
				case Constants.CommandSearchForTargetSubstring: {
						if (Service.Targets.SoftTarget is IGameObject soft)
							SearchManager.Substring = soft.Name.TextValue;
						else if (Service.Targets.Target is IGameObject hard) {
							SearchManager.Substring = hard.Name.TextValue;
						}
						else if (Service.Targets.FocusTarget is IGameObject focus) {
							SearchManager.Substring = focus.Name.TextValue;
						}
						else {
							Chat.PrintMissingTarget();
							return;
						}
					}
					break;
				default: // unpossible!
					Chat.PrintDevFuckedUp();
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

			cmds.RemoveHandler(Constants.CommandSetSubstring);
			cmds.RemoveHandler(Constants.CommandSetGlob);
			cmds.RemoveHandler(Constants.CommandSetRegex);
			cmds.RemoveHandler(Constants.CommandSearchForTargetSubstring);
			cmds.RemoveHandler(Constants.CommandClearSearch);
		}
	}

	public void Dispose() {
		this.Dispose(true);
		GC.SuppressFinalize(this);
	}

	#endregion

}
