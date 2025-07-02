using System;

using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace VariableVixen.XivEsp;

public class IPC: IDisposable {
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

	public static string GetSubstringSearch() => SearchManager.Substring;
	public static string GetGlobSearch() => SearchManager.GlobPattern;
	public static string GetRegexSearch() => SearchManager.RegexPattern;
	public static string GetUnifiedSearch() {
		string
			substring = GetSubstringSearch(),
			glob = GetGlobSearch(),
			regex = GetRegexSearch();

		return !string.IsNullOrEmpty(substring)
			? $"{Constants.StatusIndicatorSubstring}:{substring}"
			: !string.IsNullOrEmpty(glob)
			? $"{Constants.StatusIndicatorGlob}:{glob}"
			: !string.IsNullOrEmpty(regex)
			? $"{Constants.StatusIndicatorRegex}:{regex}"
			: Constants.StatusIndicatorNone;
	}

	public static bool HasAnySearch() => SearchManager.HasAny;

	public static void ClearSearch() => SearchManager.ClearSearch();

	public static void SetSubstringSearch(string pattern) => Service.Commands.HandleCommand(Constants.CommandSetSubstring, pattern);
	public static void SetGlobSearch(string pattern) => Service.Commands.HandleCommand(Constants.CommandSetGlob, pattern);
	public static void SetRegexSearch(string pattern) => Service.Commands.HandleCommand(Constants.CommandSetRegex, pattern);

	internal IPC(IDalamudPluginInterface pi) {

		this.ipcGetSubstring = pi.GetIpcProvider<string>(Constants.IpcNameGetSubstringSearch);
		this.ipcGetSubstring.RegisterFunc(GetSubstringSearch);

		this.ipcGetGlob = pi.GetIpcProvider<string>(Constants.IpcNameGetGlobSearch);
		this.ipcGetGlob.RegisterFunc(GetGlobSearch);

		this.ipcGetRegex = pi.GetIpcProvider<string>(Constants.IpcNameGetRegexSearch);
		this.ipcGetRegex.RegisterFunc(GetRegexSearch);

		this.ipcGetUnified = pi.GetIpcProvider<string>(Constants.IpcNameGetUnifiedSearch);
		this.ipcGetUnified.RegisterFunc(GetUnifiedSearch);

		this.ipcHasAnySearch = pi.GetIpcProvider<bool>(Constants.IpcNameHasAnySearch);
		this.ipcHasAnySearch.RegisterFunc(HasAnySearch);

		this.ipcClearSearch = pi.GetIpcProvider<object>(Constants.IpcNameClearSearch);
		this.ipcClearSearch.RegisterAction(ClearSearch);

		this.ipcSetSubstring = pi.GetIpcProvider<string, object>(Constants.IpcNameSetSubstringSearch);
		this.ipcSetSubstring.RegisterAction(SetSubstringSearch);

		this.ipcSetGlob = pi.GetIpcProvider<string, object>(Constants.IpcNameSetGlobSearch);
		this.ipcSetGlob.RegisterAction(SetGlobSearch);

		this.ipcSetRegex = pi.GetIpcProvider<string, object>(Constants.IpcNameSetRegexSearch);
		this.ipcSetRegex.RegisterAction(SetRegexSearch);
	}

	#region Disposable
	private bool disposed;

	protected virtual void Dispose(bool disposing) {
		if (this.disposed)
			return;
		this.disposed = true;

		if (disposing) {
			this.ipcGetSubstring.UnregisterFunc();
			this.ipcGetGlob.UnregisterFunc();
			this.ipcGetRegex.UnregisterFunc();
			this.ipcGetUnified.UnregisterFunc();
			this.ipcHasAnySearch.UnregisterFunc();
			this.ipcClearSearch.UnregisterAction();
			this.ipcSetSubstring.UnregisterAction();
			this.ipcSetGlob.UnregisterAction();
			this.ipcSetRegex.UnregisterAction();
		}
	}

	public void Dispose() {
		this.Dispose(true);
		GC.SuppressFinalize(this);
	}

	#endregion

}
