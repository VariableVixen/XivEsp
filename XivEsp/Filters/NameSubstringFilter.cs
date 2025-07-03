using Dalamud.Game.ClientState.Objects.Types;

namespace VariableVixen.XivEsp.Filters;

public class NameSubstringFilter: IGameObjectFilter {

	public string Filter { get; init; }

	public string FilterType => "Substring";
	public string FilterLabel => this.Filter;

	public char FilterId => 'S';

	public NameSubstringFilter(string filter) {
		this.Filter = filter;
	}

	public bool Test(IGameObject thing) => IGameObjectFilter.GameObjectIsAlive(thing) && thing.Name.TextValue.Contains(this.Filter, Constants.StrCompNoCase);
}
