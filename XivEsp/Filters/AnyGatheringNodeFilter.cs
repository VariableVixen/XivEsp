using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;

namespace VariableVixen.XivEsp.Filters;

internal class AnyGatheringNodeFilter: IGameObjectFilter {
	public string FilterType => "Type";
	public string FilterLabel => "any gathering nodes";

	public char FilterId => 'G';

	public bool Test(IGameObject thing) => IGameObjectFilter.GameObjectIsAlive(thing) && thing.ObjectKind is ObjectKind.GatheringPoint;
}
