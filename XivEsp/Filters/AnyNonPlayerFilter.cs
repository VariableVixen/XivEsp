using Dalamud.Game.ClientState.Objects.Types;

namespace VariableVixen.XivEsp.Filters;

public class AnyNonPlayerFilter: IGameObjectFilter {

	public string FilterType => "Type";
	public string FilterLabel => "any non-players";

	public char FilterId => 'T';

	public bool Test(IGameObject thing) => IGameObjectFilter.GameObjectIsNotPlayer(thing) && !thing.IsDead;
}
