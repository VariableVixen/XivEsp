using System.Linq;

using Dalamud.Game.Text.SeStringHandling;

using VariableVixen.XivEsp.Filters;

namespace VariableVixen.XivEsp;

internal static class Chat {
	internal static SeStringBuilder StartChatMessage() => new SeStringBuilder().AddText($"[{Constants.PluginName}]", Constants.ChatColourPluginName);

	public static void PrintInfoBarState() {
		StartChatMessage()
			.AddText("Server info bar entry will be ")
			.AddText(Service.Config.HideInfoBarEntryWhenNoSearchSet ? "visible" : "hidden", Constants.ChatColourInfoBarState)
			.AddText(" when no search is set.")
			.Print();
	}

	public static void PrintPvpWarning() {
		StartChatMessage()
			.AddText("You are currently in a PvP zone.", Constants.ChatColourError)
			.AddText($"\nESP is disabled while in PvP.")
			.Print();
	}

	public static void PrintInvalidSearch() {
		Service.ChatGui.PrintError(StartChatMessage()
			.AddText(" Invalid pattern, please check your syntax.", Constants.ChatColourError)
			.BuiltString
		);
	}

	public static void PrintMissingTarget() {
		Service.ChatGui.PrintError(StartChatMessage()
			.AddText(" You don't have a target.", Constants.ChatColourError)
			.BuiltString
		);
	}

	public static void PrintUpdatedSearch() {
		ushort foreground = SearchManager.Filter switch {
			NameSubstringFilter => Constants.ChatColourSearchSubstring,
			NameGlobFilter => Constants.ChatColourSearchGlob,
			NameRegexFilter => Constants.ChatColourSearchRegex,
			_ => Constants.ChatColourSearchByType,
		};
		SeStringBuilder msg = StartChatMessage();
		if (SearchManager.Filter is null) {
			msg.AddText(" Cleared search filter", Constants.ChatColourSearchCleared);
		}
		else {
			msg
				.AddText($" Set {SearchManager.Filter.FilterType.ToLower()} filter: ")
				.AddText(SearchManager.Filter.FilterLabel, foreground);
		}
		if (SearchManager.Filter is NameGlobFilter f && !Constants.GlobSpecialChars.Any(f.Pattern.Contains)) {
			msg
				.AddUiForeground(Constants.ChatColourGlobNotSubstring)
				.AddText("\nWarning: globs are ")
				.AddItalics("not")
				.AddText(" substring searches!")
				.AddUiForegroundOff()
				.AddText($" If you want to match your filter anywhere in an object's name, use '{Constants.Command} substring' instead!");
		}
		msg.Print();

		if (Service.ClientState.IsPvP)
			PrintPvpWarning();
	}

	public static void PrintCurrentSearch() {
		ushort foreground = SearchManager.Filter switch {
			NameSubstringFilter => Constants.ChatColourSearchSubstring,
			NameGlobFilter => Constants.ChatColourSearchGlob,
			NameRegexFilter => Constants.ChatColourSearchRegex,
			_ => Constants.ChatColourSearchByType,
		};
		SeStringBuilder msg = StartChatMessage();
		if (SearchManager.Filter is null) {
			msg.AddText(" No filter set", Constants.ChatColourSearchCleared);
		}
		else {
			msg
				.AddText($"[{SearchManager.Filter.FilterType}]", foreground)
				.AddText(SearchManager.Filter.FilterLabel);
		}
		msg.Print();

		if (Service.ClientState.IsPvP)
			PrintPvpWarning();
	}

	public static void PrintDevFuckedUp() => StartChatMessage().AddText(" Internal error: unexpected state", Constants.ChatColourError).Print();
}
