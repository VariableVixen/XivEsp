using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;

namespace VariableVixen.XivEsp.Filters;

public interface IGameObjectFilter {
	public const char
		IdNoFilterSet = 'N',
		IdFilterDisabledInPvp = 'X';

	public bool Test(IGameObject thing);

	public string FilterType { get; }
	public string FilterLabel { get; }

	public char FilterId { get; }

	#region Common utility checks

	public static bool GameObjectExists(IGameObject thing) => thing is not null && thing.IsValid() && thing.IsTargetable;

	public static bool GameObjectIsAlive(IGameObject thing) => GameObjectExists(thing) && !thing.IsDead;

	public static bool GameObjectIsNotPlayer(IGameObject thing) => GameObjectExists(thing) && thing.ObjectKind is not ObjectKind.Player && thing is not IPlayerCharacter;

	#endregion
}
