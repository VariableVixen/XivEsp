using System.Text.RegularExpressions;

using Dalamud.Game.ClientState.Objects.Types;

namespace VariableVixen.XivEsp.Filters;

public class NameRegexFilter: IGameObjectFilter {
	public const RegexOptions PatternMatchOptions = RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline;

	public Regex Filter { get; init; }
	public string Pattern => this.Filter.ToString();

	public string FilterType => "Regex";
	public string FilterLabel => this.Pattern;

	public char FilterId => 'R';

	public NameRegexFilter(string pattern) {
		this.Filter = new Regex(pattern, PatternMatchOptions);
	}

	public bool Test(IGameObject thing) => IGameObjectFilter.GameObjectIsAlive(thing) && this.Filter.IsMatch(thing.Name.TextValue);
}
