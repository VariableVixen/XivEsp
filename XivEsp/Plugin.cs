using System;

using Dalamud.Plugin;

namespace PrincessRTFM.XivEsp;

public class Plugin: IDalamudPlugin {
	private bool wasInPvp = false;
	internal ConfigWindow ConfigWindow { get; }

	public Plugin(IDalamudPluginInterface pluginInterface) {
		pluginInterface.Create<Service>(this, pluginInterface.GetPluginConfig() as Configuration ?? new());

		Service.Interface.UiBuilder.Draw += Service.Windows.Draw;
		Service.Interface.UiBuilder.Draw += SearchManager.Render;
		SearchManager.UpdateStatusBar();

		this.ConfigWindow = new();
		Service.Windows.AddWindow(this.ConfigWindow);

		Service.Interface.UiBuilder.OpenMainUi += this.ConfigWindow.Toggle;
		Service.Interface.UiBuilder.OpenConfigUi += this.ConfigWindow.Toggle;

		Service.ClientState.Login += this.PvpWarningCheck;
		Service.ClientState.EnterPvP += this.PvpWarningCheck;
		Service.ClientState.LeavePvP += this.PvpWarningCheck;
		Service.ClientState.Logout += this.PvpWarningCheckLogout;

		if (Service.ClientState.IsLoggedIn)
			this.PvpWarningCheck();
	}

	internal void PvpWarningCheck() {
		if (!Service.ClientState.IsLoggedIn || !Service.ClientState.IsPvP) {
			this.wasInPvp = false;
			return;
		}

		if (this.wasInPvp)
			return;
		this.wasInPvp = true;

		Chat.PrintPvpWarning();
	}

	internal void PvpWarningCheckLogout(int type, int code) => this.PvpWarningCheck();

	#region Disposable
	private bool disposed;
	protected virtual void Dispose(bool disposing) {
		if (this.disposed)
			return;
		this.disposed = true;

		if (disposing) {
			Service.IPC.Dispose();
			Service.Commands.Dispose();
			Service.Interface.UiBuilder.Draw -= Service.Windows.Draw;
			Service.Interface.UiBuilder.Draw -= SearchManager.Render;
			Service.Interface.UiBuilder.OpenMainUi -= this.ConfigWindow.Toggle;
			Service.Interface.UiBuilder.OpenConfigUi -= this.ConfigWindow.Toggle;
			Service.StatusEntry.Remove();
		}
	}
	public void Dispose() {
		this.Dispose(true);
		GC.SuppressFinalize(this);
	}
	#endregion
}
