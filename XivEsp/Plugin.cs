using System;

using Dalamud.Plugin;

namespace PrincessRTFM.XivEsp;

public class Plugin: IDalamudPlugin {

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

		// TODO add handler for entering PVP to print a message about not working there
	}

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
