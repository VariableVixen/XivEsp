using System;

using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

using VariableVixen.XivEsp.Filters;

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

	public static string GetSubstringSearch() => SearchManager.Filter is NameSubstringFilter f ? f.Filter : string.Empty;
	public static string GetGlobSearch() => SearchManager.Filter is NameGlobFilter f ? f.Pattern : string.Empty;
	public static string GetRegexSearch() => SearchManager.Filter is NameRegexFilter f ? f.Pattern : string.Empty;
	public static string GetUnifiedSearch() => SearchManager.Filter is null ? IGameObjectFilter.IdNoFilterSet.ToString() : SearchManager.Filter.FilterId.ToString() + ":" + SearchManager.Filter.FilterLabel;

	public static bool HasAnySearch() => SearchManager.Filter is not null;

	public static void ClearSearch() => SearchManager.ClearSearch();

	public static void SetSubstringSearch(string pattern) => SearchManager.Filter = new NameSubstringFilter(pattern);
	public static void SetGlobSearch(string pattern) => SearchManager.Filter = new NameGlobFilter(pattern);
	public static void SetRegexSearch(string pattern) => SearchManager.Filter = new NameRegexFilter(pattern);

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
