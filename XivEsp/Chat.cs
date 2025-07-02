using System.Linq;

using Dalamud.Game.Text.SeStringHandling;

namespace VariableVixen.XivEsp;

internal static class Chat {
	private static SeStringBuilder startChatMessage() => new SeStringBuilder().AddUiForeground(Constants.ChatColourPluginName).AddText($"[{Constants.PluginName}]").AddUiForegroundOff();

	public static void PrintInfoBarState() {
		startChatMessage()
			.AddText("Server info bar entry will be ")
			.AddUiForeground(Constants.ChatColourInfoBarState)
			.AddText(Service.Config.HideInfoBarEntryWhenNoSearchSet ? "visible" : "hidden")
			.AddUiForegroundOff()
			.AddText(" when no search is set.")
			.Print();
	}

	public static void PrintPvpWarning() {
		startChatMessage()
			.AddUiForeground(Constants.ChatColourError)
			.AddText("You are currently in a PvP zone.")
			.AddUiForegroundOff()
			.AddText($"\nESP is disabled while in PvP.")
			.Print();
	}

	public static void PrintInvalidSearch() {
		Service.ChatGui.PrintError(startChatMessage()
			.AddUiForeground(Constants.ChatColourError)
			.AddText(" Invalid pattern, please check your syntax.")
			.AddUiForegroundOff()
			.BuiltString
		);
	}

	public static void PrintMissingTarget() {
		Service.ChatGui.PrintError(startChatMessage()
			.AddUiForeground(Constants.ChatColourError)
			.AddText(" You don't have a target.")
			.AddUiForegroundOff()
			.BuiltString
		);
	}

	public static void PrintUpdatedSearch() {
		SeStringBuilder msg = startChatMessage();
		if (!string.IsNullOrEmpty(SearchManager.Substring)) {
			msg
				.AddText(" Set substring pattern: ")
				.AddUiForeground(Constants.ChatColourSearchSubstring)
				.AddText(SearchManager.Substring)
				.AddUiForegroundOff();
		}
		else if (SearchManager.Glob is not null) {
			msg
				.AddText(" Set glob pattern: ")
				.AddUiForeground(Constants.ChatColourSearchGlob)
				.AddText(SearchManager.GlobPattern)
				.AddUiForegroundOff();
			if (!Constants.GlobSpecialChars.Any(SearchManager.GlobPattern.Contains)) {
				msg
					.AddUiForeground(Constants.ChatColourGlobNotSubstring)
					.AddText("\nWarning: globs are ")
					.AddItalics("not")
					.AddText(" substring searches!")
					.AddUiForegroundOff()
					.AddText($" If you want to match your pattern anywhere in an object's name, use {Constants.CommandSetSubstring} instead!");
			}
		}
		else if (SearchManager.Regex is not null) {
			msg
				.AddText(" Set regex pattern: ")
				.AddUiForeground(Constants.ChatColourSearchGlob)
				.AddText(SearchManager.RegexPattern)
				.AddUiForegroundOff();
		}
		else {
			msg
				.AddUiForeground(Constants.ChatColourSearchCleared)
				.AddText(" Cleared search pattern")
				.AddUiForegroundOff();
		}
		msg.Print();

		if (Service.ClientState.IsPvP)
			PrintPvpWarning();
	}
	public static void PrintCurrentSearch() {
		SeStringBuilder msg = startChatMessage();
		if (!string.IsNullOrEmpty(SearchManager.Substring)) {
			msg
				.AddUiForeground(Constants.ChatColourSearchSubstring)
				.AddText("[Substring]")
				.AddUiForegroundOff()
				.AddText(SearchManager.Substring);
		}
		else if (SearchManager.Glob is not null) {
			msg
				.AddUiForeground(Constants.ChatColourSearchGlob)
				.AddText("[Glob]")
				.AddUiForegroundOff()
				.AddText(SearchManager.GlobPattern);
		}
		else if (SearchManager.Regex is not null) {
			msg
				.AddUiForeground(Constants.ChatColourSearchRegex)
				.AddText("[Regex]")
				.AddUiForegroundOff()
				.AddText(SearchManager.RegexPattern);
		}
		else {
			msg
				.AddUiForeground(Constants.ChatColourNoSearchFound)
				.AddText(" No search active")
				.AddUiForegroundOff();
		}
		msg.Print();

		if (Service.ClientState.IsPvP)
			PrintPvpWarning();
	}

	public static void PrintDevFuckedUp() {
		startChatMessage()
			.AddUiForeground(Constants.ChatColourError)
			.AddText(" Internal error: unexpected state")
			.AddUiForegroundOff()
			.Print();
	}
}
