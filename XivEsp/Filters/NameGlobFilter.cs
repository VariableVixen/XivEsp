using Dalamud.Game.ClientState.Objects.Types;

using DotNet.Globbing;

namespace VariableVixen.XivEsp.Filters;

public class NameGlobFilter: IGameObjectFilter {
	public static GlobOptions GlobMatchOptions { get; } = new() {
		Evaluation = new() {
			CaseInsensitive = true,
		}
	};

	public Glob Filter { get; init; }
	public string Pattern => this.Filter.ToString();

	public string FilterType => "Glob";
	public string FilterLabel => this.Pattern;

	public char FilterId => 'G';

	public NameGlobFilter(string pattern) {
		this.Filter = Glob.Parse(pattern, GlobMatchOptions);
	}

	public bool Test(IGameObject thing) => IGameObjectFilter.GameObjectIsAlive(thing) && this.Filter.IsMatch(thing.Name.TextValue);
}
